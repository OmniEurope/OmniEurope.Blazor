import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

// The map script (omni-map.js): the Web Mercator arithmetic (a point to its world pixel and back at every zoom, the tiles a
// view needs with their ring, the column of a tile across the date line, the framing of a set of points, a zoom that keeps
// the point under the pointer, a pan), then the module on a stand-in map element (tiles created, placed, kept to the view
// and its ring, hidden when they fail; lines and markers placed; drag, click, wheel, double click, keys, zoom buttons and
// focus told to .NET). Where the browser really draws the tiles and what real input does stay with the showcase probe
// (eng/scripts-probe/map.mjs).
const wwwroot = path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot');
const map = await import(pathToFileURL(path.join(wwwroot, 'omni-map.js')).href);
const { project, unproject, worldSize, clampLatitude, normalizeLongitude, tileRange, tileColumn, tileUrl, fitView, zoomAround, panBy, originOf, pathOf } = map;

const close = (actual, expected, tolerance = 1e-6) => assert.ok(Math.abs(actual - expected) <= tolerance, `${actual} is not ${expected} (within ${tolerance})`);
const BRUSSELS = { latitude: 50.8466, longitude: 4.3528 };

// ----- Web Mercator ------------------------------------------------------------------------------------------------

test('the world is 256 pixels at zoom 0 and doubles at every level', () => {
    assert.equal(worldSize(0), 256);
    assert.equal(worldSize(1), 512);
    assert.equal(worldSize(13), 256 * 8192);
});

test('the centre of the world is (0, 0) and its corners are the date line at the Mercator limits', () => {
    const middle = project(0, 0, 0);
    close(middle.x, 128);
    close(middle.y, 128);
    const northWest = project(85.0511287798066, -180, 0);
    close(northWest.x, 0);
    close(northWest.y, 0, 1e-6);
    const southEast = project(-85.0511287798066, 180, 2);
    close(southEast.x, 1024);
    close(southEast.y, 1024, 1e-6);
});

test('a point goes to its pixel and back at every zoom, to a millionth of a degree', () => {
    for (const [latitude, longitude] of [[50.8466, 4.3528], [0, 0], [-33.8688, 151.2093], [64.1466, -21.9426], [-84.9, -179.9]]) {
        for (const zoom of [0, 2, 9, 13, 19, 22]) {
            const pixel = project(latitude, longitude, zoom);
            const back = unproject(pixel.x, pixel.y, zoom);
            close(back.latitude, latitude, 1e-6);
            close(back.longitude, longitude, 1e-6);
        }
    }
});

test('a pixel goes to its point and back', () => {
    const point = unproject(1_100_000.25, 700_000.75, 13);
    const pixel = project(point.latitude, point.longitude, 13);
    close(pixel.x, 1_100_000.25, 1e-4);
    close(pixel.y, 700_000.75, 1e-4);
});

test('a latitude beyond the Mercator limit is drawn on it, and a longitude is brought back between -180 and 180', () => {
    assert.equal(clampLatitude(89), 85.0511287798066);
    assert.equal(clampLatitude(-90), -85.0511287798066);
    assert.equal(clampLatitude(50), 50);
    close(project(89, 0, 0).y, project(85.0511287798066, 0, 0).y);
    assert.equal(normalizeLongitude(190), -170);
    assert.equal(normalizeLongitude(-190), 170);
    assert.equal(normalizeLongitude(540), 180 - 360);
    assert.equal(normalizeLongitude(12.5), 12.5);
});

test('Brussels at zoom 13 is in tile 4195, 2748 (x = (lon + 180) / 360, y = (1 - ln(tan + sec) / pi) / 2, times 8192)', () => {
    const pixel = project(BRUSSELS.latitude, BRUSSELS.longitude, 13);
    assert.deepEqual([Math.floor(pixel.x / 256), Math.floor(pixel.y / 256)], [4195, 2748]);
});

