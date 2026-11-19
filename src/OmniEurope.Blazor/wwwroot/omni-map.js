// Map of OmniMap, drawn by the package itself: no map library.
//
// The world is Web Mercator (EPSG:3857) in 256-pixel tiles: at zoom z it is a square of 256 x 2^z pixels, x growing east
// from the date line and y growing south from the northern limit (85.0511 degrees). The view is a centre (latitude and
// longitude) and an integer zoom; the top left corner of the element is the centre's world pixel minus half its size.
//
// From that, at each change of the view, of the content or of the size, this module:
// - keeps in .omni-map__tiles one <img> per tile in view plus one ring around them, created here, positioned by the
//   custom properties --omni-map-tile-x and --omni-map-tile-y through the CSSOM, removed when they leave the ring; a tile
//   shows once loaded and stays hidden when it fails, leaving the surface colour;
// - writes the "d" of the two paths (casing and stroke) of every line group .NET rendered ([data-omni-map-line], in the
//   order of the lines), in pixels of the element;
// - places every marker button .NET rendered ([data-omni-map-marker], in the order of the markers) through
//   --omni-map-x and --omni-map-y.
// Nothing is a style attribute: custom properties set through the CSSOM and SVG geometry only.
//
// The reader moves the view: a drag pans it (pointer events, so mouse, pen and finger; a press that does not move is a
// click and reports its place), the wheel and a double click zoom one level around the pointer, the arrows pan and + and -
// zoom while the map itself has the focus, and a marker reached by Tab outside the view is brought into it. Every move is
// told to .NET once it is done (OnViewChanged), never animated.
import { notify } from './shared/notify.js';

const TILE = 256;
const MAX_LATITUDE = 85.0511287798066;
const DRAG_THRESHOLD = 4;
const KEY_STEP = 80;
const WHEEL_STEP = 100;
const FIT_PADDING = 40;
const POINT_ZOOM = 16;
const CONTROLS = 'button, a, .omni-map__controls, .omni-map__attribution';
const maps = new WeakMap();

// ----- Web Mercator ------------------------------------------------------------------------------------------------

/** The side of the world square at that zoom, in pixels. */
export const worldSize = zoom => TILE * 2 ** zoom;

/** A latitude kept inside the square world of Web Mercator. */
export const clampLatitude = latitude => Math.max(-MAX_LATITUDE, Math.min(MAX_LATITUDE, latitude));

/** A longitude brought back between -180 and 180. */
export const normalizeLongitude = longitude => (longitude >= -180 && longitude <= 180 ? longitude : ((((longitude + 180) % 360) + 360) % 360) - 180);

/** The world pixel of a point at a zoom. */
export function project(latitude, longitude, zoom) {
    const size = worldSize(zoom);
    const phi = clampLatitude(latitude) * Math.PI / 180;
    return {
        x: (longitude + 180) / 360 * size,
        y: (1 - Math.log(Math.tan(phi) + 1 / Math.cos(phi)) / Math.PI) / 2 * size
    };
}

/** The point at a world pixel of a zoom (the inverse of project). */
export function unproject(x, y, zoom) {
    const size = worldSize(zoom);
    const latitude = Math.atan(Math.sinh(Math.PI * (1 - 2 * y / size))) * 180 / Math.PI;
    return { latitude: clampLatitude(latitude), longitude: normalizeLongitude(x / size * 360 - 180) };
}

/** The world pixel of the top left corner of a view of width x height centred on a point. */
export function originOf(view, width, height) {
    const centre = project(view.latitude, view.longitude, view.zoom);
    return { x: centre.x - width / 2, y: centre.y - height / 2 };
}

/**
 * The tiles a view of width x height at that origin needs, with one ring around: their columns and rows, the rows kept
 * inside the world (there is nothing above or below it), the columns not (they repeat, see tileColumn).
 */
