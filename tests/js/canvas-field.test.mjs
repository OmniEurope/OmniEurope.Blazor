import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The lifecycle the animated canvas backdrops share (shared/canvas-field.js): the loop, its pause while the
// page is hidden or the system asks for less motion, the repaint on resize, the release of what a painter
// holds, and the colour probe. It runs against a painter that writes down what it is asked, the minimal DOM of
// dom-stub.mjs, a clock and a queue of animation frames driven by hand, and media queries a test flips. What
// the browser draws (the stars, the shader) and the real events stay with the showcase probes.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const { createCanvasField, pixelRatio, readColour } = await import(pathToFileURL(path.join(wwwroot, 'shared', 'canvas-field.js')).href);

const FRAME = 34;

let dom;
let clock;
let frames;
let nextFrame;
let queries;
let computedColour;

const installPage = () => {
    dom = installDom();
    clock = 1000;
    frames = new Map();
    nextFrame = 0;
    queries = new Map();
    computedColour = 'rgb(10, 20, 30)';
    Object.defineProperty(globalThis, 'performance', { value: { now: () => clock }, configurable: true, writable: true });
    globalThis.requestAnimationFrame = callback => {
        frames.set(++nextFrame, callback);
        return nextFrame;
    };
    globalThis.cancelAnimationFrame = id => frames.delete(id);
    Object.assign(globalThis.window, {
        innerWidth: 800,
        innerHeight: 600,
        devicePixelRatio: 1,
        matchMedia: query => {
            if (!queries.has(query)) {
                const listeners = new Set();
                queries.set(query, {
                    matches: false,
                    listeners,
                    addEventListener: (type, listener) => listeners.add(listener),
                    removeEventListener: (type, listener) => listeners.delete(listener)
                });
            }

            return queries.get(query);
        }
    });
    globalThis.document.hidden = false;
    // The probe element the colour read appends to the scope and removes again.
    const createElement = globalThis.document.createElement;
    globalThis.document.createElement = name => {
        const element = createElement(name);
        element.remove = () => {
            element.parentElement.children = element.parentElement.children.filter(child => child !== element);
            element.parentElement = null;
        };
        return element;
    };
    globalThis.getComputedStyle = () => ({ color: computedColour });
};

const reduced = () => globalThis.window.matchMedia('(prefers-reduced-motion: reduce)');
const scheme = () => globalThis.window.matchMedia('(prefers-color-scheme: dark)');
const change = (query, matches) => {
    query.matches = matches;
    [...query.listeners].forEach(listener => listener());
};
// Runs the pending animation frame at the time <at> (the clock moves with it).
const frameAt = at => {
    clock = at;
    const due = [...frames.values()];
    frames.clear();
    due.forEach(callback => callback(at));
};

// A backdrop whose painter writes down every call; tick answers what a test sets in painter.holdAfter.
const backdrop = (options = {}) => {
    installPage();
    const log = { prepared: [], draws: [], ticks: [], invalidations: 0, resumes: 0, releases: [] };
    const painter = {
        draw: now => log.draws.push(now),
        invalidate: () => { log.invalidations++; },
        tick: (now, last) => {
            log.ticks.push([now, last]);
            return options.hold?.(log) ?? true;
        },
        resume: () => { log.resumes++; },
        release: lose => log.releases.push(lose)
    };
    const field = createCanvasField((canvas, scope) => {
        log.prepared.push([canvas, scope]);
        return options.refuse ? null : painter;
    });
    const canvas = { isConnected: true };
    const scope = dom.el('div');
    return { ...field, log, canvas, scope, painter };
};

test('the loop starts: one frame at once, then one every 30th of a second', () => {
    const { start, canvas, scope, log } = backdrop();

    assert.equal(start(canvas, scope, true), true);
    assert.deepEqual(log.draws, [1000]);
    assert.equal(frames.size, 1);

    frameAt(1000 + FRAME);
    frameAt(1000 + 2 * FRAME);

    assert.deepEqual(log.draws, [1000, 1034, 1068]);
    assert.deepEqual(log.ticks, [[1034, 1000], [1068, 1034]]);
    assert.equal(frames.size, 1);
});

