import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

// The video tile script (omni-media-tile.js): the rectangle the video picture occupies in its box (contain and cover,
// landscape and portrait videos in landscape and portrait boxes), the export size cap, and the module on a stand-in tile
// (the stream set and cleared, the rectangle written as custom properties on the overlay and removed without a picture,
// the full-screen state told to .NET, the export refused without a frame). What a real video decodes, where the browser puts
// the picture and the pixels of an export stay with the showcase probe (eng/scripts-probe/media-tile.mjs).
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const mediaTile = await import(pathToFileURL(path.join(wwwroot, 'omni-media-tile.js')).href);
const { pictureRect, exportSize } = mediaTile;

const close = (actual, expected) => assert.ok(Math.abs(actual - expected) < 1e-9, `${actual} is not ${expected}`);
const assertRect = (actual, expected) => {
    close(actual.left, expected.left);
    close(actual.top, expected.top);
    close(actual.width, expected.width);
    close(actual.height, expected.height);
};

// ----- pictureRect -----------------------------------------------------------------------------------------------

test('contain: a 4:3 video in a 16:9 box keeps its height and leaves bands on the sides', () => {
    assertRect(pictureRect(1600, 900, 640, 480, 'contain'), { left: 200, top: 0, width: 1200, height: 900 });
});

test('cover: a 4:3 video in a 16:9 box keeps its width and overflows above and below', () => {
    assertRect(pictureRect(1600, 900, 640, 480, 'cover'), { left: 0, top: -150, width: 1600, height: 1200 });
});

test('contain: a 16:9 video in a 16:9 box fills it, whatever the size', () => {
    assertRect(pictureRect(320, 180, 1920, 1080, 'contain'), { left: 0, top: 0, width: 320, height: 180 });
});

test('contain: a landscape video in a portrait box has bands above and below', () => {
    assertRect(pictureRect(300, 600, 1600, 900, 'contain'), { left: 0, top: 215.625, width: 300, height: 168.75 });
});

test('cover: a landscape video in a portrait box overflows on the sides', () => {
    assertRect(pictureRect(300, 600, 1600, 900, 'cover'), { left: -(1066.6666666666667 - 300) / 2, top: 0, width: 1066.6666666666667, height: 600 });
});

test('contain: a portrait video in a landscape box has bands on the sides', () => {
    assertRect(pictureRect(800, 450, 720, 1280, 'contain'), { left: (800 - 253.125) / 2, top: 0, width: 253.125, height: 450 });
});

test('cover: a portrait video in a landscape box overflows above and below', () => {
    assertRect(pictureRect(800, 450, 720, 1280, 'cover'), { left: 0, top: (450 - 1422.2222222222222) / 2, width: 800, height: 1422.2222222222222 });
});

test('contain and cover agree when the video and the box have the same ratio', () => {
    assertRect(pictureRect(400, 400, 1080, 1080, 'contain'), pictureRect(400, 400, 1080, 1080, 'cover'));
});

test('contain is the fit of anything that is not cover', () => {
    assertRect(pictureRect(1600, 900, 640, 480, undefined), pictureRect(1600, 900, 640, 480, 'contain'));
    assertRect(pictureRect(1600, 900, 640, 480, 'fill'), pictureRect(1600, 900, 640, 480, 'contain'));
});

test('the picture is centred: the bands on both sides are equal, and so are the cut-off parts', () => {
    const contain = pictureRect(1000, 500, 300, 300, 'contain');
    close(contain.left, 1000 - contain.left - contain.width);
    const cover = pictureRect(1000, 500, 300, 300, 'cover');
    close(cover.top, 500 - cover.top - cover.height);
});

test('there is no rectangle while a size is not above zero', () => {
    assert.equal(pictureRect(0, 900, 640, 480, 'contain'), null);
    assert.equal(pictureRect(1600, 0, 640, 480, 'contain'), null);
    assert.equal(pictureRect(1600, 900, 0, 480, 'contain'), null);
    assert.equal(pictureRect(1600, 900, 640, 0, 'cover'), null);
    assert.equal(pictureRect(Number.NaN, 900, 640, 480, 'cover'), null);
});

