import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

class FakeElement {
    constructor(cell = null) {
        this.cell = cell;
        this.attributes = new Map();
        this.writes = 0;
        this.properties = new Map();
        this.style = {
            setProperty: (name, value) => this.properties.set(name, value),
            removeProperty: name => this.properties.delete(name)
        };
    }

    getAttribute(name) {
        return this.attributes.get(name) ?? null;
    }

    setAttribute(name, value) {
        this.writes++;
        this.attributes.set(name, value);
    }

    querySelector(selector) {
        return selector === 'td' ? this.cell : null;
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.document = { addEventListener: () => {}, removeEventListener: () => {} };
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const { applyLayout } = await import(pathToFileURL(path.join(wwwroot, 'omni-grid.js')).href);

const viewportWith = (top, bottom) => {
    const viewport = new FakeElement();
    viewport.querySelector = selector => (selector.includes('"top"') ? top : bottom);
    return viewport;
};

test('a grid spacer follows a height that moved and leaves an equal one alone', () => {
    const topCell = new FakeElement();
    const bottomCell = new FakeElement();
    topCell.attributes.set('height', '120.5');
    bottomCell.attributes.set('height', '4000');
    const viewport = viewportWith(new FakeElement(topCell), new FakeElement(bottomCell));

    applyLayout(viewport, 120.5, 4000, null, null);
    assert.equal(topCell.writes + bottomCell.writes, 0);

    applyLayout(viewport, 160, -1, null, null);
    assert.equal(topCell.getAttribute('height'), '160');
    assert.equal(bottomCell.getAttribute('height'), '0');
    assert.equal(topCell.writes + bottomCell.writes, 2);
});

test('a log viewer spacer, a block without a cell, stays sized by its custom property', () => {
    const top = new FakeElement();
    const bottom = new FakeElement();

    applyLayout(viewportWith(top, bottom), 80, 0, null, null);

    assert.equal(top.properties.get('--omni-grid-spacer'), '80px');
    assert.equal(bottom.properties.get('--omni-grid-spacer'), '0px');
});