test('a frame that comes too soon for the pace is not drawn, and the loop goes on', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);

    frameAt(1000 + 10);
    assert.deepEqual(log.draws, [1000]);
    assert.equal(frames.size, 1);

    // Two frames of a 60 Hz screen (16.7 ms apart) still give 30 a second, thanks to the slack.
    frameAt(1000 + 31.5);
    assert.equal(log.draws.length, 2);
});

test('the field is held still, on one frame, when it is not asked to move', () => {
    const { start, canvas, scope, log } = backdrop();

    start(canvas, scope, false);

    assert.deepEqual(log.draws, [1000]);
    assert.equal(frames.size, 0);
});

test('the field is held still, on one frame, when the system already asks for less motion', () => {
    const { start, canvas, scope, log } = backdrop();
    change(reduced(), true);

    start(canvas, scope, true);

    assert.deepEqual(log.draws, [1000]);
    assert.equal(frames.size, 0);
});

test('the loop halts while the page is hidden and starts again, with no time spent away, when it is visible', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);

    // Hidden: the event changes nothing itself, and the frame that comes does not ask for another.
    globalThis.document.hidden = true;
    dom.dispatch('visibilitychange');
    assert.equal(log.resumes, 0);
    frameAt(1000 + FRAME);
    assert.equal(frames.size, 0);

    // Visible again: the previous frame is forgotten (0) so the time away is not a step, and the loop restarts.
    globalThis.document.hidden = false;
    dom.dispatch('visibilitychange');
    assert.equal(log.resumes, 1);
    assert.equal(frames.size, 1);
    frameAt(5000);
    assert.deepEqual(log.ticks.at(-1), [5000, 0]);
    assert.equal(frames.size, 1);
});

test('a visible page that was never hidden does not start a second loop', () => {
    const { start, canvas, scope } = backdrop();
    start(canvas, scope, true);

    dom.dispatch('visibilitychange');

    assert.equal(frames.size, 1);
});

test('the loop halts under reduced motion on its current frame and moves again when the setting is lifted', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);
    assert.equal(frames.size, 1);

    change(reduced(), true);
    assert.equal(frames.size, 0);
    // No frame was pending, so a second change to reduced has nothing to cancel.
    change(reduced(), true);
    assert.equal(frames.size, 0);

    change(reduced(), false);
    assert.equal(frames.size, 1);
    assert.equal(log.resumes, 1);
    frameAt(2000);
    assert.deepEqual(log.ticks.at(-1), [2000, 0]);
});

test('a frame still pending under reduced motion is not drawn again once the loop halted', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);
    change(reduced(), true);
    const drawn = log.draws.length;

    frameAt(1000 + FRAME);

    assert.equal(log.draws.length, drawn);
});

test('a painter that answers false to its tick holds the field still after that frame', () => {
    const { start, canvas, scope, log } = backdrop({ hold: log => log.ticks.length < 3 });
    start(canvas, scope, true);

    frameAt(1034);
    frameAt(1068);
    assert.equal(frames.size, 1);
    frameAt(1102);

    // The held frame is drawn, then no other is asked for.
    assert.equal(log.draws.length, 4);
    assert.equal(frames.size, 0);
});

test('a resize, a change of scheme and a refresh drop what the painter read and repaint a still field', () => {
    const { start, refresh, canvas, scope, log } = backdrop();
    start(canvas, scope, false);

    dom.dispatch('resize');
    assert.equal(log.invalidations, 1);
    assert.equal(log.draws.length, 2);

    change(scheme(), true);
    assert.equal(log.invalidations, 2);
    assert.equal(log.draws.length, 3);

    refresh(canvas);
    assert.equal(log.invalidations, 3);
    assert.equal(log.draws.length, 4);
});

test('a resize while the loop runs only drops what was read: the next frame repaints', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);

    dom.dispatch('resize');

    assert.equal(log.invalidations, 1);
    assert.deepEqual(log.draws, [1000]);
});