// ----- Tiles -------------------------------------------------------------------------------------------------------

test('the tiles of a view are the ones it shows plus one ring around them', () => {
    // Columns 3.9 to 7.0 and rows 1.95 to 4.3 are in view: 3..7 and 1..4, then the ring.
    assert.deepEqual(tileRange({ x: 1000, y: 500 }, 800, 600, 3), { minX: 2, maxX: 8, minY: 0, maxY: 5 });
});

test('the rows stop at the edges of the world, the columns do not', () => {
    assert.deepEqual(tileRange({ x: -300, y: -200 }, 800, 900, 1), { minX: -3, maxX: 2, minY: 0, maxY: 1 });
});

test('a column outside the world loads the tile of the same place one world over', () => {
    assert.equal(tileColumn(-1, 3), 7);
    assert.equal(tileColumn(8, 3), 0);
    assert.equal(tileColumn(5, 3), 5);
    assert.equal(tileColumn(-17, 3), 7);
});

test('the tile address fills the three placeholders', () => {
    assert.equal(tileUrl('https://tile.openstreetmap.org/{z}/{x}/{y}.png', 13, 4195, 2742), 'https://tile.openstreetmap.org/13/4195/2742.png');
    assert.equal(tileUrl('/tiles/{y}/{x}@{z}', 2, 1, 3), '/tiles/3/1@2');
});

// ----- Framing, zoom and pan ---------------------------------------------------------------------------------------

const route = [[50.8466, 4.3528], [50.8503, 4.3517], [50.8355, 4.3330], [50.8270, 4.3720]];

test('fitting frames every point inside the margin, at the highest zoom that does', () => {
    const width = 800;
    const height = 400;
    const view = fitView(route, width, height, 40, 2, 19);
    const origin = originOf(view, width, height);
    for (const [latitude, longitude] of route) {
        const pixel = project(latitude, longitude, view.zoom);
        assert.ok(pixel.x - origin.x >= 40 - 1e-6 && pixel.x - origin.x <= width - 40 + 1e-6, `x ${pixel.x - origin.x} outside the margin`);
        assert.ok(pixel.y - origin.y >= 40 - 1e-6 && pixel.y - origin.y <= height - 40 + 1e-6, `y ${pixel.y - origin.y} outside the margin`);
    }

    // One level more and the box no longer fits.
    const across = project(50.8270, 4.3720, view.zoom + 1).x - project(50.8355, 4.3330, view.zoom + 1).x;
    const down = project(50.8270, 4.3720, view.zoom + 1).y - project(50.8503, 4.3517, view.zoom + 1).y;
    assert.ok(across > width - 80 || down > height - 80, 'the next zoom would still fit: the framing is not the highest');
    assert.equal(view.zoom, 13);
});

test('fitting centres the view on the middle of the box of the points', () => {
    const view = fitView([[10, 20], [20, 40]], 1000, 1000, 0, 0, 19);
    const middle = unproject((project(10, 20, 0).x + project(20, 40, 0).x) / 2, (project(10, 20, 0).y + project(20, 40, 0).y) / 2, 0);
    close(view.latitude, middle.latitude);
    close(view.longitude, middle.longitude);
});

test('fitting a single place shows it at zoom 16, or the highest allowed below', () => {
    assert.equal(fitView([[50.85, 4.35]], 800, 400, 40, 2, 19).zoom, 16);
    assert.equal(fitView([[50.85, 4.35], [50.85, 4.35]], 800, 400, 40, 2, 12).zoom, 12);
});

test('fitting keeps the zoom inside the allowed range, and has nothing to frame without a point', () => {
    assert.equal(fitView([[-60, -170], [70, 170]], 400, 300, 40, 3, 19).zoom, 3);
    assert.equal(fitView([[50.8466, 4.3528], [50.8466001, 4.3528001]], 800, 600, 40, 2, 15).zoom, 15);
    assert.equal(fitView([], 800, 600, 40, 2, 19), null);
});

