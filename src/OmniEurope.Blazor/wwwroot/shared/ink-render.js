// The stroke renderer shared by the scripts that draw an OmniInkCanvas drawing: omni-ink-canvas.js (the pad itself and
// its PNG export) and omni-media-tile.js (the strokes laid over a video frame it exports). One renderer, so a stroke
// looks the same on the pad, in an export and over a picture.
//
// Strokes cross from .NET as plain numbers: colours[], widths[] and, per stroke, a flat array of x, y, pressure
// triples (a pressure of -1 meaning none). x and y are 0..1 of the drawing area, and a line width is expressed at an
// area 600 CSS pixels wide (REFERENCE_WIDTH, OmniInkStroke.ReferenceWidth) and scales with the area width, so a
// drawing keeps its look at any size.
export const REFERENCE_WIDTH = 600;
export const THEME_COLOR = 'currentColor';
export const MIN_LINE = 0.5;
// A stroke the eraser touched but did not remove yet is drawn at this opacity.
const FADED_ALPHA = 0.3;

export const unpack = (colors, widths, flats) => colors.map((color, index) => {
    const flat = flats[index];
    const points = [];
    for (let at = 0; at + 2 < flat.length; at += 3) {
        points.push({ x: flat[at], y: flat[at + 1], pressure: flat[at + 2] < 0 ? null : flat[at + 2] });
    }

    return { color, width: widths[index], points };
});

// A pressure of 0.5 draws the stroke width; a missing pressure counts as 0.5.
export const pressureFactor = pressure => 0.5 + (pressure ?? 0.5);

export function drawStroke(context, stroke, width, height, inkOf) {
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

export function paint(context, strokes, width, height, inkOf, faded) {
    strokes.forEach((stroke, index) => {
        context.globalAlpha = faded?.has(index) ? FADED_ALPHA : 1;
        drawStroke(context, stroke, width, height, inkOf);
    });
    context.globalAlpha = 1;
}
