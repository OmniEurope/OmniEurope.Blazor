// Part of omni-grid.js, which re-exports attachResize, detachResize and autoFitColumn: the drag
// that resizes a column and the fit of a column to its widest content.
import { notifyDotNet } from './notify.js';

const resizeAttachments = new Map();

/**
 * Drag-to-resize on the handle straddling each column's trailing border. The whole gesture stays in
 * the browser: the live width lands on the matching <col> through a custom property (no style
 * attribute, so the strict CSP still holds), and .NET is told once, on release. Delegation from the
 * viewport means handles added by a later render need no re-attachment.
 */
export function attachResize(viewport, reference, minimumWidth) {
    if (!(viewport instanceof HTMLElement) || !reference) {
        return;
    }

    detachResize(viewport);

    const floor = typeof minimumWidth === 'number' && minimumWidth > 0 ? minimumWidth : 48;
    let drag = null;
    let live = true;

    const onPointerDown = event => {
        const handle = event.target instanceof Element
            ? event.target.closest('[data-omni-resize]')
            : null;
        if (!handle || !viewport.contains(handle) || event.button !== 0
            || !handle.hasAttribute('data-omni-drag')) {
            return;
        }

        const key = handle.getAttribute('data-omni-resize');
        const header = handle.closest('th');
        if (!key || !header) {
            return;
        }

        drag = {
            key,
            startX: event.clientX,
            startWidth: header.getBoundingClientRect().width,
            col: viewport.querySelector(`col[data-omni-col="${CSS.escape(key)}"]`),
            pointerId: event.pointerId
        };
        try {
            handle.setPointerCapture?.(event.pointerId);
        } catch {
            // A pointer that is no longer active cannot be captured; the window listeners below
            // still carry the gesture to its end.
        }

        viewport.classList.add('omni-data-grid__viewport--resizing');
        event.preventDefault();
    };

    const onPointerMove = event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        const width = Math.max(floor, drag.startWidth + (event.clientX - drag.startX));
        drag.width = width;
        drag.col?.style.setProperty('--omni-col-width', `${width}px`);
    };

    const onPointerUp = event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        const finished = drag;
        drag = null;
        viewport.classList.remove('omni-data-grid__viewport--resizing');
        if (typeof finished.width === 'number') {
            notifyDotNet(() => live, reference, 'OnColumnResizedAsync', finished.key, finished.width);
        }
    };

    // Excel's double click on a column edge, only where the grid allowed it: size the column to its
    // widest content. The handle carries the two gestures separately, so a column can fit without
    // being draggable and the other way round.
    const onDoubleClick = event => {
        const handle = event.target instanceof Element
            ? event.target.closest('[data-omni-resize]')
            : null;
        if (!handle || !viewport.contains(handle) || !handle.hasAttribute('data-omni-autofit')) {
            return;
        }

        const key = handle.getAttribute('data-omni-resize');
        if (!key) {
            return;
        }

        event.preventDefault();
        autoFitColumn(viewport, key);
    };

    viewport.addEventListener('pointerdown', onPointerDown);
    viewport.addEventListener('dblclick', onDoubleClick);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);

    const attachment = {
        reference,
        floor,
        fitting: new Set(),
        isLive: () => live,
        dispose: () => {
            live = false;
            viewport.removeEventListener('pointerdown', onPointerDown);
            viewport.removeEventListener('dblclick', onDoubleClick);
            window.removeEventListener('pointermove', onPointerMove);
            window.removeEventListener('pointerup', onPointerUp);
            window.removeEventListener('pointercancel', onPointerUp);
            viewport.classList.remove('omni-data-grid__viewport--resizing');
        }
    };
    resizeAttachments.set(viewport, attachment);
}

export function detachResize(viewport) {
    const attachment = resizeAttachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        resizeAttachments.delete(viewport);
    }
}

/**
 * Fits one column to its widest content: the rows on screen, the text .NET holds for every other
 * loaded row (the virtualized rows off screen included), and the header title. The result respects
 * the grid floor and the column's own minimum, lands on the <col> and is reported to .NET once.
 * A second request for the same column while the first is still measuring is ignored.
 */
