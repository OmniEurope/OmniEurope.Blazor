import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The horizontal arrows of the elements marked data-omni-mirror-arrows (the day grid of the date pickers, the
// radio cards of OmniSelectableCardGroup), which .NET counts in reading order. In a right-to-left page the
// script swaps them before the handler of Blazor sees them.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
let loads = 0;

// A page with a marked element holding a button, the focus module loaded, and a listener standing for the
// handler of Blazor (registered after the module's, at the bubbling phase). The dispatch honours
// stopImmediatePropagation, as the browser does, and a copy of an event goes to its target the same way.
const fixture = async (direction, marked = true) => {
    const dom = installDom();
    const listeners = [];
    document.addEventListener = (type, handler, capture) => {
        if (type === 'keydown') {
            listeners.push({ handler, capture: capture === true });
        }
    };
    globalThis.getComputedStyle = () => ({ direction });
    globalThis.KeyboardEvent = class {
        constructor(type, init) {
            Object.assign(this, init, { type, defaultPrevented: false });
        }

        preventDefault() { this.defaultPrevented = true; }
        stopImmediatePropagation() { this.stopped = true; }
    };
    const day = dom.el('button', {});
    const grid = dom.el('div', marked ? { 'data-omni-mirror-arrows': '' } : {}, day);
    dom.body.append(grid);
    day.dispatchEvent = event => {
        event.target = day;
        const ordered = [...listeners.filter(item => item.capture), ...listeners.filter(item => !item.capture)];
        for (const { handler } of ordered) {
            if (event.stopped) {
                break;
            }

            handler(event);
        }

        return !event.defaultPrevented;
    };
    await import(`${pathToFileURL(path.join(wwwroot, 'focus/keys.js')).href}?mirror${loads++}`);
    const received = [];
    listeners.push({ handler: event => received.push(event), capture: false });
    const press = (key, extra = {}) => {
        const original = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...extra });
        day.dispatchEvent(original);
        return original;
    };
    return { day, press, received };
};

test('left to right, the arrows reach the handler as they are', async () => {
    const { press, received } = await fixture('ltr');

    const left = press('ArrowLeft');
    press('ArrowRight');

    assert.deepEqual(received.map(event => event.key), ['ArrowLeft', 'ArrowRight']);
    assert.equal(received[0], left);
});

test('right to left, ArrowLeft reaches the handler as ArrowRight and ArrowRight as ArrowLeft, once each', async () => {
    const { press, received } = await fixture('rtl');

    const left = press('ArrowLeft');
    const right = press('ArrowRight');

    assert.deepEqual(received.map(event => event.key), ['ArrowRight', 'ArrowLeft'], 'the next day is drawn on the left: its key is the one .NET calls ArrowRight');
    assert.deepEqual(received, [left, right], 'the handler gets the very events the browser sent');
    assert.ok(!left.defaultPrevented && !left.stopped, 'the event keeps its default action and its other listeners');
});

test('right to left, the swapped event keeps its modifiers', async () => {
    const { press, received } = await fixture('rtl');

    press('ArrowLeft', { shiftKey: true, repeat: true });

    assert.equal(received.length, 1);
    assert.equal(received[0].key, 'ArrowRight');
    assert.equal(received[0].shiftKey, true);
    assert.equal(received[0].repeat, true);
});

test('right to left, the vertical keys, the page keys, Home and End are left alone', async () => {
    const { press, received } = await fixture('rtl');

    for (const key of ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End']) {
        press(key);
    }

    assert.deepEqual(received.map(event => event.key), ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End']);
});

test('right to left, an element without the marker keeps its arrows', async () => {
    const { press, received } = await fixture('rtl', false);

    press('ArrowLeft');

    assert.deepEqual(received.map(event => event.key), ['ArrowLeft']);
});
