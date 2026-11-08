import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The fit of the value of OmniStatTile (recette R-25): the value takes up to three smaller sizes
// (data-fit="shrink-1" to "shrink-3"), then wraps (data-fit="wrap"), and is never given a tooltip or an
// ellipsis. Widths are decided by hand here (a value's scrollWidth against its clientWidth, by the data-fit it
// carries): what the browser makes of the sizes is measured by the showcase probe, not by these tests.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const dom = installDom();
globalThis.MutationObserver = class { observe() { } disconnect() { } };
globalThis.ResizeObserver = class { observe() { } disconnect() { } };
const { attach, detach } = await import(pathToFileURL(path.join(wwwroot, 'omni-stat-tile.js')).href);

// A tile whose value needs `needed[step]` pixels at each data-fit step (key '' is no mark), in a box of `box`.
const tileNeeding = (box, needed, classes = 'omni-stat-tile') => {
    const value = dom.el('span', { class: 'omni-stat-tile__value', text: '08/11/2026 10:27' });
    const label = dom.el('span', { class: 'omni-stat-tile__label', text: 'Updated' });
    const tile = dom.el('div', { class: classes }, dom.el('span', { class: 'omni-stat-tile__text' }, value, label));
    value.rect.width = box;
    Object.defineProperty(value, 'scrollWidth', { get: () => needed[value.getAttribute('data-fit') ?? ''] ?? box });
    return { tile, value };
};

test('a value that fits keeps its size: no mark', () => {
    const { tile, value } = tileNeeding(200, { '': 120 });
    attach(tile);

    assert.equal(value.hasAttribute('data-fit'), false);
    detach(tile);
});

test('a value too wide takes the first size that fits', () => {
    const { tile, value } = tileNeeding(150, { '': 190, 'shrink-1': 160, 'shrink-2': 140 });
    attach(tile);

    assert.equal(value.getAttribute('data-fit'), 'shrink-2');
    detach(tile);
});

test('a value that does not fit at the smallest size wraps, and is given no tooltip', () => {
    const { tile, value } = tileNeeding(100, { '': 190, 'shrink-1': 160, 'shrink-2': 140, 'shrink-3': 120 });
    attach(tile);

    assert.equal(value.getAttribute('data-fit'), 'wrap');
    assert.equal(value.hasAttribute('title'), false);
    detach(tile);
});

test('the value is fitted even when the tile lets its title wrap', () => {
    const { tile, value } = tileNeeding(150, { '': 190, 'shrink-1': 140 }, 'omni-stat-tile omni-stat-tile--wrap');
    attach(tile);

    assert.equal(value.getAttribute('data-fit'), 'shrink-1');
    detach(tile);
});