// ----- exportSize ------------------------------------------------------------------------------------------------

test('the export keeps the native size up to 8192 on the longest side', () => {
    assert.deepEqual(exportSize(640, 480), { width: 640, height: 480 });
    assert.deepEqual(exportSize(8192, 4000), { width: 8192, height: 4000 });
});

test('the export scales a larger frame down with its ratio so the longest side is 8192', () => {
    assert.deepEqual(exportSize(16384, 8192), { width: 8192, height: 4096 });
    assert.deepEqual(exportSize(4000, 12288), { width: 2667, height: 8192 });
});

test('the export never has a side under one pixel', () => {
    assert.deepEqual(exportSize(100000, 1), { width: 8192, height: 1 });
});

// ----- The module on a stand-in tile -----------------------------------------------------------------------------

class FakeOverlay {
    properties = new Map();
    style = {
        setProperty: (name, value) => this.properties.set(name, value),
        removeProperty: name => this.properties.delete(name)
    };
}

const makeTile = ({ boxWidth = 1600, boxHeight = 900, videoWidth = 640, videoHeight = 480, fit = 'contain' } = {}) => {
    const listeners = {};
    const video = {
        srcObject: null,
        muted: true,
        videoWidth,
        videoHeight,
        readyState: 4,
        played: 0,
        addEventListener: (type, listener) => { listeners[type] = listener; },
        removeEventListener: type => { delete listeners[type]; },
        getBoundingClientRect: () => ({ width: boxWidth, height: boxHeight }),
        play() { this.played++; return Promise.resolve(); }
    };
    const overlay = new FakeOverlay();
    const root = {
        dataset: { omniMediaFit: fit },
        querySelector: selector => (selector === 'video' ? video : selector === '[data-omni-media-picture]' ? overlay : null),
        withoutOverlay() { this.querySelector = selector => (selector === 'video' ? video : null); }
    };
    return { root, video, overlay, listeners };
};

const installPage = () => {
    const state = { observed: [], disconnected: 0, documentListeners: {}, fullscreenElement: null };
    globalThis.ResizeObserver = class {
        constructor(callback) { state.callback = callback; }
        observe(element) { state.observed.push(element); }
        disconnect() { state.disconnected++; }
    };
    globalThis.document = {
        get fullscreenElement() { return state.fullscreenElement; },
        addEventListener: (type, listener) => { state.documentListeners[type] = listener; },
        removeEventListener: type => { delete state.documentListeners[type]; },
        exitFullscreen: async () => { state.fullscreenElement = null; }
    };
    return state;
};

const dotnetOf = told => ({ invokeMethodAsync: (method, ...values) => { told.push([method, ...values]); return Promise.resolve(); } });

test('setStream sets the stream as srcObject, plays it and places the overlay on the picture', () => {
    installPage();
    const { root, video, overlay } = makeTile();
    mediaTile.attach(root, dotnetOf([]));
    const stream = { id: 'stream' };

    mediaTile.setStream(root, stream);

    assert.equal(video.srcObject, stream);
    assert.equal(video.played, 1);
    assert.equal(overlay.properties.get('--omni-media-picture-left'), '200px');
    assert.equal(overlay.properties.get('--omni-media-picture-top'), '0px');
    assert.equal(overlay.properties.get('--omni-media-picture-width'), '1200px');
    assert.equal(overlay.properties.get('--omni-media-picture-height'), '900px');
});

test('cover writes the whole picture, overflowing the box', () => {
    installPage();
    const { root, overlay } = makeTile({ fit: 'cover' });
    mediaTile.attach(root, dotnetOf([]));

    mediaTile.setStream(root, {});

    assert.equal(overlay.properties.get('--omni-media-picture-top'), '-150px');
    assert.equal(overlay.properties.get('--omni-media-picture-height'), '1200px');
});

