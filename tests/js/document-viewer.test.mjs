import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

// The document viewer script (omni-document-viewer.js): the size it gives each page, the zoom that fits the width of
// the viewport, the page it reports as in view, and what it releases. The boxes come from the rects these tests set by
// hand: what the browser makes of them is the business of the showcase.
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const dom = installDom();
const { attach, update, detach } = await import(pathToFileURL(path.join(wwwroot, 'omni-document-viewer.js')).href);

const PAGE = 'omni-document-viewer__page';
const observers = [];
const frames = [];
let cancelled = [];

class FakeResizeObserver {
    constructor(callback) {
        this.callback = callback;
        this.disconnected = false;
        observers.push(this);
    }

    observe() { }
    disconnect() { this.disconnected = true; }
}

globalThis.ResizeObserver = FakeResizeObserver;
globalThis.requestAnimationFrame = callback => {
    frames.push(callback);
    return frames.length;
};
globalThis.cancelAnimationFrame = id => cancelled.push(id);

// A viewport of the given width and height holding pages, each described by its attributes and its picture.
const viewerWith = (viewportRect, pages, padding = { paddingLeft: '0px', paddingRight: '0px' }) => {
    observers.length = 0;
    frames.length = 0;
    cancelled = [];
    const listeners = {};
    const viewport = dom.el('div', {}, ...pages.map(page => {
        const children = page.naturalWidth === undefined ? [] : [Object.assign(dom.el('img'), { naturalWidth: page.naturalWidth })];
        const element = dom.el('div', { class: PAGE, ...page.attributes }, ...children);
        element.rect = page.rect ?? { left: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0 };
        return element;
    }));
    viewport.rect = { left: 0, top: 0, right: viewportRect.width, bottom: viewportRect.height, ...viewportRect };
    viewport.addEventListener = (type, handler, options) => { listeners[type] = { handler, options }; };
    viewport.removeEventListener = (type, handler) => { if (listeners[type]?.handler === handler) delete listeners[type]; };
    globalThis.getComputedStyle = element => (element === viewport ? padding : { visibility: 'visible' });
    return { viewport, listeners, pages: viewport.children };
};

const spot = (top, bottom) => ({ left: 0, top, right: 100, bottom, width: 100, height: bottom - top });

test('each page gets its natural width: the declared one, else its picture, else an A4 width', () => {
    const { viewport, pages } = viewerWith({ width: 1000, height: 500 }, [
        { attributes: { 'data-width-pt': '595.3', 'data-height-pt': '841.9' } },
        { naturalWidth: 640 },
        {}
    ]);

    attach(viewport, dotnetRecorder(), 1, false, 0.25, 4);

    assert.equal(pages[0].style.getPropertyValue('--omni-document-page-width'), `${595.3 * 96 / 72}px`);
    assert.equal(pages[0].style.getPropertyValue('--omni-document-page-height'), `${841.9 * 96 / 72}px`);
    assert.equal(pages[1].style.getPropertyValue('--omni-document-page-width'), '640px');
    assert.equal(pages[1].style.getPropertyValue('--omni-document-page-height'), '');
    assert.equal(pages[2].style.getPropertyValue('--omni-document-page-width'), '794px');
    assert.equal(viewport.style.getPropertyValue('--omni-document-zoom'), '1');
});

test('a picture that has not loaded yet falls back to the A4 width, and the page is sized again when it loads', () => {
    const { viewport, listeners, pages } = viewerWith({ width: 1000, height: 500 }, [{ naturalWidth: 0 }]);
    attach(viewport, dotnetRecorder(), 1, false, 0.25, 4);
    assert.equal(pages[0].style.getPropertyValue('--omni-document-page-width'), '794px');

    assert.equal(listeners.load.options, true);
    pages[0].children[0].naturalWidth = 500;
    listeners.load.handler();

    assert.equal(pages[0].style.getPropertyValue('--omni-document-page-width'), '500px');
});

test('a viewport that is resized lays the pages out again', () => {
    const { viewport, pages } = viewerWith({ width: 1000, height: 500 }, [{ attributes: { 'data-width-pt': '300' } }]);
    attach(viewport, dotnetRecorder(), 1, false, 0.25, 4);
    pages[0].setAttribute('data-width-pt', '450');

    observers[0].callback();

    assert.equal(pages[0].style.getPropertyValue('--omni-document-page-width'), '600px');
});

test('fitting the width reports the zoom that makes the widest page fill the viewport, less its padding, rounded down', () => {
    const { viewport } = viewerWith(
        { width: 800, height: 500 },
        [{ attributes: { 'data-width-pt': '150' } }, { attributes: { 'data-width-pt': '300' } }],
        { paddingLeft: '20px', paddingRight: '20px' });
    const dotNet = dotnetRecorder();

    attach(viewport, dotNet, 1, true, 0.25, 4);

    // 760 available, 400 wide: 1.9.
    assert.deepEqual(dotNet.calls, [['OnViewChangedAsync', 1.9, 1]]);
    assert.equal(viewport.style.getPropertyValue('--omni-document-zoom'), '1.9');

    viewport.rect = { ...viewport.rect, width: 701 };
    observers[0].callback();
    // 661 / 400 = 1.6525, rounded down to a whole percent.
    assert.deepEqual(dotNet.calls.at(-1), ['OnViewChangedAsync', 1.65, 1]);
});

