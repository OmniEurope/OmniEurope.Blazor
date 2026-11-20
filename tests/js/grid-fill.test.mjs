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

test('the grid is given the height left under its top, and nothing caps the MinHeight floor', () => {
    // A floor of 705 px (an explicit 75dvh) holds the viewport above the 678 px left: it stays a true floor (owner rule
    // of 2026-11-08), the page scrolls past the grid, so no room that would cap it is written.
    const toolbar = new FakeElement({ top: 262, height: 40 });
    const viewport = new FakeElement({ top: 302, height: 705 });
    const pager = new FakeElement({ top: 1007, height: 36 });
    const grid = gridWith(toolbar, viewport, pager);
    viewport.grid = grid;

    attachFill(viewport);

    assert.equal(grid.properties.get('--omni-grid-fill-height'), '678px');
    assert.equal(grid.properties.has('--omni-grid-viewport-room'), false);
    detachFill(viewport);
    assert.equal(grid.properties.has('--omni-grid-fill-height'), false);
});

test('a grid whose content above fills the window is given 0 px and keeps its floor alone', () => {
    const viewport = new FakeElement({ top: 1200, height: 705 });
    const grid = new FakeElement({ top: 1200, height: 705 });
    viewport.parentElement = grid;
    grid.children.push(viewport);
    viewport.grid = grid;

    attachFill(viewport);

    assert.equal(grid.properties.get('--omni-grid-fill-height'), '0px');
    detachFill(viewport);
});