test('zooming around a point keeps that point under the pointer', () => {
    const view = { ...BRUSSELS, zoom: 13 };
    for (const [px, py, zoom] of [[100, 50, 14], [700, 380, 12], [400, 200, 15]]) {
        const before = originOf(view, 800, 400);
        const anchor = unproject(before.x + px, before.y + py, 13);
        const zoomed = zoomAround(view, 800, 400, px, py, zoom);
        assert.equal(zoomed.zoom, zoom);
        const after = originOf(zoomed, 800, 400);
        const pixel = project(anchor.latitude, anchor.longitude, zoom);
        close(pixel.x - after.x, px, 1e-4);
        close(pixel.y - after.y, py, 1e-4);
    }
});

test('zooming around the centre keeps the centre', () => {
    const zoomed = zoomAround({ ...BRUSSELS, zoom: 13 }, 800, 400, 400, 200, 14);
    close(zoomed.latitude, BRUSSELS.latitude, 1e-9);
    close(zoomed.longitude, BRUSSELS.longitude, 1e-9);
});

test('panning moves the centre by the pixels asked, at the same zoom', () => {
    const view = { ...BRUSSELS, zoom: 13 };
    const moved = panBy(view, 80, -40);
    const before = project(view.latitude, view.longitude, 13);
    const after = project(moved.latitude, moved.longitude, 13);
    close(after.x - before.x, 80, 1e-6);
    close(after.y - before.y, -40, 1e-6);
    assert.equal(moved.zoom, 13);
});

test('a line is a path through its points in pixels of the view', () => {
    const origin = { x: 100, y: 200 };
    const d = pathOf([0, 0, 0, 90], 0, origin);
    assert.equal(d, 'M28 -72 L92 -72');
});

// ----- The module on a stand-in map --------------------------------------------------------------------------------

function makeMap(page, { width = 800, height = 400, lines = 1, markers = 2 } = {}) {
    const tiles = page.el('div', { class: 'omni-map__tiles' });
    const svg = page.el('svg');
    for (let index = 0; index < lines; index++) {
        svg.append(page.el('g', { 'data-omni-map-line': '' }, page.el('path', { class: 'omni-map__casing' }), page.el('path', { class: 'omni-map__stroke' })));
    }

    const layer = page.el('div');
    for (let index = 0; index < markers; index++) {
        layer.append(page.el('button', { 'data-omni-map-marker': '' }));
    }

    const controls = page.el('div', { class: 'omni-map__controls' }, page.el('button', { class: 'omni-map__zoom' }));
    const root = page.el('div', { class: 'omni-map' }, tiles, svg, layer, controls);
    const listeners = new Map();
    root.addEventListener = (type, handler, options) => listeners.set(type, { handler, options });
    root.removeEventListener = type => listeners.delete(type);
    root.captured = [];
    root.setPointerCapture = id => root.captured.push(id);
    root.rect = { left: 10, top: 20, right: 10 + width, bottom: 20 + height, width, height };
    Object.defineProperty(root, 'clientWidth', { get: () => root.rect.width });
    Object.defineProperty(root, 'clientHeight', { get: () => root.rect.height });
    page.body.append(root);
    const fire = (type, event = {}) => {
        const entry = listeners.get(type);
        assert.ok(entry, `no ${type} listener`);
        const record = { target: root, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }, ...event };
        entry.handler(record);
        return record;
    };
    return { root, tiles, svg, layer, controls, listeners, fire };
}

const installPage = () => {
    const page = installDom();
    globalThis.Element.prototype.remove ??= function remove() {
        if (this.parentElement) {
            this.parentElement.children = this.parentElement.children.filter(child => child !== this);
            this.parentElement = null;
        }
    };
    const observers = [];
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; this.disconnected = false; observers.push(this); }
        observe(element) { this.element = element; }
        disconnect() { this.disconnected = true; }
    };
    return { page, observers };
};