export function tileRange(origin, width, height, zoom) {
    const last = 2 ** zoom - 1;
    return {
        minX: Math.floor(origin.x / TILE) - 1,
        maxX: Math.floor((origin.x + width) / TILE) + 1,
        minY: Math.max(0, Math.floor(origin.y / TILE) - 1),
        maxY: Math.min(last, Math.floor((origin.y + height) / TILE) + 1)
    };
}

/** The column of the tile to load for a column of the view: the world repeats east and west. */
export const tileColumn = (column, zoom) => ((column % 2 ** zoom) + 2 ** zoom) % 2 ** zoom;

/** The address of a tile, from a template holding {z}, {x} and {y}. */
export const tileUrl = (template, zoom, column, row) => template.replace('{z}', zoom).replace('{x}', column).replace('{y}', row);

/** The view after moving its centre by dx, dy pixels of the screen (a drag of -dx, -dy). */
export function panBy(view, dx, dy) {
    const centre = project(view.latitude, view.longitude, view.zoom);
    return { ...unproject(centre.x + dx, centre.y + dy, view.zoom), zoom: view.zoom };
}

/** The view at another zoom that keeps the point under (px, py) of a view of width x height where it is. */
export function zoomAround(view, width, height, px, py, zoom) {
    const origin = originOf(view, width, height);
    const anchor = unproject(origin.x + px, origin.y + py, view.zoom);
    const moved = project(anchor.latitude, anchor.longitude, zoom);
    return { ...unproject(moved.x - px + width / 2, moved.y - py + height / 2, zoom), zoom };
}

/**
 * The view that frames points ([latitude, longitude] pairs) in width x height with a margin: the highest zoom between
 * minZoom and maxZoom at which they all fit, centred on their box; a single place is shown at POINT_ZOOM at most. Null
 * without a point.
 */
export function fitView(points, width, height, padding, minZoom, maxZoom) {
    if (points.length === 0) {
        return null;
    }

    let left = Infinity; let right = -Infinity; let top = Infinity; let bottom = -Infinity;
    for (const [latitude, longitude] of points) {
        const pixel = project(latitude, longitude, 0);
        left = Math.min(left, pixel.x);
        right = Math.max(right, pixel.x);
        top = Math.min(top, pixel.y);
        bottom = Math.max(bottom, pixel.y);
    }

    const across = right - left;
    const down = bottom - top;
    const room = Math.min(
        across > 0 ? Math.max(1, width - 2 * padding) / across : Infinity,
        down > 0 ? Math.max(1, height - 2 * padding) / down : Infinity);
    const wanted = Number.isFinite(room) ? Math.floor(Math.log2(room) + 1e-9) : Math.min(maxZoom, POINT_ZOOM);
    const zoom = Math.max(minZoom, Math.min(maxZoom, wanted));
    return { ...unproject((left + right) / 2, (top + bottom) / 2, 0), zoom };
}

// ----- Drawing -----------------------------------------------------------------------------------------------------

const tenth = value => Math.round(value * 10) / 10;
const hundredth = value => Math.round(value * 100) / 100;
const viewOf = map => ({ latitude: map.latitude, longitude: map.longitude, zoom: map.zoom });

function setView(map, view) {
    map.latitude = clampLatitude(view.latitude);
    map.longitude = normalizeLongitude(view.longitude);
    map.zoom = Math.max(map.minZoom, Math.min(map.maxZoom, Math.round(view.zoom)));
}

function placeTiles(map, origin) {
    const wanted = new Set();
    if (map.template && map.width > 0 && map.height > 0) {
        const range = tileRange(origin, map.width, map.height, map.zoom);
        for (let row = range.minY; row <= range.maxY; row++) {
            for (let column = range.minX; column <= range.maxX; column++) {
                const key = `${map.zoom}/${column}/${row}`;
                wanted.add(key);
                const image = map.images.get(key) ?? addTile(map, key, tileUrl(map.template, map.zoom, tileColumn(column, map.zoom), row));
                image.style.setProperty('--omni-map-tile-x', `${column * TILE - origin.x}px`);
                image.style.setProperty('--omni-map-tile-y', `${row * TILE - origin.y}px`);
            }
        }
    }

    for (const [key, image] of map.images) {
        if (!wanted.has(key)) {
            image.remove();
            map.images.delete(key);
        }
    }
}

