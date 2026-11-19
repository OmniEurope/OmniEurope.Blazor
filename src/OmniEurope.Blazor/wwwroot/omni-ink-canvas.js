// Drawing pad of OmniInkCanvas.
//
// .NET owns the strokes (the value of the component); this module owns the pixels and the pointer. It reads the
// pointer events of the canvas (mouse, pen with its pressure, finger: touch-action is none in the stylesheet, and
// the pointer is captured so a line goes on outside the canvas), draws the stroke being put down, and reports it to
// .NET when the pointer is released. .NET sends the strokes back (setStrokes) when they changed otherwise: undo,
// clear, a value given by the host. With the eraser tool (data-omni-ink-tool) the pointer draws nothing: it follows a
// path, strokes it touches are shown faded, and on release their indices are reported (OnErase, with the number of
// strokes held so .NET can tell a stale list) and removed here at once. It never writes a style on the canvas; the size of the drawing area goes
// through custom properties on the component root (setLayout), the one path a strict style-src leaves open.
//
// Strokes cross as plain numbers: colours[], widths[] and, per stroke, a flat array of x, y, pressure triples
// (a pressure of -1 meaning none). x and y are 0..1 of the canvas, and a line width is expressed at a canvas 600
// CSS pixels wide (REFERENCE_WIDTH, OmniInkStroke.ReferenceWidth) and scales with the canvas width, so a drawing
// keeps its look at any size.
import { REFERENCE_WIDTH, THEME_COLOR, MIN_LINE, unpack, pressureFactor, drawStroke, paint } from './shared/ink-render.js';
import { circuitDown } from './shared/notify.js';

const EXPORT_THEME_COLOR = '#000000';
const MIN_WIDTH = 0.25;
const MAX_WIDTH = 48;
// A sample closer than this (CSS pixels) to the last one is dropped.
const MIN_STEP = 0.5;
// The eraser touches a stroke within this many CSS pixels of its drawn edge, so a thin line is easy to hit.
const ERASER_TOLERANCE = 5;
const pads = new WeakMap();

// ----- Eraser hit-test, in CSS pixels -----

const cross = (ax, ay, bx, by, cx, cy) => (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);

function distanceToSegment(px, py, ax, ay, bx, by) {
    const dx = bx - ax;
    const dy = by - ay;
    const lengthSquared = dx * dx + dy * dy;
    const along = lengthSquared === 0 ? 0 : Math.min(1, Math.max(0, ((px - ax) * dx + (py - ay) * dy) / lengthSquared));
    return Math.hypot(px - (ax + along * dx), py - (ay + along * dy));
}

// The smallest distance between the segments a-b and c-d (a segment may be a single point).
function segmentDistance(a, b, c, d) {
    const crosses = cross(a.x, a.y, b.x, b.y, c.x, c.y) * cross(a.x, a.y, b.x, b.y, d.x, d.y) < 0
        && cross(c.x, c.y, d.x, d.y, a.x, a.y) * cross(c.x, c.y, d.x, d.y, b.x, b.y) < 0;
    if (crosses) {
        return 0;
    }

    return Math.min(
        distanceToSegment(a.x, a.y, c.x, c.y, d.x, d.y),
        distanceToSegment(b.x, b.y, c.x, c.y, d.x, d.y),
        distanceToSegment(c.x, c.y, a.x, a.y, b.x, b.y),
        distanceToSegment(d.x, d.y, a.x, a.y, b.x, b.y));
}

// Whether the eraser path segment a-b touches the stroke: within half of the drawn width of the stretch it meets (the
// width a pen gives it by pressure), plus the tolerance.
function touches(stroke, a, b, width, height) {
    const unit = stroke.width * (width / REFERENCE_WIDTH);
    const px = stroke.points.map(point => ({ x: point.x * width, y: point.y * height }));
    if (px.length === 1) {
        const half = Math.max(unit * pressureFactor(stroke.points[0].pressure), MIN_LINE) / 2;
        return distanceToSegment(px[0].x, px[0].y, a.x, a.y, b.x, b.y) <= half + ERASER_TOLERANCE;
    }

    for (let at = 1; at < px.length; at++) {
        const pressure = (stroke.points[at - 1].pressure ?? 0.5) / 2 + (stroke.points[at].pressure ?? 0.5) / 2;
        const half = Math.max(unit * pressureFactor(pressure), MIN_LINE) / 2;
        if (segmentDistance(a, b, px[at - 1], px[at]) <= half + ERASER_TOLERANCE) {
            return true;
        }
    }

    return false;
}

// Adds to hits the indices of the strokes the segment a-b (CSS pixels) touches.
function sweep(pad, a, b, hits) {
    const width = pad.canvas.clientWidth;
    const height = pad.canvas.clientHeight;
    pad.strokes.forEach((stroke, index) => {
        if (!hits.has(index) && touches(stroke, a, b, width, height)) {
            hits.add(index);
        }
    });
}

