import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

// The drawing pad script (omni-ink-canvas.js): pointer to stroke, stroke to .NET, strokes from .NET to
// pixels, the keys, the size and the PNG export. It runs against a canvas, a 2D context and observers made of
// the few members the script reads, and the context writes down what it was asked to draw. What the browser
// makes of the pixels and the real events stays with the showcase probe (eng/scripts-probe/ink.mjs).
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');

class FakeContext {
    log = [];
    strokeStyle = '';
    fillStyle = '';
    lineWidth = 1;
    lineCap = '';
    lineJoin = '';

    setTransform(...values) { this.log.push(['setTransform', ...values]); }
    clearRect(...values) { this.log.push(['clearRect', ...values]); }
    beginPath() { this.log.push(['beginPath']); }
    moveTo(...values) { this.log.push(['moveTo', ...values]); }
    lineTo(...values) { this.log.push(['lineTo', ...values]); }
    quadraticCurveTo(...values) { this.log.push(['curve', ...values]); }
    arc(...values) { this.log.push(['arc', ...values]); }
    stroke() { this.log.push(['stroke', this.strokeStyle, this.lineWidth]); }
    fill() { this.log.push(['fill', this.fillStyle]); }
    of(name) { return this.log.filter(entry => entry[0] === name); }
}

let observers;
let mediaListeners;
let documentCanvases;

const installPage = () => {
    observers = [];
    mediaListeners = [];
    documentCanvases = [];
    globalThis.window = {
        devicePixelRatio: 2,
        matchMedia: () => ({
            addEventListener: (type, listener) => mediaListeners.push(listener),
            removeEventListener: (type, listener) => { mediaListeners = mediaListeners.filter(item => item !== listener); }
        })
    };
    globalThis.getComputedStyle = () => ({ color: 'rgb(1, 2, 3)' });
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; this.disconnected = false; observers.push(this); }
        observe(target) { this.target = target; }
        disconnect() { this.disconnected = true; }
    };
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; this.disconnected = false; observers.push(this); }
        observe(target, options) { this.target = target; this.options = options; }
        disconnect() { this.disconnected = true; }
    };
    globalThis.document = {
        documentElement: { name: 'root' },
        createElement: tag => {
            assert.equal(tag, 'canvas');
            const canvas = { width: 0, height: 0, context: new FakeContext(), blob: undefined };
            canvas.getContext = () => canvas.context;
            canvas.toBlob = (callback, type) => {
                canvas.type = type;
                callback(canvas.blob === undefined ? { arrayBuffer: async () => new Uint8Array([137, 80, 78, 71]).buffer } : canvas.blob);
            };
            documentCanvases.push(canvas);
            return canvas;
        }
    };
};

installPage();
const ink = await import(pathToFileURL(path.join(wwwroot, 'omni-ink-canvas.js')).href);

// A canvas 600 by 200 CSS pixels whose box starts at (10, 20), and the .NET side that records what it is told.
const pad = ({ width = 600, height = 200, locked = false, color = 'currentColor', penWidth = '2', scope = null } = {}) => {
    installPage();
    const listeners = new Map();
    const context = new FakeContext();
    const dotnet = { calls: [], invokeMethodAsync(method, ...values) { this.calls.push([method, ...values]); return Promise.resolve(); } };
    const canvas = {
        clientWidth: width,
        clientHeight: height,
        width: 0,
        height: 0,
        dataset: { omniInkColor: color, omniInkWidth: penWidth, ...(locked ? { omniInkLocked: 'true' } : {}) },
        context,
        listeners,
        focused: false,
        captured: null,
        addEventListener: (type, listener) => listeners.set(type, listener),
        removeEventListener: (type, listener) => { if (listeners.get(type) === listener) listeners.delete(type); },
        getBoundingClientRect: () => ({ left: 10, top: 20, width, height }),
        getContext: () => context,
        closest: () => scope,
        setPointerCapture(id) { this.captured = id; },
        focus(options) { this.focused = true; this.focusOptions = options; }
    };
    ink.attach(canvas, dotnet);
    const fire = (type, event) => {
        const prevented = { value: false };
        listeners.get(type)?.({ type, preventDefault: () => { prevented.value = true; }, ...event });
        return prevented.value;
    };
    return { canvas, dotnet, context, fire, listeners };
};

const pointer = (x, y, extra = {}) => ({ pointerId: 1, isPrimary: true, pointerType: 'mouse', button: 0, clientX: 10 + x, clientY: 20 + y, pressure: 0.5, ...extra });

