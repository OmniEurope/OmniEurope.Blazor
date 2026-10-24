import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// OmniTileGrid.MinColumnWidth (applyTileGridMinWidth of omniInterop.js): the custom property is written
// through the CSSOM and the grid is marked while it has two real tracks or more. The tracks come from the
// resolved grid-template-columns, which these tests set by hand: what the browser makes of them is the
// business of the showcase.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const dom = installDom();
const { applyTileGridMinWidth } = await import(pathToFileURL(path.join(wwwroot, 'omniInterop.js')).href);

const MULTI = 'omni-tile-grid--multi-column';
const observers = [];

class FakeResizeObserver {
    constructor(callback) {
        this.callback = callback;
        this.targets = [];
        this.disconnected = false;
        observers.push(this);
    }

    observe(target) { this.targets.push(target); }
    disconnect() { this.disconnected = true; }
}

// A grid whose resolved tracks a test sets, in a page with a fake ResizeObserver.
const gridWith = tracks => {
    observers.length = 0;
    globalThis.ResizeObserver = FakeResizeObserver;
    const grid = dom.el('div');
    const state = { tracks };
    globalThis.getComputedStyle = element => ({ gridTemplateColumns: element === grid ? state.tracks : '' });
    return { grid, state };
};

test('the minimum is written as a custom property, trimmed, and never as a style attribute', () => {
    const { grid } = gridWith('300px 300px');

    applyTileGridMinWidth(grid, ' 18rem ');

    assert.equal(grid.style.getPropertyValue('--omni-tile-min'), '18rem');
    assert.equal(grid.hasAttribute('style'), false);
});

test('a grid with two real tracks or more is marked, one with a single track is not', () => {
    const { grid, state } = gridWith('322.656px 322.672px 322.656px');
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), true);

    state.tracks = '500px';
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), false);
});

test('the empty tracks a double tile opens in a grid of one column are not counted', () => {
    const { grid, state } = gridWith('484px 0px');

    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), false);

    state.tracks = '342px 342px 0px';
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), true);
});

test('a grid that is not laid out as a grid (hidden, or no tracks yet) is not marked', () => {
    const { grid, state } = gridWith('none');
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), false);

    state.tracks = '';
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(grid.classList.contains(MULTI), false);
});

test('the grid is watched once, and a resize measures it again', () => {
    const { grid, state } = gridWith('500px');
    applyTileGridMinWidth(grid, '18rem');
    applyTileGridMinWidth(grid, '20rem');

    assert.equal(observers.length, 1);
    assert.deepEqual(observers[0].targets, [grid]);
    assert.equal(grid.style.getPropertyValue('--omni-tile-min'), '20rem');
    assert.equal(grid.classList.contains(MULTI), false);

    state.tracks = '288px 288px';
    observers[0].callback([]);
    assert.equal(grid.classList.contains(MULTI), true);

    state.tracks = '590px';
    observers[0].callback([]);
    assert.equal(grid.classList.contains(MULTI), false);
});

test('calling again with the same value measures again, as a replaced class attribute needs', () => {
    const { grid } = gridWith('288px 288px');
    applyTileGridMinWidth(grid, '18rem');
    grid.classList.remove(MULTI);

    applyTileGridMinWidth(grid, '18rem');

    assert.equal(grid.classList.contains(MULTI), true);
    assert.equal(observers.length, 1);
});

test('no value removes the property and the mark, and stops watching the grid', () => {
    const { grid, state } = gridWith('288px 288px');
    applyTileGridMinWidth(grid, '18rem');

    applyTileGridMinWidth(grid, null);

    assert.equal(grid.style.getPropertyValue('--omni-tile-min'), '');
    assert.equal(grid.classList.contains(MULTI), false);
    assert.equal(observers[0].disconnected, true);

    // Watched afresh by the next value, with a new observer.
    state.tracks = '288px 288px';
    applyTileGridMinWidth(grid, '18rem');
    assert.equal(observers.length, 2);
    assert.equal(grid.classList.contains(MULTI), true);

    applyTileGridMinWidth(grid, '   ');
    assert.equal(grid.style.getPropertyValue('--omni-tile-min'), '');
    assert.equal(observers[1].disconnected, true);
});

test('removing the minimum of a grid never watched changes nothing and does not fail', () => {
    const { grid } = gridWith('288px 288px');

    assert.doesNotThrow(() => applyTileGridMinWidth(grid, undefined));
    assert.equal(observers.length, 0);
    assert.equal(grid.classList.contains(MULTI), false);
});

test('what is not an element (a stale reference after the grid left the page) is ignored', () => {
    gridWith('288px 288px');

    assert.doesNotThrow(() => applyTileGridMinWidth(null, '18rem'));
    assert.doesNotThrow(() => applyTileGridMinWidth({ style: {} }, '18rem'));
    assert.equal(observers.length, 0);
});

test('a browser without ResizeObserver still gets the property and a first measure', () => {
    const { grid } = gridWith('288px 288px');
    delete globalThis.ResizeObserver;

    applyTileGridMinWidth(grid, '18rem');

    assert.equal(grid.style.getPropertyValue('--omni-tile-min'), '18rem');
    assert.equal(grid.classList.contains(MULTI), true);
    assert.equal(observers.length, 0);
});