// Backing store at the device resolution, drawing in CSS pixels; skipped while the canvas has no size (hidden).
function redraw(pad) {
    const canvas = pad.canvas;
    const cssWidth = canvas.clientWidth;
    const cssHeight = canvas.clientHeight;
    if (cssWidth === 0 || cssHeight === 0) {
        return;
    }

    const ratio = window.devicePixelRatio || 1;
    const pixelWidth = Math.max(1, Math.round(cssWidth * ratio));
    const pixelHeight = Math.max(1, Math.round(cssHeight * ratio));
    if (canvas.width !== pixelWidth || canvas.height !== pixelHeight) {
        canvas.width = pixelWidth;
        canvas.height = pixelHeight;
    }

    const context = canvas.getContext('2d');
    context.setTransform(pixelWidth / cssWidth, 0, 0, pixelHeight / cssHeight, 0, 0);
    context.clearRect(0, 0, cssWidth, cssHeight);
    const text = getComputedStyle(canvas).color;
    const inkOf = color => (color === THEME_COLOR ? text : color);
    paint(context, pad.strokes, cssWidth, cssHeight, inkOf, pad.erasing?.hits);
    if (pad.active) {
        drawStroke(context, pad.active.stroke, cssWidth, cssHeight, inkOf);
    }
}

const clamp01 = value => Math.min(1, Math.max(0, value));
const round = value => Math.round(value * 100000) / 100000;

function pointOf(canvas, event) {
    const box = canvas.getBoundingClientRect();
    return {
        x: round(clamp01((event.clientX - box.left) / box.width)),
        y: round(clamp01((event.clientY - box.top) / box.height)),
        // Only a pen gives a trustworthy pressure: a mouse reports a fixed one and a finger often none.
        pressure: event.pointerType === 'pen' ? round(clamp01(event.pressure)) : null
    };
}

const flatten = points => points.flatMap(point => [point.x, point.y, point.pressure ?? -1]);

const isLocked = canvas => canvas.dataset.omniInkLocked === 'true';
const isEraser = canvas => canvas.dataset.omniInkTool === 'eraser';

function tell(dotnet, method, ...values) {
    // No call while Blazor Server shows its connection as lost: it could not arrive, and Blazor would report
    // its failed send as an uncaught rejection, out of reach of the catch below.
    if (circuitDown()) {
        return;
    }

    try {
        const pending = dotnet.invokeMethodAsync(method, ...values);
        pending?.catch?.(() => { });
    } catch {
        // The component is gone; there is nobody left to tell.
    }
}