const TEMPLATE = 'https://tile.example/{z}/{x}/{y}.png';
const LINE = [50.8466, 4.3528, 50.8355, 4.3330];
const MARKERS = [[50.8466, 4.3528], [50.8355, 4.3330]];
const lastView = calls => calls.filter(call => call[0] === 'OnViewChanged').at(-1);

// Where the script draws a point of a view: its pixel minus the origin of the view, the origin on a whole pixel as drawn.
const drawnAt = (latitude, longitude, view, width = 800, height = 400) => {
    const origin = originOf(view, width, height);
    const pixel = project(latitude, longitude, view.zoom);
    return { x: Math.round((pixel.x - Math.round(origin.x)) * 100) / 100, y: Math.round((pixel.y - Math.round(origin.y)) * 100) / 100 };
};

// The place a marker was given, read back from its two custom properties.
const placeOf = element => ({
    x: Number.parseFloat(element.style.getPropertyValue('--omni-map-x')),
    y: Number.parseFloat(element.style.getPropertyValue('--omni-map-y'))
});

test('update places the tiles of the view and its ring, each at its pixel, with an empty alternative text', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);

    map.update(stand.root, TEMPLATE, 2, 19, [LINE], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    const origin = originOf({ ...BRUSSELS, zoom: 13 }, 800, 400);
    const range = tileRange({ x: Math.round(origin.x), y: Math.round(origin.y) }, 800, 400, 13);
    const images = stand.tiles.children;
    assert.equal(images.length, (range.maxX - range.minX + 1) * (range.maxY - range.minY + 1));
    const first = images[0];
    assert.equal(first.alt, '');
    assert.equal(first.referrerPolicy, 'strict-origin-when-cross-origin');
    assert.equal(first.src, `https://tile.example/13/${range.minX}/${range.minY}.png`);
    assert.equal(first.style.getPropertyValue('--omni-map-tile-x'), `${range.minX * 256 - Math.round(origin.x)}px`);
    assert.equal(first.style.getPropertyValue('--omni-map-tile-y'), `${range.minY * 256 - Math.round(origin.y)}px`);
    // .NET gave this view: nothing to tell back.
    assert.deepEqual(dotnet.calls, []);
});

test('update writes the path of each line on its casing and its stroke, and places each marker by its custom properties', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    map.attach(stand.root, dotnetRecorder());

    map.update(stand.root, TEMPLATE, 2, 19, [LINE], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    const view = { ...BRUSSELS, zoom: 13 };
    const origin = originOf(view, 800, 400);
    const [casing, stroke] = stand.svg.children[0].children;
    assert.equal(casing.getAttribute('d'), pathOf(LINE, 13, { x: Math.round(origin.x), y: Math.round(origin.y) }));
    assert.match(casing.getAttribute('d'), /^M\d+(\.\d)? \d+(\.\d)? L\d+(\.\d)? \d+(\.\d)?$/);
    assert.equal(stroke.getAttribute('d'), casing.getAttribute('d'));
    const [start, end] = stand.layer.children;
    // The centre of the view is the middle of the element, to the rounding of the origin.
    const centre = drawnAt(BRUSSELS.latitude, BRUSSELS.longitude, view);
    assert.ok(Math.abs(centre.x - 400) <= 0.5 && Math.abs(centre.y - 200) <= 0.5);
    assert.deepEqual(placeOf(start), centre);
    assert.ok(start.hasAttribute('data-omni-map-placed'));
    assert.deepEqual(placeOf(end), drawnAt(50.8355, 4.3330, view));
});

