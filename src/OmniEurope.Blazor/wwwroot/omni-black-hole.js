// Trou noir's field for OmniThemeScope: a black hole drawn by a WebGL shader on a canvas behind the
// content of the scope. Each pixel sends a ray toward the hole and bends it step by step (the photon
// geodesic of a Schwarzschild hole, acceleration -1.5 h² r / |r|^5 with the horizon at r = 1); where the
// ray crosses the plane of the disc between r = 2.9 and r = 15 it picks up the disc's light, and along
// the way the glowing haze around the disc. So the far side of the disc shows as an arc above the horizon
// and a ring below it, as in Interstellar, and the thin photon ring circles the shadow. The gas is white
// hot at the inner edge, the accent colour further out, an ember at the rim, wound into filaments with
// darker lanes of dust; it turns, faster inside, its approaching side whiter and brighter. Faint stars
// are seen through the rays that escape. Rays that fall in draw the page colour: the horizon.
//
// Sober by design: the light rolls off softly and stays under INTENSITY. A frame costs about three
// milliseconds once the shader is compiled (1440 x 900 window); it is drawn 30 times a second, paused while the page is
// hidden, and drawn once when the scope holds its field still (BackdropMotion off) or the system asks
// for less motion. Without WebGL the canvas stays empty and the CSS field of the theme shows instead.

const INTENSITY = 0.5;
const SCALE = 0.75;
const FRAME_MS = 1000 / 30;
const states = new WeakMap();
// The canvases being drawn, so a canvas the page dropped can be found again and stopped (sweep).
const running = new Set();

const VERTEX = 'attribute vec2 a_position; void main() { gl_Position = vec4(a_position, 0.0, 1.0); }';

