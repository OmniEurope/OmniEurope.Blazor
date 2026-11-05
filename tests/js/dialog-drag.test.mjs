import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { installDom } from './dom-stub.mjs';

// The drag of OmniDialog (omni-dialog.js): the dialog is marked ready once its header listens, so the
// stylesheet shows the move cursor only when a press would be heard.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

async function load() {
    const dom = installDom();
    const script = await import(pathToFileURL(path.join(wwwroot, 'omni-dialog.js')).href);
    const handle = dom.el('header', { 'data-omni-dialog-drag-handle': '' });
    const dialog = dom.el('section', { role: 'dialog' }, handle);
    dom.body.append(dialog);
    return { script, dialog };
}

test('a dialog is not drag-ready before attach, is after it, and is not after detach', async () => {
    const { script, dialog } = await load();

    assert.equal(dialog.hasAttribute('data-omni-dialog-drag-ready'), false);
    script.attach(dialog);
    assert.equal(dialog.hasAttribute('data-omni-dialog-drag-ready'), true);
    script.detach(dialog);
    assert.equal(dialog.hasAttribute('data-omni-dialog-drag-ready'), false);
});

test('a dialog without a drag handle is never marked ready', async () => {
    const dom = installDom();
    const script = await import(pathToFileURL(path.join(wwwroot, 'omni-dialog.js')).href);
    const dialog = dom.el('section', { role: 'dialog' });
    dom.body.append(dialog);

    script.attach(dialog);
    assert.equal(dialog.hasAttribute('data-omni-dialog-drag-ready'), false);
});
