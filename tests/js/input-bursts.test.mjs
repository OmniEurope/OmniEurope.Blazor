import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The source face of OmniHtmlEditor (holdInputBursts and takeHeldInput of omniInterop.js): the page does not see the
// input events of the text area one by one, one input with the final text is raised once the typing paused for
// quietMs, and takeHeldInput hands the held text over at once. The clock and the timers are the tests' own.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
installDom();
class FakeTextArea {
    constructor() {
        this.value = '';
        this.captureListeners = [];
        this.otherListeners = {};
        this.pageSaw = [];
    }

    addEventListener(type, listener, capture) {
        if (type === 'input' && capture === true) {
            this.captureListeners.push(listener);
        } else {
            (this.otherListeners[type] ??= []).push(listener);
        }
    }

    // Leaving the text area, or a key pressed in it: its own listeners only, the page is not part of it here.
    fire(type, init = {}) {
        for (const listener of this.otherListeners[type] ?? []) {
            listener({ type, ...init });
        }
    }

    // What the browser does for an input event: the capture listeners first, the page (Blazor's own) unless one stops it.
    type(value) {
        this.value = value;
        this.dispatchEvent(new FakeEvent());
    }

    dispatchEvent(event) {
        for (const listener of this.captureListeners) {
            listener(event);
        }

        if (!event.stopped) {
            this.pageSaw.push(this.value);
        }

        return true;
    }
}

class FakeEvent {
    constructor() {
        this.stopped = false;
    }

    stopPropagation() { this.stopped = true; }
}

// The document, where a press anywhere on the page is heard in its capture phase.
const pageListeners = new Map();
globalThis.document = {
    addEventListener: (type, listener) => pageListeners.set(listener, type),
    removeEventListener: (type, listener) => pageListeners.delete(listener)
};
const press = () => {
    for (const [listener, type] of [...pageListeners]) {
        if (type === 'pointerdown') listener({ type });
    }
};

globalThis.HTMLTextAreaElement = FakeTextArea;
globalThis.Event = class extends FakeEvent {};
let now = 0;
const timers = [];
globalThis.performance = { now: () => now };
globalThis.setTimeout = (callback, milliseconds) => { timers.push({ callback, at: now + milliseconds }); return timers.length; };
globalThis.clearTimeout = id => { if (id > 0 && timers[id - 1]) { timers[id - 1].callback = null; } };
const advance = milliseconds => {
    now += milliseconds;
    for (const timer of timers) {
        if (timer.callback && timer.at <= now) {
            const { callback } = timer;
            timer.callback = null;
            callback();
        }
    }
};

const { holdInputBursts, takeHeldInput } = await import(pathToFileURL(path.join(wwwroot, 'omniInterop.js')).href);

const watched = () => {
    const area = new FakeTextArea();
    holdInputBursts(area, 150);
    return area;
};

test('a single input reaches the page once the typing paused, with its text', () => {
    const area = watched();
    advance(1000);

    area.type('a');
    assert.deepEqual(area.pageSaw, []);
    advance(150);

    assert.deepEqual(area.pageSaw, ['a']);
});

test('the inputs of a burst are kept from the page and one input with the final text is raised after it', () => {
    const area = watched();
    advance(1000);

    area.type('a');
    for (let line = 2; line <= 200; line++) {
        advance(1);
        area.type('a'.repeat(line));
    }

    assert.deepEqual(area.pageSaw, []);
    advance(149);
    assert.deepEqual(area.pageSaw, []);
    advance(2);
    assert.deepEqual(area.pageSaw, ['a'.repeat(200)]);
});

test('the input raised after a burst is not held again', () => {
    const area = watched();
    advance(1000);
    area.type('ab');

    advance(200);

    assert.deepEqual(area.pageSaw, ['ab']);
    advance(1000);
    area.type('abc');
    advance(200);
    assert.deepEqual(area.pageSaw, ['ab', 'abc']);
});

test('takeHeldInput hands the held text over, once, and the page never sees it', () => {
    const area = watched();
    advance(1000);
    area.type('a');
    advance(1);
    area.type('ab');

    assert.equal(takeHeldInput(area), 'ab');
    assert.equal(takeHeldInput(area), null);
    advance(500);

    assert.deepEqual(area.pageSaw, []);
});

test('takeHeldInput with nothing held, or for an element that is not watched, answers null', () => {
    const area = watched();
    advance(1000);
    area.type('a');
    advance(150);

    assert.equal(takeHeldInput(area), null);
    assert.equal(takeHeldInput(new FakeTextArea()), null);
    assert.equal(takeHeldInput(null), null);
});

test('watching the same text area twice listens once', () => {
    const area = new FakeTextArea();
    holdInputBursts(area, 150);
    holdInputBursts(area, 150);

    assert.equal(area.captureListeners.length, 1);
});

test('a press anywhere on the page hands the held text to the page at once, before the press is handled', () => {
    const area = watched();
    advance(1000);
    area.type('a');
    advance(1);
    area.type('ab');

    // A host Save button pressed right after typing: the page has the final text before the click runs.
    press();
    assert.deepEqual(area.pageSaw, ['ab']);

    // Released once: neither the timer nor another press raises it again, and the page listener is gone.
    advance(500);
    press();
    assert.deepEqual(area.pageSaw, ['ab']);
    assert.equal([...pageListeners.values()].filter(type => type === 'pointerdown').length, 0);
});

test('leaving the text area hands the held text to the page at once', () => {
    const area = watched();
    advance(1000);
    area.type('abc');

    area.fire('blur');

    assert.deepEqual(area.pageSaw, ['abc']);
    advance(500);
    assert.deepEqual(area.pageSaw, ['abc']);
});

test('a shortcut hands the held text to the page at once, a plain key keeps holding it', () => {
    const area = watched();
    advance(1000);
    area.type('a');

    area.fire('keydown', { key: 'b' });
    assert.deepEqual(area.pageSaw, []);

    area.fire('keydown', { key: 's', ctrlKey: true });
    assert.deepEqual(area.pageSaw, ['a']);
});

test('takeHeldInput also stops listening to the page', () => {
    const area = watched();
    advance(1000);
    area.type('a');

    assert.equal(takeHeldInput(area), 'a');

    assert.equal([...pageListeners.values()].filter(type => type === 'pointerdown').length, 0);
});
