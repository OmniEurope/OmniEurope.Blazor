import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

// Boxes laid out by hand: the fill measure only reads rectangles and computed styles, never the layout itself.
class FakeElement {
    constructor(box, style = {}) {
        this.box = box;
        this.computed = { paddingBottom: '0', borderBottomWidth: '0', marginBottom: '0', position: 'static', overflowY: 'visible', ...style };
        this.children = [];
        this.parentElement = null;
        this.properties = new Map();
        this.style = {
            setProperty: (name, value) => this.properties.set(name, value),
            removeProperty: name => this.properties.delete(name)
        };
    }

    getBoundingClientRect() {
        return { top: this.box.top, bottom: this.box.top + this.box.height, height: this.box.height };
    }

    getClientRects() {
        return this.box.height > 0 ? [this.box] : [];
    }

    closest(selector) {
        return selector === '.omni-data-grid' ? this.grid ?? null : null;
    }
}

const page = new FakeElement({ top: 0, height: 940 });
page.scrollTop = 0;
globalThis.HTMLElement = FakeElement;
globalThis.getComputedStyle = element => element.computed;
globalThis.window = { innerHeight: 940, addEventListener: () => {}, removeEventListener: () => {}, requestAnimationFrame: () => 1, cancelAnimationFrame: () => {} };
globalThis.document = { addEventListener: () => {}, removeEventListener: () => {}, scrollingElement: page, documentElement: page };
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const { attachFill, detachFill } = await import(pathToFileURL(path.join(wwwroot, 'omni-grid.js')).href);

// A grid 262 px down a 940 px window: 678 px are left to it.
const gridWith = (...children) => {
    const grid = new FakeElement({ top: 262, height: 678 });
    for (const child of children) {
        child.parentElement = grid;
        grid.children.push(child);
    }

    return grid;
};

const room = grid => Number.parseFloat(grid.properties.get('--omni-grid-viewport-room'));

test('a viewport the 75dvh floor makes taller than its grid leaves the rows no more than the room left', () => {
    // The floor (75dvh = 705 px) holds the viewport above the 678 px grid: the difference is negative.
    const viewport = new FakeElement({ top: 262, height: 705 });
    const grid = gridWith(viewport);
    viewport.grid = grid;

    attachFill(viewport);

    assert.equal(grid.properties.get('--omni-grid-fill-height'), '678px');
    assert.equal(room(grid), 678);
    detachFill(viewport);
});

test('the toolbar above and the pager below an overflowing viewport are taken off the room', () => {
    const toolbar = new FakeElement({ top: 262, height: 40 });
    const viewport = new FakeElement({ top: 302, height: 705 });
    const pager = new FakeElement({ top: 1007, height: 36 });
    const menu = new FakeElement({ top: 300, height: 2000 }, { position: 'absolute' });
    const grid = gridWith(toolbar, viewport, pager, menu);
    viewport.grid = grid;

    attachFill(viewport);

    assert.equal(room(grid), 678 - 40 - 36);
    assert.ok(room(grid) <= 678);
    detachFill(viewport);
});

test('a viewport that fits its grid keeps the room its height less the bars', () => {
    const toolbar = new FakeElement({ top: 262, height: 40 });
    const viewport = new FakeElement({ top: 302, height: 600 });
    const pager = new FakeElement({ top: 902, height: 36 }, { marginBottom: '0' });
    const grid = gridWith(toolbar, viewport, pager);
    grid.computed.borderBottomWidth = '2';
    viewport.grid = grid;

    attachFill(viewport);

    assert.equal(room(grid), 678 - 40 - 36 - 2);
    detachFill(viewport);
});