test('clearing the stream removes the rectangle: the overlay has no size without a picture', () => {
    installPage();
    const { root, video, overlay } = makeTile();
    mediaTile.attach(root, dotnetOf([]));
    mediaTile.setStream(root, {});

    mediaTile.setStream(root, null);

    assert.equal(video.srcObject, null);
    assert.equal(overlay.properties.size, 0);
});

test('a video without a size yet has no rectangle, and the metadata event places the overlay', () => {
    installPage();
    const { root, video, overlay, listeners } = makeTile({ videoWidth: 0, videoHeight: 0 });
    mediaTile.attach(root, dotnetOf([]));
    mediaTile.setStream(root, {});
    assert.equal(overlay.properties.size, 0);

    video.videoWidth = 640;
    video.videoHeight = 480;
    listeners.loadedmetadata();

    assert.equal(overlay.properties.get('--omni-media-picture-width'), '1200px');
});

test('a change of the video size (resize event) and of the box (observer) places the overlay again', () => {
    const page = installPage();
    const { root, video, overlay, listeners } = makeTile({ boxWidth: 800, boxHeight: 450 });
    mediaTile.attach(root, dotnetOf([]));
    mediaTile.setStream(root, {});
    assert.equal(overlay.properties.get('--omni-media-picture-width'), '600px');

    video.videoWidth = 1280;
    video.videoHeight = 720;
    listeners.resize();
    assert.equal(overlay.properties.get('--omni-media-picture-width'), '800px');

    video.getBoundingClientRect = () => ({ width: 400, height: 400 });
    page.callback();
    assert.equal(overlay.properties.get('--omni-media-picture-width'), '400px');
    assert.equal(overlay.properties.get('--omni-media-picture-height'), '225px');
    assert.deepEqual(page.observed, [video]);
});

test('refresh places the overlay again after the fit changed, and does nothing without an overlay', () => {
    installPage();
    const tile = makeTile();
    mediaTile.attach(tile.root, dotnetOf([]));
    mediaTile.setStream(tile.root, {});
    tile.root.dataset.omniMediaFit = 'cover';

    mediaTile.refresh(tile.root);
    assert.equal(tile.overlay.properties.get('--omni-media-picture-height'), '1200px');

    tile.root.withoutOverlay();
    assert.doesNotThrow(() => mediaTile.refresh(tile.root));
});

test('setMuted sets the muted state of the video', () => {
    installPage();
    const { root, video } = makeTile();
    mediaTile.attach(root, dotnetOf([]));

    mediaTile.setMuted(root, false);
    assert.equal(video.muted, false);
    mediaTile.setMuted(root, true);
    assert.equal(video.muted, true);
});

test('a refused play waits for a press or a key and retries once', async () => {
    const page = installPage();
    const { root, video } = makeTile();
    mediaTile.attach(root, dotnetOf([]));
    let attempts = 0;
    video.play = () => { attempts++; return attempts === 1 ? Promise.reject(Object.assign(new Error('blocked'), { name: 'NotAllowedError' })) : Promise.resolve(); };

    mediaTile.setStream(root, {});
    await new Promise(resolve => setImmediate(resolve));
    assert.ok(page.documentListeners.pointerdown && page.documentListeners.keydown);

    page.documentListeners.pointerdown();
    assert.equal(attempts, 2);
    assert.equal(page.documentListeners.pointerdown, undefined);
    assert.equal(page.documentListeners.keydown, undefined);
});

test('the full-screen state is told to .NET at each change, and only when it changes', () => {
    const page = installPage();
    const { root } = makeTile();
    const told = [];
    mediaTile.attach(root, dotnetOf(told));

    page.documentListeners.fullscreenchange();
    assert.deepEqual(told, []);

    page.fullscreenElement = root;
    page.documentListeners.fullscreenchange();
    page.documentListeners.fullscreenchange();
    page.fullscreenElement = null;
    page.documentListeners.fullscreenchange();

    assert.deepEqual(told, [['OnFullscreenChanged', true], ['OnFullscreenChanged', false]]);
});