function addTile(map, key, source) {
    const image = document.createElement('img');
    image.className = 'omni-map__tile';
    image.alt = '';
    image.decoding = 'async';
    image.draggable = false;
    // OpenStreetMap refuses a tile asked without a referrer; the origin alone is sent, whatever the page's policy.
    image.referrerPolicy = 'strict-origin-when-cross-origin';
    image.addEventListener('load', () => image.classList.add('omni-map__tile--loaded'), { once: true });
    image.addEventListener('error', () => image.classList.add('omni-map__tile--failed'), { once: true });
    image.src = source;
    map.tiles.append(image);
    map.images.set(key, image);
    return image;
}

/** The "d" of a line through [latitude, longitude, latitude, longitude...] in pixels of a view at that origin. */
export function pathOf(flat, zoom, origin) {
    const parts = [];
    for (let index = 0; index + 1 < flat.length; index += 2) {
        const pixel = project(flat[index], flat[index + 1], zoom);
        parts.push(`${index === 0 ? 'M' : 'L'}${tenth(pixel.x - origin.x)} ${tenth(pixel.y - origin.y)}`);
    }

    return parts.join(' ');
}

// The origin of the drawing, on a whole pixel: the tiles meet without a seam, and the lines, the markers and a click sit
// on the same pixels as the tiles.
function drawnOrigin(map) {
    const exact = originOf(viewOf(map), map.width, map.height);
    return { x: Math.round(exact.x), y: Math.round(exact.y) };
}

function render(map) {
    const origin = drawnOrigin(map);
    placeTiles(map, origin);
    const groups = map.root.querySelectorAll('[data-omni-map-line]');
    groups.forEach((group, index) => {
        const flat = map.lines[index];
        const d = flat ? pathOf(flat, map.zoom, origin) : '';
        group.querySelectorAll('path').forEach(path => path.setAttribute('d', d));
    });
    const buttons = map.root.querySelectorAll('[data-omni-map-marker]');
    map.placed = [];
    buttons.forEach((button, index) => {
        const point = map.markers[index];
        if (!point) {
            return;
        }

        const pixel = project(point[0], point[1], map.zoom);
        const place = { x: hundredth(pixel.x - origin.x), y: hundredth(pixel.y - origin.y) };
        map.placed[index] = place;
        button.style.setProperty('--omni-map-x', `${place.x}px`);
        button.style.setProperty('--omni-map-y', `${place.y}px`);
        button.setAttribute('data-omni-map-placed', '');
    });
}

function tellView(map) {
    const told = map.told;
    if (told && told.latitude === map.latitude && told.longitude === map.longitude && told.zoom === map.zoom) {
        return;
    }

    map.told = viewOf(map);
    notify(map.dotnet, 'OnViewChanged', map.latitude, map.longitude, map.zoom);
}

function frame(map) {
    const points = [...map.markers];
    for (const flat of map.lines) {
        for (let index = 0; index + 1 < flat.length; index += 2) {
            points.push([flat[index], flat[index + 1]]);
        }
    }

    map.pendingFit = false;
    const view = fitView(points, map.width, map.height, FIT_PADDING, map.minZoom, map.maxZoom);
    if (view) {
        setView(map, view);
    }
}

// Moves the view, draws it and tells .NET.
function move(map, view) {
    setView(map, view);
    render(map);
    tellView(map);
}

// ----- Reader input ------------------------------------------------------------------------------------------------

const pointerIn = (map, event) => {
    const box = map.root.getBoundingClientRect();
    return { x: event.clientX - box.left, y: event.clientY - box.top };
};

