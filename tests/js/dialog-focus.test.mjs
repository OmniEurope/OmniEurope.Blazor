import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

test('a button disabled by its fieldset is out of the focus cycle', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const first = dom.el('button', { id: 'first' });
    const blocked = dom.el('button', { id: 'blocked' });
    const last = dom.el('button', { id: 'last' });
    const dialog = dom.el('div', { role: 'dialog' }, first, dom.el('fieldset', { disabled: '' }, blocked), last);
    dom.body.append(dialog);

    assert.deepEqual(focusableElements(dialog).map(element => element.getAttribute('id')), ['first', 'last']);
});

test('a button of an enabled fieldset stays in the focus cycle', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const inside = dom.el('button', { id: 'inside' });
    const dialog = dom.el('div', { role: 'dialog' }, dom.el('fieldset', {}, inside));
    dom.body.append(dialog);

    assert.deepEqual(focusableElements(dialog).map(element => element.getAttribute('id')), ['inside']);
});
