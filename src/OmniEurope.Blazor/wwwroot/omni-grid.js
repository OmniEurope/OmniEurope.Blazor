const attachments = new Map();

function metrics(viewport) {
    return {
        scrollTop: viewport.scrollTop,
        viewportHeight: viewport.clientHeight,
        scrollHeight: viewport.scrollHeight
    };
}

function collectRows(viewport) {
    const rows = [];
    for (const row of viewport.querySelectorAll('[data-omni-row-index]')) {
        const index = Number.parseInt(row.getAttribute('data-omni-row-index') ?? '', 10);
        if (!Number.isNaN(index)) {
            rows.push({ index, height: row.getBoundingClientRect().height });
        }
    }

    return rows;
}

/**
 * Starts observing a grid viewport. Scroll and resize notifications are coalesced on the next
 * animation frame so a fast scroll produces one .NET round trip per frame at most.
 */
export function attach(viewport, reference) {
    if (!(viewport instanceof HTMLElement) || !reference) {
        return null;
    }

    detach(viewport);

    let frame = 0;
    const notify = () => {
        frame = 0;
        const current = metrics(viewport);
        reference.invokeMethodAsync('OnViewportChangedAsync', current.scrollTop, current.viewportHeight);
    };
    const schedule = () => {
        if (frame === 0) {
            frame = window.requestAnimationFrame(notify);
        }
    };

    viewport.addEventListener('scroll', schedule, { passive: true });
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(viewport);

    attachments.set(viewport, {
        dispose: () => {
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }

            viewport.removeEventListener('scroll', schedule);
            resizeObserver?.disconnect();
        }
    });

    return metrics(viewport);
}

export function detach(viewport) {
    const attachment = attachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        attachments.delete(viewport);
    }
}

/**
 * Reads the viewport geometry and the height of every rendered row in one round trip, so .NET can
 * replace its row-height estimates with real measurements.
 */
export function sync(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return null;
    }

    return { ...metrics(viewport), rows: collectRows(viewport) };
}

/**
 * Sizes the spacers that stand in for the rows outside the rendered window. Custom properties are
 * used instead of the style attribute so the strict CSP of the library still holds.
 */
export function applyLayout(viewport, topSpacer, bottomSpacer, height) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    const top = viewport.querySelector('[data-omni-spacer="top"]');
    const bottom = viewport.querySelector('[data-omni-spacer="bottom"]');
    top?.style.setProperty('--omni-grid-spacer', `${Math.max(0, topSpacer)}px`);
    bottom?.style.setProperty('--omni-grid-spacer', `${Math.max(0, bottomSpacer)}px`);
    if (typeof height === 'string' && height.trim().length > 0) {
        viewport.style.setProperty('--omni-grid-viewport', height.trim());
    } else {
        viewport.style.removeProperty('--omni-grid-viewport');
    }
}

/**
 * Sets the fixed row height custom property consumed by the .omni-data-grid--fixed-row-height
 * CSS, again through a custom property rather than the style attribute for the same CSP reason
 * as applyLayout above.
 */
export function applyRowHeight(viewport, height) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    if (typeof height === 'number' && height > 0) {
        viewport.style.setProperty('--omni-row-height', `${height}px`);
    } else {
        viewport.style.removeProperty('--omni-row-height');
    }
}

/**
 * Applies the CSS length of every column and the sticky offset of the frozen ones. Widths land on
 * the matching <col> element and offsets on each cell, again through custom properties only.
 */
export function applyColumns(viewport, columns) {
    if (!(viewport instanceof HTMLElement) || !Array.isArray(columns)) {
        return;
    }

    let frozenOffset = 0;
    for (const column of columns) {
        const selector = `[data-omni-col="${CSS.escape(column.key)}"]`;
        const col = viewport.querySelector(`col${selector}`);
        if (col) {
            if (column.width) {
                col.style.setProperty('--omni-col-width', column.width);
            } else {
                col.style.removeProperty('--omni-col-width');
            }

            if (column.minWidth) {
                col.style.setProperty('--omni-col-min', column.minWidth);
            } else {
                col.style.removeProperty('--omni-col-min');
            }
        }

        const cells = viewport.querySelectorAll(`th${selector}, td${selector}`);
        for (const cell of cells) {
            if (column.frozen) {
                cell.style.setProperty('--omni-col-offset', `${frozenOffset}px`);
            } else {
                cell.style.removeProperty('--omni-col-offset');
            }
        }

        if (column.frozen) {
            const header = viewport.querySelector(`th${selector}`);
            frozenOffset += header ? header.getBoundingClientRect().width : 0;
        }
    }
}

export function scrollToOffset(viewport, offset) {
    if (viewport instanceof HTMLElement) {
        viewport.scrollTop = Math.max(0, offset);
    }
}