test('a mouse stroke is drawn as it goes and reported to .NET when the pointer is released', () => {
    const { canvas, dotnet, context, fire } = pad();

    assert.equal(fire('pointerdown', pointer(0, 0)), true);
    assert.equal(canvas.focused, true);
    assert.deepEqual(canvas.focusOptions, { preventScroll: true });
    assert.equal(canvas.captured, 1);
    fire('pointermove', pointer(300, 100));
    fire('pointermove', pointer(600, 200));
    assert.equal(context.of('stroke').length > 0, true);
    assert.equal(canvas.width, 1200);
    assert.equal(canvas.height, 400);
    fire('pointerup', pointer(600, 200));

    assert.deepEqual(dotnet.calls, [['OnStroke', 'currentColor', 2, [0, 0, -1, 0.5, 0.5, -1, 1, 1, -1]]]);
});

test('the theme colour is resolved from the canvas, not drawn as a keyword', () => {
    const { context, fire } = pad();

    fire('pointerdown', pointer(0, 0));
    fire('pointermove', pointer(60, 60));

    assert.equal(context.of('stroke').at(-1)[1], 'rgb(1, 2, 3)');
});

test('a click is a dot, drawn as a filled circle of the stroke width', () => {
    const { dotnet, context, fire } = pad({ color: '#ff0000', penWidth: '6' });

    fire('pointerdown', pointer(300, 100));
    assert.deepEqual(context.of('arc').at(-1), ['arc', 300, 100, 3, 0, Math.PI * 2]);
    assert.equal(context.of('fill').at(-1)[1], '#ff0000');
    fire('pointerup', pointer(300, 100));

    assert.deepEqual(dotnet.calls, [['OnStroke', '#ff0000', 6, [0.5, 0.5, -1]]]);
});

test('a pen reports its pressure, which thickens the line stretch by stretch', () => {
    const { dotnet, context, fire } = pad({ penWidth: '4' });

    fire('pointerdown', pointer(0, 0, { pointerType: 'pen', pressure: 0.2 }));
    fire('pointermove', pointer(100, 0, { pointerType: 'pen', pressure: 0.8 }));
    fire('pointerup', pointer(100, 0, { pointerType: 'pen', pressure: 0.8 }));

    assert.deepEqual(dotnet.calls[0].slice(0, 3), ['OnStroke', 'currentColor', 4]);
    assert.deepEqual(dotnet.calls[0][3], [0, 0, 0.2, 0.16667, 0, 0.8]);
    // The width at the reference width is the stroke width; the pressure of the stretch is the mean 0.5.
    assert.equal(context.of('stroke').at(-1)[2], 4);
});

test('a pressure-less stroke past the first segments is one smooth curve', () => {
    const { context, fire } = pad();

    fire('pointerdown', pointer(0, 0));
    fire('pointermove', pointer(100, 50));
    fire('pointermove', pointer(200, 0));
    fire('pointermove', pointer(300, 50));

    assert.equal(context.of('curve').length > 0, true);
});

test('samples closer than half a pixel are dropped, and coalesced samples are all kept', () => {
    const { dotnet, fire } = pad();

    fire('pointerdown', pointer(0, 0));
    fire('pointermove', pointer(0.2, 0));
    fire('pointermove', { ...pointer(30, 0), getCoalescedEvents: () => [pointer(10, 0), pointer(20, 0), pointer(30, 0)] });
    fire('pointerup', pointer(30, 0));

    assert.deepEqual(dotnet.calls[0][3], [0, 0, -1, 0.01667, 0, -1, 0.03333, 0, -1, 0.05, 0, -1]);
});

test('coordinates stay inside the canvas, whatever the pointer does', () => {
    const { dotnet, fire } = pad();

    fire('pointerdown', pointer(-50, -50));
    fire('pointermove', pointer(900, 400));
    fire('pointerup', pointer(900, 400));

    assert.deepEqual(dotnet.calls[0][3], [0, 0, -1, 1, 1, -1]);
});

test('only the primary pointer, the left button and an unlocked canvas draw', () => {
    const { dotnet, fire } = pad();
    assert.equal(fire('pointerdown', pointer(0, 0, { isPrimary: false })), false);
    assert.equal(fire('pointerdown', pointer(0, 0, { button: 2 })), false);
    fire('pointerup', pointer(0, 0));
    const locked = pad({ locked: true });
    assert.equal(locked.fire('pointerdown', pointer(0, 0)), false);
    locked.fire('pointerup', pointer(0, 0));

    assert.deepEqual(dotnet.calls, []);
    assert.deepEqual(locked.dotnet.calls, []);
});