function onPointerDown(map, event) {
    if ((event.pointerType === 'mouse' && event.button !== 0) || event.target?.closest?.(CONTROLS)) {
        return;
    }

    const centre = project(map.latitude, map.longitude, map.zoom);
    map.drag = { id: event.pointerId, x: event.clientX, y: event.clientY, centre, moved: false };
    try {
        map.root.setPointerCapture?.(event.pointerId);
    } catch {
        // A pointer already released cannot be captured; the drag still follows it while it is over the map.
    }
}

function onPointerMove(map, event) {
    const drag = map.drag;
    if (!drag || drag.id !== event.pointerId) {
        return;
    }

    const dx = event.clientX - drag.x;
    const dy = event.clientY - drag.y;
    if (!drag.moved && Math.hypot(dx, dy) < DRAG_THRESHOLD) {
        return;
    }

    drag.moved = true;
    map.root.classList.add('omni-map--dragging');
    setView(map, { ...unproject(drag.centre.x - dx, drag.centre.y - dy, map.zoom), zoom: map.zoom });
    render(map);
}

function onPointerUp(map, event, cancelled) {
    const drag = map.drag;
    if (!drag || drag.id !== event.pointerId) {
        return;
    }

    map.drag = null;
    map.root.classList.remove('omni-map--dragging');
    if (drag.moved) {
        tellView(map);
    } else if (!cancelled) {
        const pointer = pointerIn(map, event);
        const origin = drawnOrigin(map);
        const place = unproject(origin.x + pointer.x, origin.y + pointer.y, map.zoom);
        notify(map.dotnet, 'OnMapClick', place.latitude, place.longitude);
    }
}

function zoomAt(map, pointer, delta) {
    const zoom = Math.max(map.minZoom, Math.min(map.maxZoom, map.zoom + delta));
    if (zoom !== map.zoom) {
        move(map, zoomAround(viewOf(map), map.width, map.height, pointer.x, pointer.y, zoom));
    }
}

function onWheel(map, event) {
    event.preventDefault();
    const unit = event.deltaMode === 1 ? 40 : event.deltaMode === 2 ? 800 : 1;
    map.wheel += event.deltaY * unit;
    if (Math.abs(map.wheel) < WHEEL_STEP) {
        return;
    }

    const delta = map.wheel < 0 ? 1 : -1;
    map.wheel = 0;
    zoomAt(map, pointerIn(map, event), delta);
}

function onDoubleClick(map, event) {
    if (event.target?.closest?.(CONTROLS)) {
        return;
    }

    event.preventDefault();
    zoomAt(map, pointerIn(map, event), event.shiftKey ? -1 : 1);
}

const KEYS = {
    ArrowLeft: [-KEY_STEP, 0], ArrowRight: [KEY_STEP, 0], ArrowUp: [0, -KEY_STEP], ArrowDown: [0, KEY_STEP]
};

function onKeyDown(map, event) {
    if (event.target !== map.root || event.altKey || event.ctrlKey || event.metaKey) {
        return;
    }

    const step = KEYS[event.key];
    if (step) {
        event.preventDefault();
        move(map, panBy(viewOf(map), step[0], step[1]));
    } else if (event.key === '+' || event.key === '=' || event.key === '-' || event.key === '_') {
        event.preventDefault();
        zoomAt(map, { x: map.width / 2, y: map.height / 2 }, event.key === '+' || event.key === '=' ? 1 : -1);
    }
}

// A marker reached by Tab while it is outside the view comes into it: the view centres on it.
function onFocusIn(map, event) {
    const target = event.target;
    if (!target?.matches?.('[data-omni-map-marker]')) {
        return;
    }

    const index = [...map.root.querySelectorAll('[data-omni-map-marker]')].indexOf(target);
    const place = map.placed[index];
    if (place && (place.x < 0 || place.y < 0 || place.x > map.width || place.y > map.height)) {
        const point = map.markers[index];
        move(map, { latitude: point[0], longitude: point[1], zoom: map.zoom });
    }
}