const FRAGMENT = `
precision highp float;
uniform vec2 u_center;
uniform float u_unit;
uniform float u_time;
uniform float u_tilt;
uniform vec3 u_light;
uniform vec3 u_accent;
uniform vec3 u_surface;
uniform float u_intensity;

float hash(vec2 p) { p = fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32); return fract(p.x * p.y); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}
float fbm(vec2 p) {
    return 0.5 * noise(p) + 0.25 * noise(p * 2.03 + 7.3) + 0.15 * noise(p * 4.1 + 1.7) + 0.1 * noise(p * 8.3 + 4.1);
}

// The colour of the gas at radius rr: white hot at the inner edge, the accent further out, a dim ember
// at the rim. heat > 1 on the side that comes toward the viewer whitens it, < 1 reddens the other side.
vec3 gas(float rr, float heat) {
    float t = smoothstep(3.0, 13.0, rr);
    vec3 hot = mix(u_light, u_accent, 0.3);
    vec3 c = t < 0.3 ? mix(hot, u_accent, t / 0.3) : mix(u_accent, u_accent * 0.32, (t - 0.3) / 0.7);
    return mix(c * vec3(1.0, 0.78, 0.62), mix(c, u_light, 0.35), clamp(heat - 0.4, 0.0, 1.0));
}

void main() {
    // The pixel in units of the impact parameter (2.6 is the edge of the shadow), turned by the tilt.
    vec2 p = (gl_FragCoord.xy - u_center) / u_unit;
    float c = cos(u_tilt), s = sin(u_tilt);
    p = vec2(c * p.x - s * p.y, s * p.x + c * p.y);

    // A distant camera a few degrees above the plane of the disc: parallel rays offset by p.
    float el = 0.1;
    vec3 forward = normalize(vec3(0.0, -sin(el), -cos(el)));
    vec3 up = normalize(vec3(0.0, cos(el), -sin(el)));
    vec3 pos = -forward * 30.0 + vec3(1.0, 0.0, 0.0) * p.x + up * p.y;
    vec3 vel = forward;
    vec3 h = cross(pos, vel);
    float h2 = dot(h, h);

    vec3 emit = vec3(0.0);
    float alpha = 0.0;
    float crossings = 0.0;
    float captured = 0.0;
    for (int i = 0; i < 560; i++) {
        float r2 = dot(pos, pos);
        if (r2 < 1.0) {
            captured = 1.0 - alpha;
            break;
        }
        if (r2 > 1600.0 && dot(pos, vel) > 0.0) break;
        float r = sqrt(r2);
        float dt = clamp(0.02 * r, 0.01, 0.9);
        vel += -1.5 * h2 * pos / (r2 * r2 * r) * dt;
        vec3 next = pos + vel * dt;

        // The thick glowing haze around the disc, crossed along the bent ray: it swells the lensed arc
        // and veils the near side of the shadow.
        float rx = length(pos.xz);
        if (rx < 15.0) {
            float haze = exp(-abs(pos.y) / (0.15 + 0.08 * rx)) * smoothstep(2.6, 3.6, rx) * smoothstep(15.0, 4.0, rx) / (rx * rx);
            emit += (1.0 - alpha) * haze * dt * 4.5 * gas(rx, 1.0);
        }

        if (pos.y * next.y < 0.0) {
            vec3 hit = mix(pos, next, pos.y / (pos.y - next.y));
            float rr = length(hit.xz);
            crossings += 1.0;
            if (rr > 2.9 && rr < 15.0) {
                // The gas turns, faster inside, so its streaks wind into thin concentric filaments.
                float angle = atan(hit.z, hit.x) + u_time * 1.1 / pow(rr, 1.5);
                vec2 q = vec2(cos(angle), sin(angle));
                float filaments = fbm(vec2(rr * 5.5, 0.0) + q * 1.1);
                float fine = 0.6 + 0.4 * noise(vec2(rr * 21.0, 3.0) + q * 2.5);
                // Dark lanes of dust, further out, where the gas is cooler.
                float dust = smoothstep(0.45, 0.75, fbm(q * 2.4 + vec2(rr * 1.4, 9.0))) * smoothstep(4.0, 7.5, rr);
                float profile = smoothstep(2.9, 3.5, rr) * pow(3.5 / rr, 1.5) * smoothstep(15.0, 6.0, rr);
                vec3 tangent = normalize(vec3(-hit.z, 0.0, hit.x));
                float doppler = clamp(1.0 + 0.55 * dot(tangent, -normalize(vel)) * sqrt(3.0 / rr), 0.45, 1.7);
                // The images after the second crossing (the photon ring) stay faint.
                float a = clamp(profile * (0.45 + 0.75 * filaments) * 1.6, 0.0, 1.0) * (crossings > 2.0 ? 0.2 : 1.0);
                float glow = (0.35 + 0.9 * filaments) * fine * (1.0 - 0.8 * dust) * doppler * doppler;
                emit += (1.0 - alpha) * a * glow * gas(rr, doppler);
                alpha += (1.0 - alpha) * a;
                if (alpha > 0.98) break;
            }
        }
        pos = next;
    }

    // A few faint stars, seen through the bent rays that escaped: the sky behind the hole, shifted by
    // the deflection of each ray, so the stars close to the shadow are lensed too.
    if (captured == 0.0 && alpha < 0.98) {
        vec3 d = normalize(vel);
        vec2 sky = (p + vec2(d.x, dot(d, up)) * 6.0) * 2.6;
        vec2 cell = floor(sky);
        float star = step(0.988, hash(cell)) * smoothstep(0.32, 0.0, length(fract(sky) - 0.5 - 0.3 * (vec2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5)));
        emit += (1.0 - alpha) * star * (0.25 + 0.5 * hash(cell + 1.3)) * u_light * 0.5;
    }

    // The photon ring: light that circled the hole before escaping, a thin line at the critical impact
    // parameter (3 sqrt(3) / 2 horizon radii), smooth where the images it is made of would break into dots. The disc in front hides it.
    emit += pow(1.0 - alpha, 4.0) * exp(-pow((sqrt(h2) - 2.598) / 0.035, 2.0)) * 0.4 * mix(u_light, u_accent, 0.3);

    // Soft highlights: the brightest gas rolls off to white rather than clipping.
    vec3 light = (1.0 - exp(-emit * 1.4)) * u_intensity;
    float glowAlpha = clamp(max(light.r, max(light.g, light.b)), 0.0, 1.0);
    gl_FragColor = vec4(light + captured * u_surface, max(glowAlpha, captured));
}
`;

function compile(gl, type, source) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    return gl.getShaderParameter(shader, gl.COMPILE_STATUS) ? shader : null;
}

/** A CSS colour token of the scope as 0..1 RGB, read through a probe element (the CSSOM, which a strict CSP allows). */
function readColour(scope, token, fallback) {
    const probe = document.createElement('span');
    probe.hidden = true;
    probe.style.color = `var(${token}, ${fallback})`;
    scope.appendChild(probe);
    const computed = getComputedStyle(probe).color;
    probe.remove();
    // rgb() comes on 0..255; a color-mix() (Trou noir's page) comes back as color(srgb ...) on 0..1.
    const scale = computed.startsWith('color(') ? 1 : 255;
    const parts = computed.match(/[\d.]+/g) || [];
    return parts.length >= 3 ? parts.slice(0, 3).map(value => Number(value) / scale) : [0, 0, 0];
}

/**
 * Starts drawing the hole on the canvas; returns false when WebGL is unavailable, in which case the
 * scope keeps the theme's CSS field. moving false draws one still frame.
 */
