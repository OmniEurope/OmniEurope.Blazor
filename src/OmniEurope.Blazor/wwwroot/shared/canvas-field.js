// The lifecycle the animated canvas backdrops of OmniThemeScope share (omni-pixel-stars.js, omni-black-hole.js):
// a loop paced at 30 frames a second that stops while the page is hidden or the system asks for less motion and
// starts again after, one still frame when the field is held still, a repaint when the window, the scheme or the
// scope's tokens change, and a release of what a backdrop holds (a WebGL context) once it stops. A backdrop
// brings only its drawing: a `prepare` function that makes a painter for one canvas.
//
// Nothing but the canvas API and a hidden probe element read through the CSSOM is touched, as the strict CSP
// requires (no style attribute, no style element, no eval).

// Drawn at the screen's own resolution, device pixels and not CSS pixels: a smaller canvas is stretched over the
// window and shows its pixels. At most two device pixels per CSS pixel bound the cost.
const MAX_PIXEL_RATIO = 2;
/** Device pixels per CSS pixel of the window, from 1 to 2. */
export const pixelRatio = () => Math.min(Math.max(window.devicePixelRatio || 1, 1), MAX_PIXEL_RATIO);

const FRAME_MS = 1000 / 30;
// Animation frames come every 16.7 ms at 60 Hz, two of them a hair under FRAME_MS apart: without this slack
// every third frame would be drawn, 20 a second instead of 30.
const FRAME_SLACK_MS = 2;

/**
 * A CSS colour token of the scope, read through a probe element (the CSSOM, which a strict CSP allows):
 * its three channels as the browser wrote them and the full scale of the channels (255 for rgb(), 1 for a
 * color(srgb ...) such as a color-mix() comes back as), or null when the value has no three channels.
 */
export function readColour(scope, token, fallback) {
    const probe = document.createElement('span');
    probe.hidden = true;
    probe.style.color = `var(${token}, ${fallback})`;
    scope.appendChild(probe);
    const computed = getComputedStyle(probe).color;
    probe.remove();
    const parts = computed.match(/[\d.]+/g) || [];
    return parts.length >= 3 ? { channels: parts.slice(0, 3).map(Number), max: computed.startsWith('color(') ? 1 : 255 } : null;
}

/**
 * Makes the start, refresh, stop and sweep functions of one backdrop module. `prepare(canvas, scope)` makes
 * the painter of a canvas, or returns null when it cannot draw (no context), in which case the scope keeps the
 * theme's CSS field. The painter has:
 *   draw(now)         paints one frame at the time <now> (milliseconds);
 *   invalidate()      drops what was read or sized for the window, the scheme or the tokens (read again by draw);
 *   tick(now, last)   optional, before each frame of the loop (not before a still one), <last> being the time of
 *                     the previous frame, 0 after a return to the page; returns false to hold the field still;
 *   resume()          optional, when the page is visible again after it was hidden;
 *   release(lose)     optional, when the canvas stops: lose is true for a stop, false for a restart on the same canvas.
 */
export function createCanvasField(prepare) {
    const states = new WeakMap();
    // The canvases being drawn, so a canvas the page dropped can be found again and stopped (sweep).
    const running = new Set();

    function halt(canvas, lose) {
        const state = canvas ? states.get(canvas) : undefined;
        if (!state) {
            return;
        }

        if (state.frame) {
            cancelAnimationFrame(state.frame);
        }
        window.removeEventListener('resize', state.resize);
        document.removeEventListener('visibilitychange', state.visibility);
        state.scheme.removeEventListener('change', state.resize);
        state.reduced.removeEventListener('change', state.motion);
        state.painter.release?.(lose);
        states.delete(canvas);
        running.delete(canvas);
    }

    /** Stops drawing and releases what the painter holds. */
    function stop(canvas) {
        halt(canvas, true);
    }

    /**
     * Starts drawing on the canvas; returns false when the painter cannot draw, in which case the scope keeps
     * the theme's CSS field. moving false draws one still frame.
     */
    function start(canvas, scope, moving) {
        // A restart keeps what a canvas cannot give twice (a lost WebGL context is handed back lost).
        halt(canvas, false);
        if (!canvas || !scope) {
            return false;
        }

        const painter = prepare(canvas, scope);
        if (!painter) {
            return false;
        }

        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
        // The system mode, followed by a scope in System mode: its change swaps the tokens with no .NET round trip.
        const scheme = window.matchMedia('(prefers-color-scheme: dark)');
        // last: the time of the previous frame drawn, 0 after a return to the page.
        const state = { painter, frame: 0, last: 0, moving, reduced, scheme, resize: null, visibility: null, motion: null };
        const animated = () => state.moving && !state.reduced.matches;

        const loop = now => {
            state.frame = 0;
            // The scope dropped its canvas (another theme): nothing left to draw on.
            if (!canvas.isConnected) {
                stop(canvas);
                return;
            }
            if (now - state.last >= FRAME_MS - FRAME_SLACK_MS) {
                const hold = painter.tick?.(now, state.last) === false;
                state.last = now;
                painter.draw(now);
                if (hold) {
                    state.moving = false;
                }
            }
            if (animated() && !document.hidden) {
                state.frame = requestAnimationFrame(loop);
            }
        };

        state.resize = () => {
            painter.invalidate();
            if (!state.frame) {
                painter.draw(performance.now());
            }
        };
        state.visibility = () => {
            if (document.hidden) {
                return;
            }
            // The time spent away from the page is not time the field moved.
            state.last = 0;
            painter.resume?.();
            if (animated() && !state.frame) {
                state.frame = requestAnimationFrame(loop);
            }
        };
        // The system asks for less motion, or no longer does: the field stops on its current frame, or moves again.
        state.motion = () => {
            if (!state.reduced.matches) {
                state.visibility();
            }
            else if (state.frame) {
                cancelAnimationFrame(state.frame);
                state.frame = 0;
            }
        };
        window.addEventListener('resize', state.resize);
        document.addEventListener('visibilitychange', state.visibility);
        state.scheme.addEventListener('change', state.resize);
        state.reduced.addEventListener('change', state.motion);
        states.set(canvas, state);
        running.add(canvas);

        state.last = performance.now();
        painter.draw(state.last);
        if (animated()) {
            state.frame = requestAnimationFrame(loop);
        }
        return true;
    }

    /** Repaints with what is in force after the scope's tokens changed (another palette, another mode). */
    function refresh(canvas) {
        const state = canvas ? states.get(canvas) : undefined;
        if (state) {
            state.resize();
        }
    }

    /** Stops every canvas this module draws that is no longer in the page (the scope left the theme). */
    function sweep() {
        for (const canvas of [...running]) {
            if (!canvas.isConnected) {
                stop(canvas);
            }
        }
    }

    return { start, refresh, stop, sweep };
}
