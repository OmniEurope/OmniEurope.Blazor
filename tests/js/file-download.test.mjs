import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

// The hand-over of a file (OmniFileDownloader, the grid's export bar): a Blob behind an object URL that a
// hidden link follows, the URL revoked afterwards. The browser is stubbed by hand: this proves the sequence
// and the arguments, the real download stays the business of the showcase.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const links = [];
const revoked = [];
const created = [];
const timers = [];
globalThis.window = { setTimeout: callback => timers.push(callback) };
globalThis.URL.createObjectURL = blob => {
    created.push(blob);
    return 'blob:test';
};
globalThis.URL.revokeObjectURL = url => revoked.push(url);
globalThis.document = {
    body: { appendChild: link => { link.attached = true; } },
    createElement: () => {
        const link = { clicked: false, attached: false, remove() { this.attached = false; }, click() { this.clicked = this.attached; } };
        links.push(link);
        return link;
    }
};
const { download, downloadStream } = await import(pathToFileURL(path.join(wwwroot, 'omni-document-editor.js')).href);

test('download follows a hidden link to the blob, named and typed, then revokes the url', () => {
    download('a.txt', 'text/plain', new Uint8Array([104, 105]));

    const link = links.at(-1);
    assert.equal(link.href, 'blob:test');
    assert.equal(link.download, 'a.txt');
    assert.equal(link.hidden, true);
    assert.equal(link.clicked, true);
    assert.equal(link.attached, false);
    assert.equal(created.at(-1).type, 'text/plain');
    assert.equal(created.at(-1).size, 2);
    assert.deepEqual(revoked, []);
    timers.shift()();
    assert.deepEqual(revoked, ['blob:test']);
});

test('downloadStream reads the stream whole, then downloads it as download does', async () => {
    const bytes = new Uint8Array([1, 2, 3, 4, 5]).buffer;
    await downloadStream('big.bin', 'application/x-test', { arrayBuffer: async () => bytes });

    const link = links.at(-1);
    assert.equal(link.download, 'big.bin');
    assert.equal(link.clicked, true);
    assert.equal(created.at(-1).type, 'application/x-test');
    assert.equal(created.at(-1).size, 5);
});