export async function autoFitColumn(viewport, key) {
    const attachment = resizeAttachments.get(viewport);
    if (!attachment || typeof key !== 'string' || key.length === 0 || attachment.fitting.has(key)) {
        return;
    }

    attachment.fitting.add(key);
    try {
        // Asked first, measured after: the rows on screen may change while .NET answers, and the
        // two measurements must describe the same page.
        let texts = null;
        try {
            texts = await attachment.reference.invokeMethodAsync('GetColumnAutoFitTexts', key);
        } catch (error) {
            if (attachment.isLive()) {
                throw error;
            }
            return;
        }
        if (!attachment.isLive() || !viewport.isConnected) {
            return;
        }

        const selector = `[data-omni-col="${CSS.escape(key)}"]`;
        const col = viewport.querySelector(`col${selector}`);
        const widest = Math.max(
            measureRenderedColumn(viewport, selector, col),
            Array.isArray(texts) ? measureTexts(viewport, selector, texts) : 0);
        const width = Math.max(attachment.floor, columnMinimum(viewport, col), Math.ceil(widest) + 1);
        col?.style.setProperty('--omni-col-width', `${width}px`);
        notifyDotNet(attachment.isLive, attachment.reference, 'OnColumnAutoFitAsync', key, width);
    } finally {
        attachment.fitting.delete(key);
    }
}

// The measurement needs the constraints actually lifted, which inline styles do and a class cannot:
// under table-layout: fixed a cell is exactly as wide as its column, so reading it while the column
// still applies returns the width being replaced rather than the width the content wants.
function measureRenderedColumn(viewport, selector, col) {
    const table = viewport.querySelector('.omni-data-grid__table');
    const cells = [...viewport.querySelectorAll(`td${selector}`)];
    if (!table) {
        return 0;
    }

    // First pass, constraints still in place, on the clipped cells only: their scrollWidth is the
    // width their content wanted, the only reading that sees through an inner flex box whose items
    // were told they may shrink. It already counts the padding. A cell that is not clipped reports
    // the current column width there, which would forbid the fit from ever narrowing the column,
    // so it is left to the second pass.
    let widest = 0;
    for (const cell of cells) {
        if (cell.scrollWidth > cell.clientWidth) {
            const cellStyle = getComputedStyle(cell);
            const borders = parseFloat(cellStyle.borderInlineStartWidth || '0')
                + parseFloat(cellStyle.borderInlineEndWidth || '0');
            widest = Math.max(widest, cell.scrollWidth + borders);
        }
    }

    const tableStyle = table.getAttribute('style');
    const colStyle = col?.getAttribute('style') ?? null;
    const cellStyles = cells.map(cell => cell.getAttribute('style'));

    // The table keeps a minimum width made of the widths already applied, this column's included:
    // left in place, it hands the column its old width back and a fit could never narrow it.
    table.style.tableLayout = 'auto';
    table.style.width = 'max-content';
    table.style.minWidth = '0';
    col?.style.setProperty('--omni-col-width', 'auto');
    col?.style.setProperty('--omni-col-min', '0');
    for (const cell of cells) {
        liftCellConstraints(cell);
    }

    // Second pass, constraints lifted: border-box widths, so the padding is already counted.
    // This catches what the first pass cannot see, a cell that was not clipping because its own
    // box had already been squeezed to fit the column.
    for (const cell of cells) {
        widest = Math.max(widest, cell.getBoundingClientRect().width);
    }

    // The header is measured through the text of its title alone: counting the sort button and the
    // filter icon would make a short column grow on a gesture meant to shrink it. The text, not the
    // title's box, which stretches with the column and would make every new fit a little wider.
    const title = viewport.querySelector(`th${selector} .omni-data-grid__title`);
    if (title) {
        const header = title.closest('th');
        const padding = header
            ? parseFloat(getComputedStyle(header).paddingInlineStart || '0')
                + parseFloat(getComputedStyle(header).paddingInlineEnd || '0')
            : 0;
        const text = document.createRange();
        text.selectNodeContents(title);
        widest = Math.max(widest, text.getBoundingClientRect().width + padding);
    }

    cells.forEach((cell, index) => restoreStyle(cell, cellStyles[index]));
    restoreStyle(col, colStyle);
    restoreStyle(table, tableStyle);
    return widest;
}