test('a tile that fails stays hidden, one that loads is shown', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const created = [];
    const createElement = document.createElement;
    document.createElement = name => {
        const element = createElement(name);
        const handlers = {};
        element.addEventListener = (type, handler) => { handlers[type] = handler; };
        element.fire = type => handlers[type]();
        created.push(element);
        return element;
    };
    map.attach(stand.root, dotnetRecorder());

    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    created[0].fire('error');
    created[1].fire('load');

    assert.ok(created[0].classList.contains('omni-map__tile--failed'));
    assert.ok(!created[0].classList.contains('omni-map__tile--loaded'));
    assert.ok(created[1].classList.contains('omni-map__tile--loaded'));
    document.createElement = createElement;
});

test('only the tiles of the view and its ring stay: a move removes the ones left behind and reuses the others', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    map.attach(stand.root, dotnetRecorder());
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    const before = [...stand.tiles.children];

    stand.fire('keydown', { key: 'ArrowRight' });
    stand.fire('keydown', { key: 'ArrowRight' });
    stand.fire('keydown', { key: 'ArrowRight' });
    stand.fire('keydown', { key: 'ArrowRight' });

    const after = [...stand.tiles.children];
    assert.ok(after.some(image => before.includes(image)), 'no tile was reused');
    assert.ok(before.some(image => !after.includes(image)), 'no tile left the page');
    assert.equal(after.length, before.length);
});

test('fitting frames the content once the map has a size, and tells .NET the new view', () => {
    const { page, observers } = installPage();
    const stand = makeMap(page, { width: 0, height: 0 });
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [LINE], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], true);
    assert.deepEqual(dotnet.calls, [], 'framed before the map had a size');

    stand.root.rect = { left: 0, top: 0, right: 800, bottom: 400, width: 800, height: 400 };
    observers[0].callback();

    const wanted = fitView([...MARKERS, [LINE[0], LINE[1]], [LINE[2], LINE[3]]], 800, 400, 40, 2, 19);
    const told = lastView(dotnet.calls);
    assert.deepEqual(told.slice(1), [wanted.latitude, wanted.longitude, wanted.zoom]);
    assert.ok(stand.tiles.children.length > 0);
});

test('a zoom out of the allowed range is brought back into it and told to .NET', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 18], false);

    map.update(stand.root, TEMPLATE, 2, 15, null, null, null, false);

    assert.equal(lastView(dotnet.calls)[3], 15);
});

test('a drag pans the view with the pointer and tells .NET once it is released, never a click', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [LINE], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    const before = project(BRUSSELS.latitude, BRUSSELS.longitude, 13);

    stand.fire('pointerdown', { pointerId: 7, pointerType: 'touch', button: 0, clientX: 300, clientY: 200 });
    stand.fire('pointermove', { pointerId: 7, clientX: 302, clientY: 201 });
    const still = placeOf(stand.layer.children[0]);
    assert.deepEqual(still, drawnAt(BRUSSELS.latitude, BRUSSELS.longitude, { ...BRUSSELS, zoom: 13 }), 'moved under the threshold');
    stand.fire('pointermove', { pointerId: 7, clientX: 250, clientY: 260 });
    assert.ok(stand.root.classList.contains('omni-map--dragging'));
    // The marker follows the pointer, 50 left and 60 down, to the whole pixel the origin is drawn on.
    const dragged = placeOf(stand.layer.children[0]);
    assert.ok(Math.abs(dragged.x - still.x + 50) <= 1 && Math.abs(dragged.y - still.y - 60) <= 1, `the marker did not follow: ${JSON.stringify({ still, dragged })}`);
    assert.deepEqual(dotnet.calls, [], 'told before the release');
    stand.fire('pointerup', { pointerId: 7, clientX: 250, clientY: 260 });

    assert.ok(!stand.root.classList.contains('omni-map--dragging'));
    assert.deepEqual(stand.root.captured, [7]);
    assert.equal(dotnet.calls.length, 1);
    const [, latitude, longitude, zoom] = dotnet.calls[0];
    const after = project(latitude, longitude, zoom);
    close(after.x - before.x, 50, 1e-6);
    close(after.y - before.y, -60, 1e-6);
});