test('refresh and stop on a canvas that was never started change nothing', () => {
    const { refresh, stop, canvas, log } = backdrop();

    refresh(canvas);
    refresh(null);
    stop(canvas);
    stop(null);

    assert.deepEqual(log.draws, []);
    assert.deepEqual(log.releases, []);
});

test('stop releases what the painter holds once, cancels the frame and removes every listener', () => {
    const { start, stop, canvas, scope, log } = backdrop();
    start(canvas, scope, true);
    assert.equal(dom.listenerCount('resize'), 1);
    assert.equal(dom.listenerCount('visibilitychange'), 1);
    assert.equal(reduced().listeners.size, 1);
    assert.equal(scheme().listeners.size, 1);

    stop(canvas);
    stop(canvas);

    assert.deepEqual(log.releases, [true]);
    assert.equal(frames.size, 0);
    assert.equal(dom.listenerCount('resize'), 0);
    assert.equal(dom.listenerCount('visibilitychange'), 0);
    assert.equal(reduced().listeners.size, 0);
    assert.equal(scheme().listeners.size, 0);
});

test('a restart on the same canvas releases the first run without losing the context, and listens once', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);

    start(canvas, scope, true);

    assert.deepEqual(log.releases, [false]);
    assert.equal(log.prepared.length, 2);
    assert.equal(frames.size, 1);
    assert.equal(dom.listenerCount('resize'), 1);
});

test('start without a canvas or a scope draws nothing, and a painter that cannot draw leaves no trace', () => {
    const { start, canvas, scope, log } = backdrop();
    assert.equal(start(null, scope, true), false);
    assert.equal(start(canvas, null, true), false);
    assert.deepEqual(log.prepared, []);

    const refusing = backdrop({ refuse: true });
    assert.equal(refusing.start(refusing.canvas, refusing.scope, true), false);
    assert.equal(refusing.log.prepared.length, 1);
    assert.deepEqual(refusing.log.draws, []);
    assert.equal(frames.size, 0);
    assert.equal(dom.listenerCount('resize'), 0);
});

test('a canvas the page dropped stops itself on its next frame', () => {
    const { start, canvas, scope, log } = backdrop();
    start(canvas, scope, true);

    canvas.isConnected = false;
    frameAt(1000 + FRAME);

    assert.deepEqual(log.releases, [true]);
    assert.equal(log.draws.length, 1);
    assert.equal(frames.size, 0);
});

test('sweep stops the canvases the page dropped and keeps the ones still in it', () => {
    const { start, sweep, canvas, scope, log } = backdrop();
    const other = { isConnected: true };
    start(canvas, scope, false);
    start(other, scope, false);

    sweep();
    assert.deepEqual(log.releases, []);

    canvas.isConnected = false;
    sweep();
    assert.deepEqual(log.releases, [true]);
    dom.dispatch('resize');
    // Only the canvas still running answers the resize.
    assert.equal(log.invalidations, 1);
});

test('the colour probe reads the three channels and their scale from the computed colour, and cleans up', () => {
    installPage();
    const scope = dom.el('div');

    assert.deepEqual(readColour(scope, '--omni-color-text', '#fff'), { channels: [10, 20, 30], max: 255 });
    computedColour = 'color(srgb 0.25 0.5 0.75)';
    assert.deepEqual(readColour(scope, '--omni-color-text', '#fff'), { channels: [0.25, 0.5, 0.75], max: 1 });
    computedColour = 'transparent';
    assert.equal(readColour(scope, '--omni-color-text', '#fff'), null);
    assert.equal(scope.children.length, 0);
});

test('the pixel ratio is the device ratio kept between one and two', () => {
    installPage();

    assert.equal(pixelRatio(), 1);
    globalThis.window.devicePixelRatio = 1.5;
    assert.equal(pixelRatio(), 1.5);
    globalThis.window.devicePixelRatio = 3;
    assert.equal(pixelRatio(), 2);
    globalThis.window.devicePixelRatio = 0.5;
    assert.equal(pixelRatio(), 1);
    globalThis.window.devicePixelRatio = undefined;
    assert.equal(pixelRatio(), 1);
});
