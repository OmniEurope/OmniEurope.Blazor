// Drawing pad of OmniInkCanvas.
//
// .NET owns the strokes (the value of the component); this module owns the pixels and the pointer. It reads the
// pointer events of the canvas (mouse, pen with its pressure, finger: touch-action is none in the stylesheet, and
// the pointer is captured so a line goes on outside the canvas), draws the stroke being put down, and reports it to
// .NET when the pointer is released. .NET sends the strokes back (setStrokes) when they changed otherwise: undo,
// clear, a value given by the host. It never writes a style on the canvas; the size of the drawing area goes
// through custom properties on the component root (setLayout), the one path a strict style-src leaves open.
//
// Strokes cross as plain numbers: colours[], widths[] and, per stroke, a flat array of x, y, pressure triples
// (a pressure of -1 meaning none). x and y are 0..1 of the canvas, and a line width is expressed at a canvas 600
// CSS pixels wide (REFERENCE_WIDTH, OmniInkStroke.ReferenceWidth) and scales with the canvas width, so a drawing
// keeps its look at any size.
const REFERENCE_WIDTH = 600;
const THEME_COLOR = 'currentColor';
const EXPORT_THEME_COLOR = '#000000';
const MIN_WIDTH = 0.25;
const MAX_WIDTH = 48;
const MIN_LINE = 0.5;
// A sample closer than this (CSS pixels) to the last one is dropped.
const MIN_STEP = 0.5;
const pads = new WeakMap();

const unpack = (colors, widths, flats) => colors.map((color, index) => {
    const flat = flats[index];
    const points = [];
    for (let at = 0; at + 2 < flat.length; at += 3) {
        points.push({ x: flat[at], y: flat[at + 1], pressure: flat[at + 2] < 0 ? null : flat[at + 2] });
    }

    return { color, width: widths[index], points };
});

// A pressure of 0.5 draws the stroke width; a missing pressure counts as 0.5.
const pressureFactor = pressure => 0.5 + (pressure ?? 0.5);

function drawStroke(context, stroke, width, height, inkOf) {
    const points = stroke.points;
    if (points.length === 0) {
        return;
    }

    const unit = stroke.width * (width / REFERENCE_WIDTH);
    const ink = inkOf(stroke.color);
    context.strokeStyle = ink;
    context.fillStyle = ink;
    context.lineCap = 'round';
    context.lineJoin = 'round';
    if (points.length === 1) {
        context.beginPath();
        context.arc(points[0].x * width, points[0].y * height, Math.max(unit * pressureFactor(points[0].pressure), MIN_LINE) / 2, 0, Math.PI * 2);
        context.fill();
        return;
    }

    if (points.some(point => point.pressure !== null)) {
        // A pen: the line is as wide as the pressure of each stretch says.
        for (let at = 1; at < points.length; at++) {
            context.lineWidth = Math.max(unit * pressureFactor((points[at - 1].pressure ?? 0.5) / 2 + (points[at].pressure ?? 0.5) / 2), MIN_LINE);
            context.beginPath();
            context.moveTo(points[at - 1].x * width, points[at - 1].y * height);
            context.lineTo(points[at].x * width, points[at].y * height);
            context.stroke();
        }

        return;
    }

    // A mouse or a finger: one smooth curve through the middles of the segments.
    context.lineWidth = Math.max(unit, MIN_LINE);
    context.beginPath();
    context.moveTo(points[0].x * width, points[0].y * height);
    for (let at = 1; at < points.length - 1; at++) {
        const middleX = (points[at].x + points[at + 1].x) / 2;
        const middleY = (points[at].y + points[at + 1].y) / 2;
        context.quadraticCurveTo(points[at].x * width, points[at].y * height, middleX * width, middleY * height);
    }

    const last = points[points.length - 1];
    context.lineTo(last.x * width, last.y * height);
    context.stroke();
}

function paint(context, strokes, width, height, inkOf) {
    for (const stroke of strokes) {
        drawStroke(context, stroke, width, height, inkOf);
    }
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
    paint(context, pad.strokes, cssWidth, cssHeight, inkOf);
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

function tell(dotnet, method, ...values) {
    try {
        const pending = dotnet.invokeMethodAsync(method, ...values);
        pending?.catch?.(() => { });
    } catch {
        // The component is gone; there is nobody left to tell.
    }
}

export function attach(canvas, dotnet) {
    if (!canvas || pads.has(canvas)) {
        return;
    }

    const pad = { canvas, strokes: [], active: null, observers: [] };

    const onPointerDown = event => {
        if (pad.active || !event.isPrimary || (event.pointerType === 'mouse' && event.button !== 0) || isLocked(canvas)) {
            return;
        }

        const width = Number.parseFloat(canvas.dataset.omniInkWidth);
        event.preventDefault();
        canvas.focus({ preventScroll: true });
        canvas.setPointerCapture(event.pointerId);
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

    const finish = event => {
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

    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerup', finish);
    canvas.addEventListener('pointercancel', finish);
    canvas.addEventListener('keydown', onKeyDown);

    const resize = new ResizeObserver(repaint);
    resize.observe(canvas);
    const scheme = window.matchMedia('(prefers-color-scheme: dark)');
    scheme.addEventListener('change', repaint);
    const scope = canvas.closest('[data-omni-theme]') ?? document.documentElement;
    const theme = new MutationObserver(repaint);
    theme.observe(scope, { attributes: true, attributeFilter: ['data-omni-theme', 'style', 'class'] });

    pad.release = () => {
        canvas.removeEventListener('pointerdown', onPointerDown);
        canvas.removeEventListener('pointermove', onPointerMove);
        canvas.removeEventListener('pointerup', finish);
        canvas.removeEventListener('pointercancel', finish);
        canvas.removeEventListener('keydown', onKeyDown);
        resize.disconnect();
        theme.disconnect();
        scheme.removeEventListener('change', repaint);
    };

    pads.set(canvas, pad);
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
