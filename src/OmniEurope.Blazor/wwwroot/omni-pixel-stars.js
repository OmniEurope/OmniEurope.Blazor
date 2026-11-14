// Octet's field for OmniThemeScope: pixel stars on a 2D canvas behind the content of the scope, three
// layers that scroll at three speeds (a parallax: the near stars cross the window five times faster than
// the far ones), down and to the left, wrapping at the edges. A star is a square of one "pixel of art" (two
// CSS pixels), snapped to the grid of those pixels, so the motion goes pixel by pixel as in an eight-bit game;
// it twinkles through four levels of brightness on a period of its own (3 to 9 seconds, never more than two
// steps a second), and the nearest stars grow a plus-shaped glint at their brightest. The positions come from
// a fixed seed, in fractions of the window, so a resize keeps the same sky.
//
// The colours are the two stops the theme's CSS field declares (--omni-backdrop-star for most stars,
// --omni-backdrop-spark for one in five), read from the scope, which the contrast checks measure: a star is
// never brighter than that stop, only dimmer at the low end of its twinkle, so the text reads over it as it
// does on the stop.
//
// Light and cheap by design: about two hundred squares a frame, 30 frames a second, paused while the page is
// hidden, and one still frame when the scope holds its field still (BackdropMotion off) or the system asks for
// less motion, which the field follows as it changes. Nothing but the canvas API and a hidden probe element
// read through the CSSOM is touched, as the strict CSP requires (no style attribute, no style element).

import { createCanvasField, pixelRatio, readColour as readChannels } from './shared/canvas-field.js';

const PIXEL = 2;
// A gap longer than this between two frames (the tab was in the background) is not time the sky moved.
const MAX_STEP_SECONDS = 0.25;
const TAU = Math.PI * 2;
const SEED = 0x0c7e7;

// area: CSS pixels squared of window per star; speed: CSS pixels a second to the left and downwards;
// strength: the share of the stop's brightness the layer reaches; glint: grows a plus at its brightest.
const LAYERS = [
    { area: 9000, left: 3, down: 1.2, strength: 0.6, glint: false },
    { area: 24000, left: 8, down: 3, strength: 0.85, glint: false },
    { area: 90000, left: 16, down: 6, strength: 1, glint: true },
];

/** A small seeded generator (mulberry32): the same stars every time. */
function generator(seed) {
    let state = seed >>> 0;
    return () => {
        state = (state + 0x6d2b79f5) >>> 0;
        let value = Math.imul(state ^ (state >>> 15), 1 | state);
        value = (value + Math.imul(value ^ (value >>> 7), 61 | value)) ^ value;
        return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
    };
}

/** The stars of every layer for a window of <width> by <height> CSS pixels, as fractions of the window. */
function makeStars(width, height) {
    const random = generator(SEED);
    return LAYERS.map(layer => Array.from({ length: Math.max(8, Math.round(width * height / layer.area)) }, () => ({
        x: random(),
        y: random(),
        period: 3 + random() * 6,
        phase: random(),
        spark: random() < 0.2,
    })));
}

/** <value> brought into 0..<size> whatever its sign, then onto the grid of <unit>. */
const wrap = (value, size, unit) => Math.floor((((value % size) + size) % size) / unit) * unit;

/** A CSS colour token of the scope as an rgb() string, read through a probe element (the CSSOM, which a strict CSP allows). */
function readColour(scope, token, fallback) {
    const colour = readChannels(scope, token, fallback);
    // rgb() comes on 0..255; a color-mix() comes back as color(srgb ...) on 0..1.
    return colour ? `rgb(${colour.channels.map(value => Math.round(value * (255 / colour.max))).join(' ')})` : fallback;
}

/**
 * The painter of one canvas, or null when it has no 2D context, in which case the scope keeps the theme's CSS
 * field. The colours are read from the scope once per size, theme or mode change.
 */
function prepare(canvas, scope) {
    const context = canvas.getContext('2d');
    if (!context) {
        return null;
    }

    // elapsed: the seconds the sky has moved.
    const state = { elapsed: 0, colours: null, stars: null, stamp: '' };

    const draw = () => {
        const ratio = pixelRatio();
        const width = Math.max(1, Math.round(window.innerWidth * ratio));
        const height = Math.max(1, Math.round(window.innerHeight * ratio));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }

        // Stars and colours are made once per size or theme change, not at every frame.
        const stamp = `${window.innerWidth}x${window.innerHeight}`;
        if (state.stamp !== stamp) {
            state.stamp = stamp;
            state.stars = makeStars(window.innerWidth, window.innerHeight);
        }
        state.colours ??= [readColour(scope, '--omni-backdrop-star', '#808080'), readColour(scope, '--omni-backdrop-spark', '#808080')];

        context.clearRect(0, 0, width, height);
        const unit = PIXEL * ratio;
        LAYERS.forEach((layer, index) => {
            for (const star of state.stars[index]) {
                const x = wrap(star.x * width - state.elapsed * layer.left * ratio, width, unit);
                const y = wrap(star.y * height + state.elapsed * layer.down * ratio, height, unit);
                // Four levels, so the twinkle goes by steps: 0, 1/3, 2/3, 1.
                const step = Math.round((0.5 + 0.5 * Math.sin(TAU * (state.elapsed / star.period + star.phase))) * 3);
                context.globalAlpha = layer.strength * (0.35 + 0.65 * step / 3);
                context.fillStyle = state.colours[star.spark ? 1 : 0];
                context.fillRect(x, y, unit, unit);
                if (layer.glint && step >= 2) {
                    context.fillRect(x - unit, y, unit, unit);
                    context.fillRect(x + unit, y, unit, unit);
                    context.fillRect(x, y - unit, unit, unit);
                    context.fillRect(x, y + unit, unit, unit);
                }
            }
        });
        context.globalAlpha = 1;
    };

    return {
        draw,
        invalidate: () => {
            state.colours = null;
            state.stamp = '';
        },
        // The sky moves by the time since the previous frame; none after a return to the page.
        tick: (now, last) => {
            if (last) {
                state.elapsed += Math.min((now - last) / 1000, MAX_STEP_SECONDS);
            }
        },
    };
}

const field = createCanvasField(prepare);

/**
 * Starts drawing the stars on the canvas; returns false when the canvas has no 2D context, in which case the
 * scope keeps the theme's CSS field. moving false draws one still frame. The colours are read from the scope
 * once per size, theme or mode change.
 */
export const start = field.start;

/** Repaints with the colours in force after the scope's tokens changed (another palette, another mode). */
export const refresh = field.refresh;

/** Stops drawing. */
export const stop = field.stop;

/** Stops every canvas this module draws that is no longer in the page (the scope left the theme). */
export const sweep = field.sweep;