// How many candidates of the canvas ranking are measured again in a real cell: the canvas knows the
// font but not letter spacing, font features or a cell's own inner layout, so the final reading is
// always a laid-out cell, on the few texts that can possibly win.
const textProbeCandidates = 8;

/**
 * Widest of the given texts as the column would show them. Every text is ranked with the cell font
 * on a canvas, which costs nothing per row; the leaders are then laid out in a hidden probe cell
 * built from a rendered cell of the column (same classes, same padding), inside the grid so every
 * inherited rule still applies. The probe is added and removed within this call, before Blazor can
 * render again.
 */
function measureTexts(viewport, selector, texts) {
    const table = viewport.querySelector('.omni-data-grid__table');
    if (!table || texts.length === 0) {
        return 0;
    }

    const template = viewport.querySelector(`td${selector}`);
    const probeTable = document.createElement('table');
    probeTable.className = table.className;
    probeTable.setAttribute('aria-hidden', 'true');
    probeTable.style.position = 'absolute';
    probeTable.style.insetBlockStart = '0';
    probeTable.style.insetInlineStart = '0';
    probeTable.style.visibility = 'hidden';
    probeTable.style.pointerEvents = 'none';
    probeTable.style.tableLayout = 'auto';
    probeTable.style.width = 'max-content';
    // Same class, so same minimum width as the grid's table: left in place, it stretches the probe
    // cell to the widths already applied instead of the text.
    probeTable.style.minWidth = '0';
    const row = probeTable.createTBody().insertRow();
    const cell = template ? template.cloneNode(false) : document.createElement('td');
    cell.removeAttribute('id');
    liftCellConstraints(cell);
    row.appendChild(cell);
    viewport.appendChild(probeTable);

    try {
        let candidates = texts;
        if (texts.length > textProbeCandidates) {
            const context = document.createElement('canvas').getContext('2d');
            const font = canvasFont(getComputedStyle(cell));
            if (context && font) {
                context.font = font;
                candidates = texts
                    .map(text => ({ text, width: context.measureText(text).width }))
                    .sort((left, right) => right.width - left.width)
                    .slice(0, textProbeCandidates)
                    .map(entry => entry.text);
            } else {
                candidates = [...texts]
                    .sort((left, right) => right.length - left.length)
                    .slice(0, textProbeCandidates);
            }
        }

        let widest = 0;
        for (const text of candidates) {
            cell.textContent = text;
            widest = Math.max(widest, cell.getBoundingClientRect().width);
        }
        return widest;
    } finally {
        probeTable.remove();
    }
}

/**
 * The column's declared minimum in pixels, whatever its unit: the raw length is resolved by laying
 * out a probe of that width inside the grid, so rem, em or ch follow the grid's own font.
 */
function columnMinimum(viewport, col) {
    const declared = col?.style.getPropertyValue('--omni-col-min').trim();
    if (!declared) {
        return 0;
    }

    const probe = document.createElement('div');
    probe.setAttribute('aria-hidden', 'true');
    probe.style.position = 'absolute';
    probe.style.visibility = 'hidden';
    probe.style.inlineSize = declared;
    viewport.appendChild(probe);
    try {
        return probe.getBoundingClientRect().width;
    } finally {
        probe.remove();
    }
}

// The computed `font` shorthand is empty in Chromium whenever a longhand it cannot express is set
// (font-variant-numeric, font-feature-settings...), and a canvas given an empty font measures in its
// 10px default. The canvas only needs these four, so they are assembled from their longhands.
function canvasFont(style) {
    return [style.fontStyle, style.fontWeight, style.fontSize, style.fontFamily]
        .filter(part => typeof part === 'string' && part.length > 0)
        .join(' ');
}

function liftCellConstraints(cell) {
    cell.style.whiteSpace = 'nowrap';
    cell.style.overflow = 'visible';
    cell.style.textOverflow = 'clip';
    cell.style.maxWidth = 'none';
    cell.style.width = 'max-content';
}

// Puts back the declarations saved before a measurement. Through the CSSOM only: under the strict CSP
// (no 'unsafe-inline' for style attributes) setAttribute('style', ...) is refused and reported, while
// cssText is a script-side style change the policy allows.
function restoreStyle(element, declaration) {
    if (!element) {
        return;
    }
    if (declaration === null) {
        element.removeAttribute('style');
    } else {
        element.style.cssText = declaration;
    }
}
