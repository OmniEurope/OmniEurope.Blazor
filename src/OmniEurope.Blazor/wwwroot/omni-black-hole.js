// Trou noir's field for OmniThemeScope: a black hole drawn by a WebGL shader on a canvas behind the
// content of the scope. Each pixel sends a ray toward the hole and bends it step by step (the photon
// geodesic of a Schwarzschild hole, acceleration -1.5 h² r / |r|^5 with the horizon at r = 1); where the
// ray crosses the plane of the disc between r = 2.9 and r = 8.5 it picks up the disc's light. So the far
// side of the disc shows as an arc above the horizon and a ring below it, as in Interstellar, and the
// thin photon ring circles the shadow. The disc turns, faster inside, its approaching side brighter.
// Rays that fall in draw the page colour: the horizon.
//
// Sober by design: the light is the text colour at INTENSITY at most. A frame costs one to three
// milliseconds once the shader is compiled; it is drawn 30 times a second, paused while the page is
// hidden, and drawn once when the scope holds its field still (BackdropMotion off) or the system asks
// for less motion. Without WebGL the canvas stays empty and the CSS field of the theme shows instead.

const INTENSITY = 0.62;
const SCALE = 0.75;
const FRAME_MS = 1000 / 30;
const states = new WeakMap();

const VERTEX = 'attribute vec2 a_position; void main() { gl_Position = vec4(a_position, 0.0, 1.0); }';

const FRAGMENT = `
precision highp float;
uniform vec2 u_center;
uniform float u_unit;
uniform float u_time;
uniform float u_tilt;
uniform vec3 u_light;
uniform vec3 u_surface;
uniform float u_intensity;

float hash(vec2 p) { p = fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32); return fract(p.x * p.y); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}
float fbm(vec2 p) { return 0.6 * noise(p) + 0.3 * noise(p * 2.1 + 7.3) + 0.1 * noise(p * 4.3 + 1.7); }

void main() {
    // The pixel in units of the impact parameter (2.6 is the edge of the shadow), turned by the tilt.
    vec2 p = (gl_FragCoord.xy - u_center) / u_unit;
    float c = cos(u_tilt), s = sin(u_tilt);
    p = vec2(c * p.x - s * p.y, s * p.x + c * p.y);

    // A distant camera a few degrees above the plane of the disc: parallel rays offset by p.
    float el = 0.12;
    vec3 forward = normalize(vec3(0.0, -sin(el), -cos(el)));
    vec3 up = normalize(vec3(0.0, cos(el), -sin(el)));
    vec3 pos = -forward * 30.0 + vec3(1.0, 0.0, 0.0) * p.x + up * p.y;
    vec3 vel = forward;
    vec3 h = cross(pos, vel);
    float h2 = dot(h, h);

    vec3 color = vec3(0.0);
    float alpha = 0.0;
    float crossings = 0.0;
    for (int i = 0; i < 520; i++) {
        float r2 = dot(pos, pos);
        if (r2 < 1.0) {
            color += (1.0 - alpha) * u_surface;
            alpha = 1.0;
            break;
        }
        if (r2 > 1400.0 && dot(pos, vel) > 0.0) break;
        float r = sqrt(r2);
        float dt = clamp(0.022 * r, 0.012, 1.0);
        vel += -1.5 * h2 * pos / (r2 * r2 * r) * dt;
        vec3 next = pos + vel * dt;
        if (pos.y * next.y < 0.0) {
            vec3 hit = mix(pos, next, pos.y / (pos.y - next.y));
            float rr = length(hit.xz);
            crossings += 1.0;
            if (rr > 2.9 && rr < 8.5) {
                float angle = atan(hit.z, hit.x) + u_time * 1.2 / pow(rr, 1.5);
                vec2 q = vec2(cos(angle), sin(angle)) * rr * 0.9;
                float lanes = 0.7 + 0.3 * fbm(vec2(rr * 3.2, 0.0) + q * 0.35);
                float swirl = 0.75 + 0.25 * fbm(q * 1.6 + vec2(rr * 0.7));
                float profile = smoothstep(2.9, 3.3, rr) * pow(3.2 / rr, 2.0) * smoothstep(8.5, 5.5, rr);
                vec3 tangent = normalize(vec3(-hit.z, 0.0, hit.x));
                float doppler = clamp(1.0 + 0.6 * dot(tangent, -normalize(vel)) * sqrt(3.0 / rr), 0.4, 1.8);
                // The images after the second crossing (the photon ring) stay faint.
                float a = clamp(profile * lanes * swirl * doppler * 1.3, 0.0, 1.0) * (crossings > 2.0 ? 0.35 : 1.0);
                vec3 light = mix(u_light * vec3(1.0, 0.93, 0.84), u_light, smoothstep(3.0, 6.0, rr));
                color += (1.0 - alpha) * a * light * u_intensity;
                alpha += (1.0 - alpha) * a;
                if (alpha > 0.98) break;
            }
        }
        pos = next;
    }

    gl_FragColor = vec4(color, alpha);
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
    const parts = getComputedStyle(probe).color.match(/[\d.]+/g) || [];
    probe.remove();
    return parts.length >= 3 ? parts.slice(0, 3).map(value => Number(value) / 255) : [0, 0, 0];
}

/**
 * Starts drawing the hole on the canvas; returns false when WebGL is unavailable, in which case the
 * scope keeps the theme's CSS field. moving false draws one still frame.
 */
export function start(canvas, scope, moving) {
    stop(canvas);
    if (!canvas || !scope) {
        return false;
    }

    const gl = canvas.getContext('webgl', { premultipliedAlpha: true, antialias: false, alpha: true });
    const vertex = gl && compile(gl, gl.VERTEX_SHADER, VERTEX);
    const fragment = gl && compile(gl, gl.FRAGMENT_SHADER, FRAGMENT);
    if (!vertex || !fragment) {
        return false;
    }

    const program = gl.createProgram();
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        return false;
    }

    gl.useProgram(program);
    gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    const position = gl.getAttribLocation(program, 'a_position');
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

    const uniform = name => gl.getUniformLocation(program, name);
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
    const state = { gl, frame: 0, last: 0, origin: performance.now(), moving, reduced, colours: null, resize: null, visibility: null };
    const animated = () => state.moving && !state.reduced.matches;

    const draw = now => {
        const width = Math.max(1, Math.round(window.innerWidth * SCALE));
        const height = Math.max(1, Math.round(window.innerHeight * SCALE));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }

        // Colours are read once per size or theme change, not at every frame.
        state.colours ??= [readColour(scope, '--omni-color-text', '#fff'), readColour(scope, '--omni-color-surface', '#000')];
        gl.viewport(0, 0, width, height);
        // The horizon sits high on the right, where the CSS field of the theme puts it.
        gl.uniform2f(uniform('u_center'), width * 0.76, height * 0.74);
        gl.uniform1f(uniform('u_unit'), Math.max(width, height) * 0.034);
        gl.uniform1f(uniform('u_time'), (now - state.origin) / 1000);
        gl.uniform1f(uniform('u_tilt'), -16 * Math.PI / 180);
        gl.uniform3fv(uniform('u_light'), state.colours[0]);
        gl.uniform3fv(uniform('u_surface'), state.colours[1]);
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
    const state = canvas ? states.get(canvas) : undefined;
    if (!state) {
        return;
    }

    if (state.frame) {
        cancelAnimationFrame(state.frame);
    }
    window.removeEventListener('resize', state.resize);
    document.removeEventListener('visibilitychange', state.visibility);
    state.gl.getExtension('WEBGL_lose_context')?.loseContext();
    states.delete(canvas);
}