test('a press that does not move is a click: .NET gets the place under the pointer', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    // The element starts at (10, 20): the middle of the map is (410, 220) on the screen.
    stand.fire('pointerdown', { pointerId: 1, pointerType: 'mouse', button: 0, clientX: 410, clientY: 220 });
    stand.fire('pointerup', { pointerId: 1, clientX: 411, clientY: 221 });

    assert.equal(dotnet.calls.length, 1);
    const [method, latitude, longitude] = dotnet.calls[0];
    assert.equal(method, 'OnMapClick');
    const origin = originOf({ ...BRUSSELS, zoom: 13 }, 800, 400);
    const wanted = unproject(Math.round(origin.x) + 401, Math.round(origin.y) + 201, 13);
    close(latitude, wanted.latitude, 1e-9);
    close(longitude, wanted.longitude, 1e-9);
});

test('a press on a button or the attribution neither pans nor clicks the map, nor does a right press', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    const marker = stand.layer.children[0];

    stand.fire('pointerdown', { target: marker, pointerId: 2, pointerType: 'mouse', button: 0, clientX: 410, clientY: 220 });
    stand.fire('pointermove', { pointerId: 2, clientX: 300, clientY: 100 });
    stand.fire('pointerup', { pointerId: 2, clientX: 300, clientY: 100 });
    stand.fire('pointerdown', { pointerId: 3, pointerType: 'mouse', button: 2, clientX: 410, clientY: 220 });
    stand.fire('pointerup', { pointerId: 3, clientX: 410, clientY: 220 });

    assert.deepEqual(dotnet.calls, []);
});

test('a cancelled press is no click, a cancelled drag still tells its view', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    stand.fire('pointerdown', { pointerId: 4, pointerType: 'touch', clientX: 100, clientY: 100 });
    stand.fire('pointercancel', { pointerId: 4, clientX: 100, clientY: 100 });
    assert.deepEqual(dotnet.calls, []);
    stand.fire('pointerdown', { pointerId: 5, pointerType: 'touch', clientX: 100, clientY: 100 });
    stand.fire('pointermove', { pointerId: 5, clientX: 140, clientY: 100 });
    stand.fire('pointercancel', { pointerId: 5, clientX: 140, clientY: 100 });
    assert.equal(dotnet.calls.length, 1);
    assert.equal(dotnet.calls[0][0], 'OnViewChanged');
});

test('the wheel zooms one level per notch around the pointer and keeps the page from scrolling', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    assert.deepEqual(stand.listeners.get('wheel').options, { passive: false });

    const small = stand.fire('wheel', { deltaY: -40, deltaMode: 0, clientX: 110, clientY: 70 });
    assert.ok(small.defaultPrevented);
    assert.deepEqual(dotnet.calls, [], 'a small turn zoomed');
    stand.fire('wheel', { deltaY: -80, deltaMode: 0, clientX: 110, clientY: 70 });

    const [, latitude, longitude, zoom] = lastView(dotnet.calls);
    assert.equal(zoom, 14);
    const wanted = zoomAround({ ...BRUSSELS, zoom: 13 }, 800, 400, 100, 50, 14);
    close(latitude, wanted.latitude, 1e-9);
    close(longitude, wanted.longitude, 1e-9);
    stand.fire('wheel', { deltaY: 3, deltaMode: 1, clientX: 110, clientY: 70 });
    assert.equal(lastView(dotnet.calls)[3], 13);
});

test('a double click zooms in around the pointer, with Shift it zooms out, never past the range', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 14, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    assert.ok(stand.fire('dblclick', { clientX: 210, clientY: 120 }).defaultPrevented);
    assert.equal(lastView(dotnet.calls)[3], 14);
    stand.fire('dblclick', { clientX: 210, clientY: 120 });
    assert.equal(dotnet.calls.length, 1, 'zoomed past the highest level');
    stand.fire('dblclick', { clientX: 210, clientY: 120, shiftKey: true });
    assert.equal(lastView(dotnet.calls)[3], 13);
});