// The computed colour as #rrggbb, the alpha ignored: the colour field of the toolbar only takes a hex. Null when
// the browser gave nothing this can read (no rgb() form yet).
function resolveHex(canvas) {
    const match = /^rgba?\(\s*([\d.]+)[\s,]+([\d.]+)[\s,]+([\d.]+)/i.exec(getComputedStyle(canvas).color ?? '');
    if (!match) {
        return null;
    }

    return '#' + match.slice(1, 4).map(part => Math.min(255, Math.round(Number.parseFloat(part))).toString(16).padStart(2, '0')).join('');
}

/**
 * Attaches the pointer and keyboard handling to the canvas. Returns the theme colour resolved on the canvas as a
 * #rrggbb hex (null when unresolved); a later change of it is told to .NET by OnThemeColor.
 */
export function attach(canvas, dotnet) {
    if (!canvas || pads.has(canvas)) {
        return null;
    }

    const pad = { canvas, strokes: [], active: null, erasing: null, observers: [], hex: resolveHex(canvas) };

    const cssPoint = event => {
        const box = canvas.getBoundingClientRect();
        return { x: event.clientX - box.left, y: event.clientY - box.top };
    };

    const onPointerDown = event => {
        if (pad.active || !event.isPrimary || (event.pointerType === 'mouse' && event.button !== 0) || isLocked(canvas)) {
            return;
        }

        const width = Number.parseFloat(canvas.dataset.omniInkWidth);
        event.preventDefault();
        canvas.focus({ preventScroll: true });
        canvas.setPointerCapture(event.pointerId);
        if (isEraser(canvas)) {
            const start = cssPoint(event);
            pad.erasing = { pointerId: event.pointerId, last: start, hits: new Set() };
            sweep(pad, start, start, pad.erasing.hits);
            redraw(pad);
            return;
        }

        pad.active = {
            pointerId: event.pointerId,
            stroke: {
                color: canvas.dataset.omniInkColor || THEME_COLOR,
                width: Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, Number.isFinite(width) ? width : 2)),
                points: [pointOf(canvas, event)]
            }
        };
        redraw(pad);
    };

    const onPointerMove = event => {
        if (pad.erasing && event.pointerId === pad.erasing.pointerId) {
            const samples = event.getCoalescedEvents?.() ?? [];
            const before = pad.erasing.hits.size;
            for (const sample of samples.length > 0 ? samples : [event]) {
                const next = cssPoint(sample);
                sweep(pad, pad.erasing.last, next, pad.erasing.hits);
                pad.erasing.last = next;
            }

            if (pad.erasing.hits.size !== before) {
                redraw(pad);
            }

            return;
        }

        if (!pad.active || event.pointerId !== pad.active.pointerId) {
            return;
        }

        const points = pad.active.stroke.points;
        // A pen reports several samples per event: all of them are kept, so a fast line stays smooth.
        const samples = event.getCoalescedEvents?.() ?? [];
        for (const sample of samples.length > 0 ? samples : [event]) {
            const point = pointOf(canvas, sample);
            const last = points[points.length - 1];
            if (Math.hypot((point.x - last.x) * canvas.clientWidth, (point.y - last.y) * canvas.clientHeight) >= MIN_STEP) {
                points.push(point);
            }
        }

        redraw(pad);
    };

    const finishErase = event => {
        const hits = [...pad.erasing.hits].sort((first, second) => first - second);
        const count = pad.strokes.length;
        pad.erasing = null;
        if (event.type === 'pointercancel' || hits.length === 0) {
            redraw(pad);
            return;
        }

        pad.strokes = pad.strokes.filter((_, index) => !hits.includes(index));
        redraw(pad);
        tell(dotnet, 'OnErase', hits, count);
    };

    const finish = event => {
        if (pad.erasing && event.pointerId === pad.erasing.pointerId) {
            finishErase(event);
            return;
        }

        if (!pad.active || event.pointerId !== pad.active.pointerId) {
            return;
        }

        const stroke = pad.active.stroke;
        pad.active = null;
        if (event.type === 'pointercancel') {
            redraw(pad);
            return;
        }

        pad.strokes.push(stroke);
        tell(dotnet, 'OnStroke', stroke.color, stroke.width, flatten(stroke.points));
    };

    const onKeyDown = event => {
        if (pad.active || isLocked(canvas) || event.altKey) {
            return;
        }

        const modifier = event.ctrlKey || event.metaKey;
        const undo = modifier && !event.shiftKey && event.key.toLowerCase() === 'z';
        const clear = !modifier && event.key === 'Delete';
        if (!undo && !clear) {
            return;
        }

        event.preventDefault();
        tell(dotnet, undo ? 'OnUndo' : 'OnClear');
    };

    // One repaint for every reason the pixels may be stale: a new size, a new theme colour, a new colour scheme.
    const repaint = () => redraw(pad);
    const retheme = () => {
        repaint();
        const hex = resolveHex(canvas);
        if (hex && hex !== pad.hex) {
            pad.hex = hex;
            tell(dotnet, 'OnThemeColor', hex);
        }
    };

    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerup', finish);
    canvas.addEventListener('pointercancel', finish);
    canvas.addEventListener('keydown', onKeyDown);

    const resize = new ResizeObserver(repaint);
    resize.observe(canvas);
    const scheme = window.matchMedia('(prefers-color-scheme: dark)');
    scheme.addEventListener('change', retheme);
    const scope = canvas.closest('[data-omni-theme]') ?? document.documentElement;
    const theme = new MutationObserver(retheme);
    theme.observe(scope, { attributes: true, attributeFilter: ['data-omni-theme', 'style', 'class'] });

    pad.release = () => {
        canvas.removeEventListener('pointerdown', onPointerDown);
        canvas.removeEventListener('pointermove', onPointerMove);
        canvas.removeEventListener('pointerup', finish);
        canvas.removeEventListener('pointercancel', finish);
        canvas.removeEventListener('keydown', onKeyDown);
        resize.disconnect();
        theme.disconnect();
        scheme.removeEventListener('change', retheme);
    };

    pads.set(canvas, pad);
    return pad.hex;
}

export function detach(canvas) {
    const pad = canvas ? pads.get(canvas) : undefined;
    if (!pad) {
        return;
    }

    pad.release();
    pads.delete(canvas);
}

export function setStrokes(canvas, colors, widths, points) {
    const pad = canvas ? pads.get(canvas) : undefined;
    if (!pad) {
        return;
    }

    pad.strokes = unpack(colors, widths, points);
    // The indices the eraser had gathered belong to the list it replaces.
    pad.erasing?.hits.clear();
    redraw(pad);
}

const setOrRemove = (element, name, value) => {
    if (value === null || value === undefined) {
        element.style.removeProperty(name);
    } else {
        element.style.setProperty(name, value);
    }
};

// The size of the drawing area, as custom properties of the component root. With an aspect ratio the height
// follows the width (auto), so a stale height is not left behind.
export function setLayout(root, width, height, aspect) {
    if (!root) {
        return;
    }

    setOrRemove(root, '--omni-ink-width', width);
    setOrRemove(root, '--omni-ink-height', aspect ? 'auto' : height);
    setOrRemove(root, '--omni-ink-aspect', aspect ? String(aspect) : null);
}

// The strokes on a transparent canvas of the asked size, as the bytes of a PNG. No theme there: a stroke in the
// theme colour is black.
export async function exportPng(colors, widths, points, width, height) {
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    paint(context, unpack(colors, widths, points), width, height, color => (color === THEME_COLOR ? EXPORT_THEME_COLOR : color));
    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!blob) {
        throw new Error('The browser could not encode the drawing as a PNG image.');
    }

    return new Uint8Array(await blob.arrayBuffer());
}