test('a second pointer is ignored while a stroke is under way, and its moves and release too', () => {
    const { dotnet, fire } = pad();

    fire('pointerdown', pointer(0, 0));
    assert.equal(fire('pointerdown', pointer(100, 100, { pointerId: 2 })), false);
    fire('pointermove', pointer(300, 0, { pointerId: 2 }));
    fire('pointerup', pointer(300, 0, { pointerId: 2 }));
    fire('pointermove', pointer(300, 0));
    fire('pointerup', pointer(300, 0));

    assert.deepEqual(dotnet.calls[0][3], [0, 0, -1, 0.5, 0, -1]);
});

test('moves without a pointer down draw nothing', () => {
    const { context, fire } = pad();

    fire('pointermove', pointer(100, 100));

    assert.deepEqual(context.log, []);
});

test('a cancelled stroke is dropped, not reported', () => {
    const { dotnet, fire } = pad();

    fire('pointerdown', pointer(0, 0));
    fire('pointermove', pointer(300, 100));
    fire('pointercancel', pointer(300, 100));

    assert.deepEqual(dotnet.calls, []);
});

test('the stroke width is kept in range, and a missing width falls back to 2', () => {
    const wide = pad({ penWidth: '500' });
    wide.fire('pointerdown', pointer(0, 0));
    wide.fire('pointerup', pointer(0, 0));
    const narrow = pad({ penWidth: '0' });
    narrow.fire('pointerdown', pointer(0, 0));
    narrow.fire('pointerup', pointer(0, 0));
    const missing = pad({ penWidth: 'x', color: '' });
    missing.fire('pointerdown', pointer(0, 0));
    missing.fire('pointerup', pointer(0, 0));

    assert.equal(wide.dotnet.calls[0][2], 48);
    assert.equal(narrow.dotnet.calls[0][2], 0.25);
    assert.equal(missing.dotnet.calls[0][2], 2);
    assert.equal(missing.dotnet.calls[0][1], 'currentColor');
});

test('Ctrl+Z and Cmd+Z undo, Delete clears, and no other key does anything', () => {
    const { dotnet, fire } = pad();
    const key = (value, extra = {}) => fire('keydown', { key: value, ...extra });

    assert.equal(key('z', { ctrlKey: true }), true);
    assert.equal(key('Z', { metaKey: true }), true);
    assert.equal(key('Delete'), true);
    assert.equal(key('z', { ctrlKey: true, shiftKey: true }), false);
    assert.equal(key('z', { ctrlKey: true, altKey: true }), false);
    assert.equal(key('z'), false);
    assert.equal(key('Delete', { ctrlKey: true }), false);
    assert.equal(key('a', { ctrlKey: true }), false);

    assert.deepEqual(dotnet.calls, [['OnUndo'], ['OnUndo'], ['OnClear']]);
});

test('the keys do nothing on a locked canvas or during a stroke', () => {
    const locked = pad({ locked: true });
    locked.fire('keydown', { key: 'Delete' });
    const drawing = pad();
    drawing.fire('pointerdown', pointer(0, 0));
    drawing.fire('keydown', { key: 'z', ctrlKey: true });

    assert.deepEqual(locked.dotnet.calls, []);
    assert.deepEqual(drawing.dotnet.calls, []);
});

test('a .NET side that is gone, throwing or rejecting, is not an error', async () => {
    const { dotnet, fire } = pad();
    dotnet.invokeMethodAsync = () => { throw new Error('disposed'); };
    fire('keydown', { key: 'Delete' });
    dotnet.invokeMethodAsync = () => Promise.reject(new Error('gone'));
    fire('keydown', { key: 'Delete' });
    dotnet.invokeMethodAsync = () => undefined;
    fire('keydown', { key: 'Delete' });
    await new Promise(resolve => setTimeout(resolve, 0));
});

test('setStrokes draws what .NET holds, at the device resolution, and replaces the previous drawing', () => {
    const { canvas, context } = pad();

    ink.setStrokes(canvas, ['#00ff00', 'currentColor'], [3, 6], [[0.1, 0.5, -1, 0.9, 0.5, -1], [0.5, 0.5, -1]]);

    assert.equal(canvas.width, 1200);
    assert.deepEqual(context.log[0], ['setTransform', 2, 0, 0, 2, 0, 0]);
    assert.deepEqual(context.log[1], ['clearRect', 0, 0, 600, 200]);
    assert.equal(context.of('stroke').at(-1)[1], '#00ff00');
    assert.equal(context.of('fill').at(-1)[1], 'rgb(1, 2, 3)');
    context.log.length = 0;

    ink.setStrokes(canvas, [], [], []);
    assert.deepEqual(context.of('stroke'), []);
    assert.deepEqual(context.of('arc'), []);
});

test('a line is as wide as its stroke says on a canvas 600 pixels wide, and proportional to the width', () => {
    const narrow = pad({ width: 300 });
    ink.setStrokes(narrow.canvas, ['#000000'], [4], [[0.1, 0.5, -1, 0.9, 0.5, -1]]);

    assert.equal(narrow.context.of('stroke').at(-1)[2], 2);
});