export function start(canvas, scope, moving) {
    // A restart keeps the context: once lost, the canvas hands the same lost context back.
    halt(canvas, false);
    if (!canvas || !scope) {
        return false;
    }

    const gl = canvas.getContext('webgl', { premultipliedAlpha: true, antialias: false, alpha: true });
    const vertex = gl && compile(gl, gl.VERTEX_SHADER, VERTEX);
    const fragment = gl && compile(gl, gl.FRAGMENT_SHADER, FRAGMENT);
    if (!vertex || !fragment || gl.isContextLost()) {
        return false;
    }

    const program = gl.createProgram();
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    // Linked, the program keeps what it needs: the shaders go with it.
    gl.deleteShader(vertex);
    gl.deleteShader(fragment);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        gl.deleteProgram(program);
        return false;
    }

    gl.useProgram(program);
    const buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    const position = gl.getAttribLocation(program, 'a_position');
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

    const uniform = name => gl.getUniformLocation(program, name);
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
    const state = { gl, program, buffer, frame: 0, last: 0, origin: performance.now(), moving, reduced, colours: null, resize: null, visibility: null };
    const animated = () => state.moving && !state.reduced.matches;

    const draw = now => {
        const width = Math.max(1, Math.round(window.innerWidth * SCALE));
        const height = Math.max(1, Math.round(window.innerHeight * SCALE));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }

        // Colours are read once per size or theme change, not at every frame.
        state.colours ??= [readColour(scope, '--omni-color-text', '#fff'), readColour(scope, '--omni-color-accent', '#d9660b'), readColour(scope, '--omni-scope-page', 'var(--omni-color-surface, #000)')];
        gl.viewport(0, 0, width, height);
        // The horizon sits high on the right, where the CSS field of the theme puts it.
        gl.uniform2f(uniform('u_center'), width * 0.78, height * 0.74);
        gl.uniform1f(uniform('u_unit'), Math.max(width, height) * 0.024);
        gl.uniform1f(uniform('u_time'), (now - state.origin) / 1000);
        gl.uniform1f(uniform('u_tilt'), -11 * Math.PI / 180);
        gl.uniform3fv(uniform('u_light'), state.colours[0]);
        gl.uniform3fv(uniform('u_accent'), state.colours[1]);
        gl.uniform3fv(uniform('u_surface'), state.colours[2]);
        gl.uniform1f(uniform('u_intensity'), INTENSITY);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    };

    const loop = now => {
        state.frame = 0;
        // The scope dropped its canvas (another theme): nothing left to draw on.
        if (!canvas.isConnected) {
            stop(canvas);
            return;
        }
        if (now - state.last >= FRAME_MS) {
            state.last = now;
            draw(now);
        }
        if (animated() && !document.hidden) {
            state.frame = requestAnimationFrame(loop);
        }
    };

    state.resize = () => {
        state.colours = null;
        if (!state.frame) {
            draw(performance.now());
        }
    };
    state.visibility = () => {
        if (!document.hidden && animated() && !state.frame) {
            state.frame = requestAnimationFrame(loop);
        }
    };
    window.addEventListener('resize', state.resize);
    document.addEventListener('visibilitychange', state.visibility);
    states.set(canvas, state);
    running.add(canvas);

    draw(performance.now());
    if (animated()) {
        state.frame = requestAnimationFrame(loop);
    }
    return true;
}

/** Repaints with the colours in force after the scope's tokens changed (another palette, another mode). */
export function refresh(canvas) {
    const state = canvas ? states.get(canvas) : undefined;
    if (state) {
        state.resize();
    }
}

/** Stops drawing and releases the WebGL context. */
export function stop(canvas) {
    halt(canvas, true);
}

function halt(canvas, release) {
    const state = canvas ? states.get(canvas) : undefined;
    if (!state) {
        return;
    }

    if (state.frame) {
        cancelAnimationFrame(state.frame);
    }
    window.removeEventListener('resize', state.resize);
    document.removeEventListener('visibilitychange', state.visibility);
    if (release) {
        state.gl.getExtension('WEBGL_lose_context')?.loseContext();
    }
    else {
        // The context stays for the next start, which makes its own program and buffer.
        state.gl.deleteProgram(state.program);
        state.gl.deleteBuffer(state.buffer);
    }
    states.delete(canvas);
    running.delete(canvas);
}

/** Stops every canvas this module draws that is no longer in the page (the scope left the theme). */
export function sweep() {
    for (const canvas of [...running]) {
        if (!canvas.isConnected) {
            stop(canvas);
        }
    }
}