test('the arrows pan and + and - zoom while the map itself has the focus, not from a marker or with Ctrl', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], MARKERS, [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    const before = project(BRUSSELS.latitude, BRUSSELS.longitude, 13);

    assert.ok(stand.fire('keydown', { key: 'ArrowLeft' }).defaultPrevented);
    stand.fire('keydown', { key: 'ArrowDown' });
    const [, latitude, longitude] = lastView(dotnet.calls);
    const after = project(latitude, longitude, 13);
    close(after.x - before.x, -80, 1e-6);
    close(after.y - before.y, 80, 1e-6);

    stand.fire('keydown', { key: '+' });
    assert.equal(lastView(dotnet.calls)[3], 14);
    stand.fire('keydown', { key: '-' });
    stand.fire('keydown', { key: '-' });
    assert.equal(lastView(dotnet.calls)[3], 12);

    const count = dotnet.calls.length;
    assert.ok(!stand.fire('keydown', { key: 'ArrowLeft', target: stand.layer.children[0] }).defaultPrevented);
    assert.ok(!stand.fire('keydown', { key: '+', ctrlKey: true }).defaultPrevented);
    assert.ok(!stand.fire('keydown', { key: 'a' }).defaultPrevented);
    assert.equal(dotnet.calls.length, count);
});

test('zoomBy zooms around the centre, as the buttons ask', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);

    map.zoomBy(stand.root, 1);

    const [, latitude, longitude, zoom] = lastView(dotnet.calls);
    assert.equal(zoom, 14);
    close(latitude, BRUSSELS.latitude, 1e-9);
    close(longitude, BRUSSELS.longitude, 1e-9);
    assert.match(stand.tiles.children[0].src, /\/14\//);
});

test('a marker reached by Tab outside the view brings the view onto it; one inside leaves the view alone', () => {
    const { page } = installPage();
    const stand = makeMap(page);
    const dotnet = dotnetRecorder();
    map.attach(stand.root, dotnet);
    map.update(stand.root, TEMPLATE, 2, 19, [], [[50.8466, 4.3528], [50.6, 4.9]], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    const [inside, outside] = stand.layer.children;

    stand.fire('focusin', { target: inside });
    assert.deepEqual(dotnet.calls, []);
    stand.fire('focusin', { target: outside });

    const [, latitude, longitude] = lastView(dotnet.calls);
    close(latitude, 50.6, 1e-9);
    close(longitude, 4.9, 1e-9);
    assert.ok(Math.abs(placeOf(outside).x - 400) <= 0.5 && Math.abs(placeOf(outside).y - 200) <= 0.5);
});

test('detach stops every listener and the observer, and removes the tiles', () => {
    const { page, observers } = installPage();
    const stand = makeMap(page);
    map.attach(stand.root, dotnetRecorder());
    map.update(stand.root, TEMPLATE, 2, 19, [], [], [BRUSSELS.latitude, BRUSSELS.longitude, 13], false);
    assert.equal(stand.listeners.size, 8);

    map.detach(stand.root);

    assert.equal(stand.listeners.size, 0);
    assert.ok(observers[0].disconnected);
    assert.equal(stand.tiles.children.length, 0);
    assert.doesNotThrow(() => map.update(stand.root, TEMPLATE, 2, 19, [], [], null, false));
    assert.doesNotThrow(() => map.zoomBy(stand.root, 1));
    assert.doesNotThrow(() => map.detach(stand.root));
});

test('attaching twice attaches once, and an element without its tile layer is left alone', () => {
    const { page, observers } = installPage();
    const stand = makeMap(page);
    map.attach(stand.root, dotnetRecorder());
    map.attach(stand.root, dotnetRecorder());
    map.attach(page.el('div'), dotnetRecorder());
    map.attach(null, dotnetRecorder());

    assert.equal(observers.length, 1);
});