test('a stroke without any point, which a host may give, draws nothing and breaks nothing', () => {
    const { canvas, context } = pad();

    ink.setStrokes(canvas, ['#000000'], [4], [[]]);

    assert.deepEqual(context.of('stroke'), []);
    assert.deepEqual(context.of('arc'), []);
});

test('a canvas without a size (hidden) is not drawn on', () => {
    const { canvas, context } = pad({ width: 0, height: 0 });

    ink.setStrokes(canvas, ['#000000'], [4], [[0.1, 0.5, -1, 0.9, 0.5, -1]]);

    assert.deepEqual(context.log, []);
});

test('a change of size, of theme or of colour scheme redraws', () => {
    const { canvas, context } = pad({ scope: { name: 'scope' } });
    ink.setStrokes(canvas, ['#000000'], [4], [[0.1, 0.5, -1, 0.9, 0.5, -1]]);
    const [resize, theme] = observers;

    assert.deepEqual(theme.target, { name: 'scope' });
    assert.deepEqual(theme.options, { attributes: true, attributeFilter: ['data-omni-theme', 'style', 'class'] });
    for (const trigger of [resize.callback, theme.callback, mediaListeners[0]]) {
        context.log.length = 0;
        trigger();
        assert.equal(context.of('stroke').length, 1);
    }
});

test('without a theme scope, the document root is watched', () => {
    pad();

    assert.deepEqual(observers[1].target, { name: 'root' });
});

test('attaching twice does nothing more, and a missing canvas is ignored', () => {
    const { canvas, dotnet } = pad();

    ink.attach(canvas, dotnet);
    ink.attach(null, dotnet);

    assert.equal(observers.length, 2);
});

test('detach removes the listeners and the observers, and later calls on that canvas do nothing', () => {
    const { canvas, listeners, context } = pad();
    mediaListeners.length = 1;

    ink.detach(canvas);
    ink.detach(canvas);
    ink.detach(null);
    ink.setStrokes(canvas, ['#000000'], [4], [[0.1, 0.5, -1, 0.9, 0.5, -1]]);
    ink.setStrokes(null, [], [], []);

    assert.equal(listeners.size, 0);
    assert.equal(observers.every(observer => observer.disconnected), true);
    assert.deepEqual(mediaListeners, []);
    assert.deepEqual(context.log, []);
});

test('setLayout writes the size as custom properties of the root, and removes what is not given', () => {
    const properties = new Map();
    const root = { style: { setProperty: (name, value) => properties.set(name, value), removeProperty: name => properties.delete(name) } };

    ink.setLayout(root, '30rem', '10rem', null);
    assert.deepEqual([...properties], [['--omni-ink-width', '30rem'], ['--omni-ink-height', '10rem']]);

    ink.setLayout(root, null, '10rem', 3);
    assert.deepEqual([...properties], [['--omni-ink-height', 'auto'], ['--omni-ink-aspect', '3']]);

    ink.setLayout(root, undefined, undefined, undefined);
    assert.deepEqual([...properties], []);
    ink.setLayout(null, '1px', '1px', null);
});

test('exportPng draws the strokes on a transparent canvas of the asked size and returns the PNG bytes', async () => {
    installPage();

    const bytes = await ink.exportPng(['#336699', 'currentColor'], [3, 6], [[0.1, 0.5, -1, 0.9, 0.5, -1], [0.5, 0.5, -1]], 1200, 400);

    const canvas = documentCanvases[0];
    assert.deepEqual([canvas.width, canvas.height], [1200, 400]);
    assert.equal(canvas.type, 'image/png');
    assert.deepEqual(canvas.context.log.filter(entry => entry[0] === 'clearRect'), []);
    // 1200 pixels is twice the reference width: a 3 pixel line is 6 pixels wide; the theme colour is black.
    assert.deepEqual(canvas.context.of('stroke').at(-1), ['stroke', '#336699', 6]);
    assert.equal(canvas.context.of('fill').at(-1)[1], '#000000');
    assert.deepEqual(canvas.context.of('arc').at(-1), ['arc', 600, 200, 6, 0, Math.PI * 2]);
    assert.equal(bytes instanceof Uint8Array, true);
    assert.deepEqual([...bytes], [137, 80, 78, 71]);
});

test('exportPng fails clearly when the browser cannot encode the image', async () => {
    installPage();
    const original = globalThis.document.createElement;
    globalThis.document.createElement = tag => {
        const canvas = original(tag);
        canvas.blob = null;
        return canvas;
    };

    await assert.rejects(ink.exportPng([], [], [], 10, 10), /PNG/);
});