test('the fitted zoom is kept between the limits, and kept when the viewport has no width', () => {
    const wide = viewerWith({ width: 4000, height: 500 }, [{ attributes: { 'data-width-pt': '300' } }]);
    const wideNet = dotnetRecorder();
    attach(wide.viewport, wideNet, 1, true, 0.25, 4);
    assert.deepEqual(wideNet.calls, [['OnViewChangedAsync', 4, 1]]);

    const narrow = viewerWith({ width: 40, height: 500 }, [{ attributes: { 'data-width-pt': '3000' } }]);
    const narrowNet = dotnetRecorder();
    attach(narrow.viewport, narrowNet, 1, true, 0.25, 4);
    assert.deepEqual(narrowNet.calls, [['OnViewChangedAsync', 0.25, 1]]);

    const hidden = viewerWith({ width: 0, height: 0 }, [{ attributes: { 'data-width-pt': '300' } }]);
    const hiddenNet = dotnetRecorder();
    attach(hidden.viewport, hiddenNet, 1.5, true, 0.25, 4);
    assert.deepEqual(hiddenNet.calls, []);
    assert.equal(hidden.viewport.style.getPropertyValue('--omni-document-zoom'), '1.5');
});

test('without fitting, the zoom .NET holds is applied and .NET is told nothing', () => {
    const { viewport } = viewerWith({ width: 800, height: 500 }, [{ attributes: { 'data-width-pt': '300' }, rect: spot(0, 300) }]);
    const dotNet = dotnetRecorder();

    attach(viewport, dotNet, 2, false, 0.25, 4);
    assert.equal(viewport.style.getPropertyValue('--omni-document-zoom'), '2');
    update(viewport, 3, false);
    assert.equal(viewport.style.getPropertyValue('--omni-document-zoom'), '3');

    assert.deepEqual(dotNet.calls, []);
});

test('update takes the fit and the zoom .NET sends, and a fit that finds another zoom is reported', () => {
    const { viewport } = viewerWith({ width: 800, height: 500 }, [{ attributes: { 'data-width-pt': '300' } }]);
    const dotNet = dotnetRecorder();
    attach(viewport, dotNet, 1, false, 0.25, 4);

    update(viewport, 1, true);

    // 800 / 400 = 2.
    assert.deepEqual(dotNet.calls, [['OnViewChangedAsync', 2, 1]]);
    update(viewport, 2, true);
    assert.equal(dotNet.calls.length, 1);
});

test('the page that shows most inside the viewport is reported on scroll, once per frame', () => {
    const { viewport, listeners, pages } = viewerWith({ width: 800, height: 500 }, [
        { attributes: { 'data-width-pt': '300' }, rect: spot(0, 400) },
        { attributes: { 'data-width-pt': '300' }, rect: spot(400, 800) }
    ]);
    const dotNet = dotnetRecorder();
    attach(viewport, dotNet, 1, false, 0.25, 4);
    assert.deepEqual(dotNet.calls, []);
    assert.deepEqual(listeners.scroll.options, { passive: true });

    pages[0].rect = spot(-300, 100);
    pages[1].rect = spot(100, 500);
    listeners.scroll.handler();
    listeners.scroll.handler();
    assert.equal(frames.length, 1);
    frames.shift()();
    assert.deepEqual(dotNet.calls, [['OnViewChangedAsync', 1, 2]]);

    // The same page again tells nothing; a scroll after the frame ran asks for a new frame.
    listeners.scroll.handler();
    frames.shift()();
    assert.equal(dotNet.calls.length, 1);
});

test('a viewer without pages reports no page in view', () => {
    const { viewport } = viewerWith({ width: 800, height: 500 }, []);
    const dotNet = dotnetRecorder();

    attach(viewport, dotNet, 1, true, 0.25, 4);

    assert.deepEqual(dotNet.calls, [['OnViewChangedAsync', 1, 0]]);
});

test('detach releases the observer, the listeners and the zoom', () => {
    const { viewport, listeners } = viewerWith({ width: 800, height: 500 }, [{ attributes: { 'data-width-pt': '300' } }]);
    attach(viewport, dotnetRecorder(), 1, false, 0.25, 4);
    listeners.scroll.handler();
    const frame = frames.length;

    detach(viewport);

    assert.equal(observers[0].disconnected, true);
    assert.equal(listeners.load, undefined);
    assert.equal(listeners.scroll, undefined);
    assert.deepEqual(cancelled, [frame]);
    assert.equal(viewport.style.getPropertyValue('--omni-document-zoom'), '');
});

test('detach of a viewport the script does not hold, or of a removed one (null), does nothing', () => {
    // Disposed after the page left: Blazor resolves the removed element to null, never attached here.
    assert.doesNotThrow(() => detach(null));
    assert.doesNotThrow(() => detach(undefined));
});