test('toggleFullscreen enters full screen, leaves it, and reports a refusal by its result', async () => {
    const page = installPage();
    const { root } = makeTile();
    root.requestFullscreen = async () => { page.fullscreenElement = root; };

    assert.equal(await mediaTile.toggleFullscreen(root), true);
    assert.equal(await mediaTile.toggleFullscreen(root), false);

    root.requestFullscreen = async () => { throw new Error('not allowed'); };
    assert.equal(await mediaTile.toggleFullscreen(root), false);

    delete root.requestFullscreen;
    assert.equal(await mediaTile.toggleFullscreen(root), false);
});

test('detach clears the stream, stops observing and forgets the tile', () => {
    const page = installPage();
    const { root, video, listeners } = makeTile();
    mediaTile.attach(root, dotnetOf([]));
    mediaTile.setStream(root, {});

    mediaTile.detach(root);

    assert.equal(video.srcObject, null);
    assert.equal(page.disconnected, 1);
    assert.deepEqual(Object.keys(listeners), []);
    assert.equal(page.documentListeners.fullscreenchange, undefined);
    mediaTile.setStream(root, {});
    assert.equal(video.srcObject, null);
});

test('attaching twice to the same tile attaches once', () => {
    const page = installPage();
    const { root } = makeTile();

    mediaTile.attach(root, dotnetOf([]));
    mediaTile.attach(root, dotnetOf([]));

    assert.equal(page.observed.length, 1);
});

test('the export gives nothing while there is no stream or no decoded frame', async () => {
    installPage();
    const { root, video } = makeTile();
    mediaTile.attach(root, dotnetOf([]));
    assert.equal(await mediaTile.exportPng(root, [], [], []), null);

    mediaTile.setStream(root, {});
    video.readyState = 1;
    assert.equal(await mediaTile.exportPng(root, [], [], []), null);

    video.readyState = 4;
    video.videoWidth = 0;
    assert.equal(await mediaTile.exportPng(root, [], [], []), null);
    assert.equal(await mediaTile.exportPng({}, [], [], []), null);
});

test('the export draws the frame at its native size, then the strokes with the shared renderer, and encodes a PNG', async () => {
    installPage();
    const { root, video } = makeTile({ videoWidth: 640, videoHeight: 480 });
    mediaTile.attach(root, dotnetOf([]));
    mediaTile.setStream(root, {});
    const calls = [];
    const context = {
        drawImage: (...values) => calls.push(['drawImage', values[0] === video, ...values.slice(1)]),
        beginPath: () => calls.push(['beginPath']),
        moveTo: (...values) => calls.push(['moveTo', ...values]),
        lineTo: (...values) => calls.push(['lineTo', ...values]),
        quadraticCurveTo: () => { },
        arc: () => { },
        stroke() { calls.push(['stroke', this.strokeStyle, this.lineWidth]); },
        fill: () => { }
    };
    const canvas = { getContext: () => context, toBlob: callback => callback({ arrayBuffer: async () => Uint8Array.of(137, 80, 78, 71).buffer }) };
    globalThis.document.createElement = tag => { assert.equal(tag, 'canvas'); return canvas; };

    const png = await mediaTile.exportPng(root, ['currentColor'], [6], [[0, 0, -1, 1, 1, -1]]);

    assert.deepEqual([canvas.width, canvas.height], [640, 480]);
    assert.deepEqual(calls[0], ['drawImage', true, 0, 0, 640, 480]);
    // A stroke in the theme colour is black in an export; its width is 6 / 600 of the 640 pixels of the frame.
    assert.deepEqual(calls.find(call => call[0] === 'stroke'), ['stroke', '#000000', 6 * (640 / 600)]);
    assert.deepEqual([...png], [137, 80, 78, 71]);
});
