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
export function sync(viewport, measureRows = true) {
    if (!(viewport instanceof HTMLElement)) {
        return null;
    }

    // Measuring every rendered row forces a layout on each scroll frame. A grid with a known row
    // height throws those measurements away anyway, so it does not pay for them.
    return { ...metrics(viewport), rows: measureRows ? collectRows(viewport) : null };
}

/**
 * Sizes the spacers that stand in for the rows outside the rendered window. Custom properties are
 * used instead of the style attribute so the strict CSP of the library still holds.
 */
export function applyLayout(viewport, topSpacer, bottomSpacer, height, minHeight) {
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

    if (typeof minHeight === 'string' && minHeight.trim().length > 0) {
        viewport.style.setProperty('--omni-grid-viewport-min', minHeight.trim());
    } else {
        viewport.style.removeProperty('--omni-grid-viewport-min');
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

    const onPointerDown = event => {
        const handle = event.target instanceof Element
            ? event.target.closest('[data-omni-resize]')
            : null;
        if (!handle || !viewport.contains(handle) || event.button !== 0) {
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
            reference.invokeMethodAsync('OnColumnResizedAsync', finished.key, finished.width);
        }
    };

    // Excel's double click on a column edge: size the column to its widest content. The cells are
    // measured with their width constraint lifted, so the value is the natural width, not the
    // current clipped one; only the rendered rows exist, so this fits what is on screen.
    const onDoubleClick = event => {
        const handle = event.target instanceof Element
            ? event.target.closest('[data-omni-resize]')
            : null;
        if (!handle || !viewport.contains(handle)) {
            return;
        }

        const key = handle.getAttribute('data-omni-resize');
        if (!key) {
            return;
        }

        const selector = `[data-omni-col="${CSS.escape(key)}"]`;
        const col = viewport.querySelector(`col${selector}`);
        const previous = col ? col.style.getPropertyValue('--omni-col-width') : '';
        col?.style.setProperty('--omni-col-width', 'max-content');

        // Data cells decide the width. The header is measured through its title alone: counting the
        // sort button and the filter icon would make a short column grow on a gesture meant to
        // shrink it. Only the rows currently rendered can be measured, which under virtualization
        // means the fit follows what is on screen.
        let widest = 0;
        for (const cell of viewport.querySelectorAll(`td${selector}`)) {
            widest = Math.max(widest, cell.scrollWidth);
        }

        const title = viewport.querySelector(`th${selector} .omni-data-grid__title`);
        if (title) {
            const header = title.closest('th');
            const padding = header
                ? parseFloat(getComputedStyle(header).paddingInlineStart || '0')
                    + parseFloat(getComputedStyle(header).paddingInlineEnd || '0')
                : 0;
            widest = Math.max(widest, Math.ceil(title.scrollWidth + padding));
        }

        if (previous) {
            col?.style.setProperty('--omni-col-width', previous);
        } else {
            col?.style.removeProperty('--omni-col-width');
        }

        const width = Math.max(floor, Math.ceil(widest) + 1);
        col?.style.setProperty('--omni-col-width', `${width}px`);
        reference.invokeMethodAsync('OnColumnResizedAsync', key, width);
        event.preventDefault();
    };

    viewport.addEventListener('pointerdown', onPointerDown);
    viewport.addEventListener('dblclick', onDoubleClick);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);

    resizeAttachments.set(viewport, {
        dispose: () => {
            viewport.removeEventListener('pointerdown', onPointerDown);
            viewport.removeEventListener('dblclick', onDoubleClick);
            window.removeEventListener('pointermove', onPointerMove);
            window.removeEventListener('pointerup', onPointerUp);
            window.removeEventListener('pointercancel', onPointerUp);
            viewport.classList.remove('omni-data-grid__viewport--resizing');
        }
    });
}

export function detachResize(viewport) {
    const attachment = resizeAttachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        resizeAttachments.delete(viewport);
    }
}

const menuAttachments = new Map();

/**
 * Dismissal behaviour of the per-column filter popovers. A <details> element only closes on its own
 * summary, so a click anywhere else, or the Escape key, is handled here; with hideOnSelect the menu
 * also closes as soon as a value is picked. All of it stays in the browser: opening and closing a
 * popover is not state .NET needs to hear about.
 */
export function attachFilterMenus(viewport, hideOnSelect) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachFilterMenus(viewport);

    const close = except => {
        for (const menu of viewport.querySelectorAll('details.omni-data-grid__filter-menu[open]')) {
            if (menu !== except) {
                menu.open = false;
            }
        }
    };

    const onDocumentPointerDown = event => {
        const target = event.target instanceof Element ? event.target : null;
        close(target?.closest('details.omni-data-grid__filter-menu') ?? null);
    };

    const onKeyDown = event => {
        if (event.key === 'Escape') {
            close(null);
        }
    };

    // Native controls report their pick through change. The combo suggestion list is Blazor markup
    // that disappears on selection, so its own pick is reported from .NET through closeFilterMenus.
    const onPicked = event => {
        const target = event.target instanceof Element ? event.target : null;
        // A checkable list is meant to take several ticks, so it never closes the menu by itself.
        if (target?.closest('.omni-multi-select')) {
            return;
        }

        const menu = target?.closest('details.omni-data-grid__filter-menu');
        if (menu) {
            menu.open = false;
        }
    };

    document.addEventListener('pointerdown', onDocumentPointerDown, true);
    document.addEventListener('keydown', onKeyDown, true);
    if (hideOnSelect) {
        viewport.addEventListener('change', onPicked);
    }

    menuAttachments.set(viewport, {
        dispose: () => {
            document.removeEventListener('pointerdown', onDocumentPointerDown, true);
            document.removeEventListener('keydown', onKeyDown, true);
            viewport.removeEventListener('change', onPicked);
        }
    });
}

/** Closes every open filter popover of a grid, whatever put them there. */
export function closeFilterMenus(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    for (const menu of viewport.querySelectorAll('details.omni-data-grid__filter-menu[open]')) {
        menu.open = false;
    }
}

export function detachFilterMenus(viewport) {
    const attachment = menuAttachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        menuAttachments.delete(viewport);
    }
}

export function scrollToOffset(viewport, offset) {
    if (viewport instanceof HTMLElement) {
        viewport.scrollTop = Math.max(0, offset);
    }
}