function onResize(map) {
    const width = map.root.clientWidth;
    const height = map.root.clientHeight;
    if (width === map.width && height === map.height) {
        return;
    }

    map.width = width;
    map.height = height;
    const fitting = map.pendingFit && width > 0 && height > 0;
    if (fitting) {
        frame(map);
    }

    render(map);
    if (fitting) {
        tellView(map);
    }
}

// ----- .NET entry points -------------------------------------------------------------------------------------------

/** Starts drawing the map in its element and following the reader's input; .NET then gives it its view and content by update. */
export function attach(root, dotnet) {
    const tiles = root?.querySelector('.omni-map__tiles');
    if (!tiles || maps.has(root)) {
        return;
    }

    const map = {
        root, dotnet, tiles, images: new Map(), template: null, minZoom: 0, maxZoom: 22, latitude: 0, longitude: 0, zoom: 0,
        lines: [], markers: [], placed: [], width: root.clientWidth, height: root.clientHeight, drag: null, wheel: 0,
        pendingFit: false, told: null
    };
    const handlers = {
        pointerdown: event => onPointerDown(map, event),
        pointermove: event => onPointerMove(map, event),
        pointerup: event => onPointerUp(map, event, false),
        pointercancel: event => onPointerUp(map, event, true),
        wheel: event => onWheel(map, event),
        dblclick: event => onDoubleClick(map, event),
        keydown: event => onKeyDown(map, event),
        focusin: event => onFocusIn(map, event)
    };
    for (const [type, handler] of Object.entries(handlers)) {
        root.addEventListener(type, handler, type === 'wheel' ? { passive: false } : undefined);
    }

    const observer = new ResizeObserver(() => onResize(map));
    observer.observe(root);
    map.stop = () => {
        for (const [type, handler] of Object.entries(handlers)) {
            root.removeEventListener(type, handler, type === 'wheel' ? { passive: false } : undefined);
        }

        observer.disconnect();
    };
    maps.set(root, map);
}

/**
 * Takes what .NET changed: the tile template and the zoom range (always), the lines (flat [latitude, longitude...] arrays)
 * and the markers ([latitude, longitude] pairs) when they changed, else null, the view [latitude, longitude, zoom] it asks
 * for, else null, and whether to frame the content (now if the map has a size, else as soon as it has one). Draws, and
 * tells .NET the view when it differs from the one asked (a zoom kept in range, a framing).
 */
export function update(root, template, minZoom, maxZoom, lines, markers, view, fit) {
    const map = root ? maps.get(root) : undefined;
    if (!map) {
        return;
    }

    map.template = template;
    map.minZoom = minZoom;
    map.maxZoom = maxZoom;
    if (lines) {
        map.lines = lines;
    }

    if (markers) {
        map.markers = markers;
    }

    if (view) {
        setView(map, { latitude: view[0], longitude: view[1], zoom: view[2] });
        // .NET holds this view already: telling it back would be an echo.
        map.told = viewOf(map);
    } else {
        setView(map, viewOf(map));
    }

    if (fit) {
        map.pendingFit = true;
        if (map.width > 0 && map.height > 0) {
            frame(map);
        }
    }

    render(map);
    tellView(map);
}

/** Zooms by delta levels around the centre of the view (the + and - buttons). */
export function zoomBy(root, delta) {
    const map = root ? maps.get(root) : undefined;
    if (map) {
        zoomAt(map, { x: map.width / 2, y: map.height / 2 }, delta);
    }
}

/** Stops following the input and the size, and removes the tiles. */
export function detach(root) {
    const map = root ? maps.get(root) : undefined;
    if (!map) {
        return;
    }

    map.stop();
    for (const image of map.images.values()) {
        image.remove();
    }

    map.images.clear();
    maps.delete(root);
}
