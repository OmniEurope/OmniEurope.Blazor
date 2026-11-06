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

const ids = elements => elements.map(element => element.getAttribute('id'));

test('a closed details takes one tab stop, its summary, and none for the fields of its panel', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const before = dom.el('button', { id: 'before' });
    const summary = dom.el('summary', { id: 'summary' });
    const filter = dom.el('input', { id: 'filter', type: 'search' });
    const option = dom.el('input', { id: 'option', type: 'checkbox' });
    const after = dom.el('button', { id: 'after' });
    const dialog = dom.el('div', { role: 'dialog' }, before, dom.el('details', {}, summary, dom.el('div', {}, filter, option)), after);
    dom.body.append(dialog);

    assert.deepEqual(ids(focusableElements(dialog)), ['before', 'summary', 'after']);
});

test('an open details keeps its summary and the fields of its panel in the focus cycle', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const dialog = dom.el('div', { role: 'dialog' },
        dom.el('details', { open: '' }, dom.el('summary', { id: 'summary' }), dom.el('div', {}, dom.el('input', { id: 'filter' }), dom.el('input', { id: 'option' }))),
        dom.el('button', { id: 'after' }));
    dom.body.append(dialog);

    assert.deepEqual(ids(focusableElements(dialog)), ['summary', 'filter', 'option', 'after']);
});

test('what an open details holds is out of the cycle while a details around it is closed', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const inner = dom.el('details', { open: '' }, dom.el('summary', { id: 'inner-summary' }), dom.el('input', { id: 'inner-field' }));
    const dialog = dom.el('div', { role: 'dialog' },
        dom.el('details', {}, dom.el('summary', { id: 'outer-summary' }), inner),
        dom.el('button', { id: 'after' }));
    dom.body.append(dialog);

    assert.deepEqual(ids(focusableElements(dialog)), ['outer-summary', 'after']);
});

test('a summary that is not the first of its details, or a disabled one, is not a tab stop', async () => {
    const dom = installDom();
    const { focusableElements } = await import(pathToFileURL(path.join(wwwroot, 'focus/dialog.js')).href);
    const dialog = dom.el('div', { role: 'dialog' },
        dom.el('details', { open: '' }, dom.el('summary', { id: 'first' }), dom.el('summary', { id: 'second' })),
        dom.el('details', {}, dom.el('summary', { id: 'disabled', tabindex: '-1' })));
    dom.body.append(dialog);

    assert.deepEqual(ids(focusableElements(dialog)), ['first']);
});
