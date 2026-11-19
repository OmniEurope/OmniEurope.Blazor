import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The horizontal arrows of the components that handle them in .NET (OmniSpreadsheet, OmniTree) follow the
// direction of the page: a capturing listener swaps the two keys on the event before Blazor, which reads the
// event once it reaches the document, builds the arguments of the .NET handler from it.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

// A keydown as the browser makes it: `key` is an accessor of the prototype, not a property of the instance.
class KeyboardEventStub {
    #key;
    constructor(key, target) {
        this.#key = key;
        this.target = target;
        this.isComposing = false;
        this.prevented = false;
    }

    get key() { return this.#key; }
    preventDefault() { this.prevented = true; }
}

const rootOf = (dom, direction, ...children) => {
    globalThis.getComputedStyle = () => ({ direction });
    const root = dom.el('div', {}, ...children);
    dom.body.append(root);
    const handlers = [];
    root.addEventListener = (type, handler, capture) => handlers.push({ type, handler, capture });
    root.removeEventListener = (type, handler) => handlers.splice(0, handlers.length, ...handlers.filter(item => item.handler !== handler));
    // The key .NET would receive: read after every capturing listener of the root has run, as Blazor does.
    const press = (target, key) => {
        const event = new KeyboardEventStub(key, target);
        handlers.filter(item => item.type === 'keydown' && item.capture).forEach(item => item.handler(event));
        return { seenByDotNet: event.key, prevented: event.prevented };
    };
    return { root, press, handlers };
};

test('a left-to-right page: the arrows reach .NET as pressed', async () => {
    const { mirrorSideKeys } = await import(pathToFileURL(path.join(wwwroot, 'shared/reading-direction.js')).href);
    const dom = installDom();
    const { root } = rootOf(dom, 'ltr');
    for (const key of ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Enter']) {
        const event = new KeyboardEventStub(key, root);
        mirrorSideKeys(event, root);
        assert.equal(event.key, key);
    }
});

test('a right-to-left page: the two side arrows swap, nothing else does, and a key is swapped once', async () => {
    const { mirrorSideKeys } = await import(pathToFileURL(path.join(wwwroot, 'shared/reading-direction.js')).href);
    const dom = installDom();
    const { root } = rootOf(dom, 'rtl');
    const left = new KeyboardEventStub('ArrowLeft', root);
    mirrorSideKeys(left, root);
    mirrorSideKeys(left, root);
    assert.equal(left.key, 'ArrowRight', 'a second component on the way must not undo the swap');
    const right = new KeyboardEventStub('ArrowRight', root);
    mirrorSideKeys(right, root);
    assert.equal(right.key, 'ArrowLeft');
    for (const key of ['ArrowUp', 'ArrowDown', 'Home', 'Enter']) {
        const event = new KeyboardEventStub(key, root);
        mirrorSideKeys(event, root);
        assert.equal(event.key, key);
    }
});

test('spreadsheet, right to left: ArrowLeft reaches .NET as the key of the next column, ArrowRight as the previous one', async () => {
    const dom = installDom();
    const grid = dom.el('div', { 'data-omni-sheet-grid': '' });
    const editor = dom.el('input', { 'data-omni-sheet-editor': '', 'data-omni-sheet-mode': 'replace' });
    const { root, press } = rootOf(dom, 'rtl', grid, editor);
    const { attach } = await import(pathToFileURL(path.join(wwwroot, 'omni-spreadsheet.js')).href);
    attach(root);

    assert.deepEqual(press(grid, 'ArrowLeft'), { seenByDotNet: 'ArrowRight', prevented: true });
    assert.deepEqual(press(grid, 'ArrowRight'), { seenByDotNet: 'ArrowLeft', prevented: true });
    assert.equal(press(grid, 'ArrowDown').seenByDotNet, 'ArrowDown');
    assert.equal(press(editor, 'ArrowLeft').seenByDotNet, 'ArrowRight', 'typing over a cell: the arrow commits and moves the same way');
});

test('spreadsheet, left to right: the arrows reach .NET as pressed', async () => {
    const dom = installDom();
    const grid = dom.el('div', { 'data-omni-sheet-grid': '' });
    const { root, press } = rootOf(dom, 'ltr', grid);
    const { attach } = await import(pathToFileURL(path.join(wwwroot, 'omni-spreadsheet.js')).href);
    attach(root);

    assert.deepEqual(press(grid, 'ArrowLeft'), { seenByDotNet: 'ArrowLeft', prevented: true });
    assert.deepEqual(press(grid, 'ArrowRight'), { seenByDotNet: 'ArrowRight', prevented: true });
});

test('tree, right to left: ArrowLeft reaches .NET as ArrowRight (expand) and ArrowRight as ArrowLeft (collapse)', async () => {
    const dom = installDom();
    const item = dom.el('div', { role: 'treeitem' });
    const { root, press } = rootOf(dom, 'rtl', item);
    const { attachTreeKeys, detachTreeKeys } = await import(pathToFileURL(path.join(wwwroot, 'omni-tree.js')).href);
    attachTreeKeys(root);
    attachTreeKeys(root);

    assert.equal(press(item, 'ArrowLeft').seenByDotNet, 'ArrowRight');
    assert.equal(press(item, 'ArrowRight').seenByDotNet, 'ArrowLeft');
    assert.equal(press(item, 'ArrowDown').seenByDotNet, 'ArrowDown');
    detachTreeKeys(root);
    assert.equal(press(item, 'ArrowLeft').seenByDotNet, 'ArrowLeft', 'detached: the key is left alone');
});

test('tree, left to right: the arrows reach .NET as pressed', async () => {
    const dom = installDom();
    const item = dom.el('div', { role: 'treeitem' });
    const { root, press } = rootOf(dom, 'ltr', item);
    const { attachTreeKeys } = await import(pathToFileURL(path.join(wwwroot, 'omni-tree.js')).href);
    attachTreeKeys(root);

    assert.equal(press(item, 'ArrowRight').seenByDotNet, 'ArrowRight');
    assert.equal(press(item, 'ArrowLeft').seenByDotNet, 'ArrowLeft');
});
