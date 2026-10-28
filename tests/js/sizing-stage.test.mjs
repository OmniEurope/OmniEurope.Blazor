import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const { sizingStage } = await import(pathToFileURL(path.join(wwwroot, 'grid/content-sizing.js')).href);

test('a grid loading its first rows shows its headers only', () => {
    // Not shown and loading: the headers are measured on their own titles, the rows wait.
    assert.equal(sizingStage(false, true), 'headed');
});

test('a grid whose first rows are in is sized whole and shown', () => {
    assert.equal(sizingStage(false, false), 'sized');
});

test('a grid already shown is sized whole whatever it loads, so a reload never hides its rows again', () => {
    assert.equal(sizingStage(true, true), 'sized');
    assert.equal(sizingStage(true, false), 'sized');
});
