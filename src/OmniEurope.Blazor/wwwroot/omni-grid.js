const attachments = new Map();

function metrics(viewport) {
    return {
        scrollTop: viewport.scrollTop,
        viewportHeight: viewport.clientHeight,
        scrollHeight: viewport.scrollHeight
    };
}

function collectRows(viewport) {
    // A grouped or detailed virtualized grid tags every row of an entry (its group headers, the item
    // row, its detail row) with the entry's slot: the slot is measured as the sum of its rows.
    const slotted = viewport.querySelectorAll('[data-omni-slot]');
    if (slotted.length > 0) {
        const heights = new Map();
        for (const row of slotted) {
            const slot = Number.parseInt(row.getAttribute('data-omni-slot') ?? '', 10);
            if (!Number.isNaN(slot)) {
                heights.set(slot, (heights.get(slot) ?? 0) + row.getBoundingClientRect().height);
            }
        }

        return [...heights].map(([index, height]) => ({ index, height }));
    }

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

    let live = true;
    let frame = 0;
    const notify = () => {
        frame = 0;
        // Removed from the page: .NET is disposing the grid and cannot name this element any more
        // (its detach call resolves to null), so the attachment ends itself instead of calling back.
        if (!viewport.isConnected) {
            detach(viewport);
            return;
        }
        const current = metrics(viewport);
        notifyDotNet(() => live, reference, 'OnViewportChangedAsync', current.scrollTop, current.viewportHeight);
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
            live = false;
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
 * Sets the ceiling consumed by .omni-data-grid__viewport--capped: the viewport grows with its
 * content up to it, then scrolls. A custom property, for the same CSP reason as applyLayout.
 */
export function applyMaxHeight(viewport, maxHeight) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    if (typeof maxHeight === 'string' && maxHeight.trim().length > 0) {
        viewport.style.setProperty('--omni-grid-viewport-max', maxHeight.trim());
    } else {
        viewport.style.removeProperty('--omni-grid-viewport-max');
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
export function applyColumns(viewport, columns, tableMinimum) {
    if (!(viewport instanceof HTMLElement) || !Array.isArray(columns)) {
        return;
    }

    for (const column of columns) {
        const col = viewport.querySelector(`col[data-omni-col="${CSS.escape(column.key)}"]`);
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
    }

    // The narrowest the table may get: below it the viewport scrolls sideways rather than squeezing
    // the columns without a width down to nothing.
    if (typeof tableMinimum === 'string' && tableMinimum.length > 0) {
        viewport.style.setProperty('--omni-grid-table-min', tableMinimum);
    } else {
        viewport.style.removeProperty('--omni-grid-table-min');
    }

    applyFrozen(viewport);
}

/**
 * Sticky offset of every frozen cell: the width of the frozen header cells before it, the grid's
 * own control columns included. Measured from the header row and applied to every row, so it is
 * re-run after each render: a new page, a sort or a filter brings new cells the previous offsets
 * never reached.
 */
export function applyFrozen(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    const header = viewport.querySelector('thead > tr.omni-data-grid__header-row');
    if (!header) {
        return;
    }

    let offset = 0;
    for (const cell of header.children) {
        if (!cell.classList.contains('omni-data-grid__column--frozen')) {
            continue;
        }

        const key = cell.getAttribute('data-omni-col');
        const control = cell.getAttribute('data-omni-control');
        const selector = key !== null
            ? `[data-omni-col="${CSS.escape(key)}"].omni-data-grid__column--frozen`
            : `[data-omni-control="${CSS.escape(control ?? '')}"].omni-data-grid__column--frozen`;
        for (const target of viewport.querySelectorAll(selector)) {
            target.style.setProperty('--omni-col-offset', `${offset}px`);
        }

        offset += cell.getBoundingClientRect().width;
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
    let live = true;

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
            notifyDotNet(() => live, reference, 'OnColumnResizedAsync', finished.key, finished.width);
        }
    };

    // Excel's double click on a column edge: size the column to its widest content. The measurement
    // needs the constraints actually lifted, which inline styles do and a class cannot: under
    // table-layout: fixed a cell is exactly as wide as its column, so reading it while the column
    // still applies returns the width being replaced rather than the width the content wants.
    // Only the rendered rows exist, so the fit follows what is on screen.
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
        const table = viewport.querySelector('.omni-data-grid__table');
        const col = viewport.querySelector(`col${selector}`);
        const cells = [...viewport.querySelectorAll(`td${selector}`)];
        if (!table) {
            return;
        }

        // First pass, constraints still in place: a cell clipped by overflow reports the width its
        // content wanted through scrollWidth, which is the only reading that sees through an inner
        // flex box whose items were told they may shrink. It omits the trailing padding, added back
        // from the computed style.
        let widest = 0;
        if (cells.length > 0) {
            const cellStyle = getComputedStyle(cells[0]);
            const cellPadding = parseFloat(cellStyle.paddingInlineStart || '0')
                + parseFloat(cellStyle.paddingInlineEnd || '0');
            for (const cell of cells) {
                widest = Math.max(widest, cell.scrollWidth + cellPadding);
            }
        }

        const tableStyle = table.getAttribute('style');
        const colStyle = col?.getAttribute('style') ?? null;
        const cellStyles = cells.map(cell => cell.getAttribute('style'));
        const restore = (element, declaration) => {
            if (declaration === null) {
                element?.removeAttribute('style');
            } else {
                element?.setAttribute('style', declaration);
            }
        };

        table.style.tableLayout = 'auto';
        table.style.width = 'max-content';
        col?.style.setProperty('--omni-col-width', 'auto');
        col?.style.setProperty('--omni-col-min', '0');
        for (const cell of cells) {
            cell.style.whiteSpace = 'nowrap';
            cell.style.overflow = 'visible';
            cell.style.textOverflow = 'clip';
            cell.style.maxWidth = 'none';
            cell.style.width = 'max-content';
        }

        // Second pass, constraints lifted: border-box widths, so the padding is already counted.
        // This catches what the first pass cannot see, a cell that was not clipping because its own
        // box had already been squeezed to fit the column.
        for (const cell of cells) {
            widest = Math.max(widest, cell.getBoundingClientRect().width);
        }

        // The header is measured through its title alone: counting the sort button and the filter
        // icon would make a short column grow on a gesture meant to shrink it.
        const title = viewport.querySelector(`th${selector} .omni-data-grid__title`);
        if (title) {
            const header = title.closest('th');
            const padding = header
                ? parseFloat(getComputedStyle(header).paddingInlineStart || '0')
                    + parseFloat(getComputedStyle(header).paddingInlineEnd || '0')
                : 0;
            widest = Math.max(widest, title.getBoundingClientRect().width + padding);
        }

        cells.forEach((cell, index) => restore(cell, cellStyles[index]));
        restore(col, colStyle);
        restore(table, tableStyle);

        const width = Math.max(floor, Math.ceil(widest) + 1);
        col?.style.setProperty('--omni-col-width', `${width}px`);
        notifyDotNet(() => live, reference, 'OnColumnResizedAsync', key, width);
        event.preventDefault();
    };

    viewport.addEventListener('pointerdown', onPointerDown);
    viewport.addEventListener('dblclick', onDoubleClick);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);

    resizeAttachments.set(viewport, {
        dispose: () => {
            live = false;
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

const popoverSelector = 'details[data-omni-popover]';

/**
 * Places a popover panel under its trigger. The panel is position: fixed, so the viewport that
 * scrolls the table cannot clip it; it is kept inside the window and flips above the trigger when
 * there is no room below. Custom properties only, never the style attribute.
 */
function placePopover(popover) {
    const trigger = popover.querySelector(':scope > summary');
    const panel = popover.querySelector(':scope > .omni-data-grid__popover-panel');
    if (!trigger || !panel) {
        return;
    }

    const anchor = trigger.getBoundingClientRect();
    popover.style.setProperty('--omni-popover-min', `${Math.round(anchor.width)}px`);
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    const margin = 8;
    const left = Math.max(margin, Math.min(anchor.left, window.innerWidth - width - margin));
    const below = anchor.bottom + 4;
    const top = below + height > window.innerHeight - margin && anchor.top - height - 4 > margin
        ? anchor.top - height - 4
        : below;
    popover.style.setProperty('--omni-popover-x', `${Math.round(left)}px`);
    popover.style.setProperty('--omni-popover-y', `${Math.round(top)}px`);
}

/** Places the suggestion list of a text filter under its input, for the same reason. */
function placeCombo(combo) {
    const input = combo.querySelector('.omni-combo__input');
    if (!input) {
        return;
    }

    const anchor = input.getBoundingClientRect();
    combo.style.setProperty('--omni-anchor-x', `${Math.round(anchor.left)}px`);
    combo.style.setProperty('--omni-anchor-y', `${Math.round(anchor.bottom + 2)}px`);
    combo.style.setProperty('--omni-anchor-w', `${Math.round(anchor.width)}px`);
}

/**
 * Behaviour of the grid's popovers: header filter menus, advanced filter panels and folded
 * checkable lists, all <details data-omni-popover>. A <details> only closes on its own summary, so a
 * click anywhere else or the Escape key is handled here, one popover open at a time; with
 * hideOnSelect a header menu also closes as soon as a value is picked. Opening and closing is not
 * state .NET needs to hear about, so all of it stays in the browser.
 */
export function attachFilterMenus(viewport, hideOnSelect) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachFilterMenus(viewport);

    // A popover nested in another (a checkable list inside a menu) keeps its parent open.
    const close = keep => {
        for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
            if (!keep || !popover.contains(keep)) {
                popover.open = false;
            }
        }
    };

    const onDocumentPointerDown = event => {
        const target = event.target instanceof Element ? event.target : null;
        close(target?.closest(popoverSelector) ? target : null);
    };

    const onKeyDown = event => {
        if (event.key === 'Escape' && viewport.querySelector(`${popoverSelector}[open]`)) {
            close(null);
        }
    };

    // toggle does not bubble: listened to in the capture phase, for every popover present or future.
    const onToggle = event => {
        const popover = event.target;
        if (popover instanceof HTMLDetailsElement && popover.matches(popoverSelector) && popover.open) {
            close(popover);
            placePopover(popover);
        }
    };

    const onFocusIn = event => {
        const combo = event.target instanceof Element ? event.target.closest('.omni-combo') : null;
        if (combo && viewport.contains(combo)) {
            placeCombo(combo);
        }
    };

    // The trigger moves with any scroll, the page's or the grid's own: the open panels follow it.
    const onMove = () => {
        for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
            placePopover(popover);
        }

        const combo = document.activeElement instanceof Element ? document.activeElement.closest('.omni-combo') : null;
        if (combo && viewport.contains(combo)) {
            placeCombo(combo);
        }
    };

    // Native controls report their pick through change. The combo suggestion list is Blazor markup
    // that disappears on selection, so its own pick is reported from .NET through closeFilterMenus.
    const onPicked = event => {
        const target = event.target instanceof Element ? event.target : null;
        // A checkable list takes several ticks, and an advanced filter waits for its apply button.
        if (!target || target.closest('.omni-multi-select, .omni-data-grid__multi') || target.closest('.omni-data-grid__filter-editor')?.querySelector('.omni-data-grid__filter-apply')) {
            return;
        }

        const menu = target.closest('details.omni-data-grid__filter-menu');
        if (menu) {
            menu.open = false;
        }
    };

    // Applying or clearing from a panel is the end of the task the panel was opened for.
    const onClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const action = target?.closest('.omni-data-grid__filter-apply, .omni-data-grid__filter-clear');
        const popover = action?.closest(popoverSelector);
        if (popover) {
            popover.open = false;
        }
    };

    document.addEventListener('pointerdown', onDocumentPointerDown, true);
    document.addEventListener('keydown', onKeyDown, true);
    viewport.addEventListener('toggle', onToggle, true);
    viewport.addEventListener('focusin', onFocusIn);
    // Typing also opens the list: placed again on input, whatever the focus events did.
    viewport.addEventListener('input', onFocusIn);
    viewport.addEventListener('click', onClick);
    window.addEventListener('scroll', onMove, true);
    window.addEventListener('resize', onMove);
    if (hideOnSelect) {
        viewport.addEventListener('change', onPicked);
    }

    menuAttachments.set(viewport, {
        dispose: () => {
            document.removeEventListener('pointerdown', onDocumentPointerDown, true);
            document.removeEventListener('keydown', onKeyDown, true);
            viewport.removeEventListener('toggle', onToggle, true);
            viewport.removeEventListener('focusin', onFocusIn);
            viewport.removeEventListener('input', onFocusIn);
            viewport.removeEventListener('click', onClick);
            window.removeEventListener('scroll', onMove, true);
            window.removeEventListener('resize', onMove);
            viewport.removeEventListener('change', onPicked);
        }
    });
}

/** Closes every open filter popover of a grid, whatever put them there. */
export function closeFilterMenus(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
        popover.open = false;
    }
}

export function detachFilterMenus(viewport) {
    const attachment = menuAttachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        menuAttachments.delete(viewport);
    }
}

// ---- virtualised data list ------------------------------------------------------------------
// A data list has no viewport of its own: like the page flow it lives in, it scrolls inside the
// nearest scrolling ancestor, or the page. The geometry sent to .NET is therefore the part of that
// scroll area which overlaps the list, expressed as an offset from the list's own top edge.

const listAttachments = new Map();

function scrollContainerOf(element) {
    for (let node = element.parentElement; node && node !== document.body; node = node.parentElement) {
        const overflow = getComputedStyle(node).overflowY;
        if (overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay') {
            return node;
        }
    }

    return null;
}

function listGeometry(root, container) {
    const rect = root.getBoundingClientRect();
    let viewTop = 0;
    let viewHeight = window.innerHeight || document.documentElement.clientHeight;
    if (container) {
        viewTop = container.getBoundingClientRect().top + container.clientTop;
        viewHeight = container.clientHeight;
    }

    return {
        scrollTop: Math.max(0, viewTop - rect.top),
        viewportHeight: viewHeight,
        scrollHeight: root.scrollHeight
    };
}

function collectListItems(root) {
    // The list lays its items out on a grid with a row gap: an item occupies its own height plus
    // that gap, which is what the offset index must count.
    const gap = Number.parseFloat(getComputedStyle(root).rowGap) || 0;
    const rows = [];
    for (const item of root.querySelectorAll(':scope > [data-omni-row-index]')) {
        const index = Number.parseInt(item.getAttribute('data-omni-row-index') ?? '', 10);
        if (!Number.isNaN(index)) {
            rows.push({ index, height: item.getBoundingClientRect().height + gap });
        }
    }

    return rows;
}

/**
 * Calls .NET for an attachment that may be detached before the call lands: the grid disposes its
 * reference right after detaching, so a notification already in flight (an animation frame that fired,
 * a resize released just before navigation) rejects with "no tracked object". That rejection only
 * means the grid is gone and is dropped; while the attachment is live, a failure still surfaces.
 */
function notifyDotNet(isLive, reference, method, ...args) {
    reference.invokeMethodAsync(method, ...args).catch(error => {
        if (isLive()) {
            throw error;
        }
    });
}

/**
 * Starts following the scroll area of a virtualised list. Notifications are coalesced on the next
 * animation frame, one .NET round trip per frame at most.
 */
export function attachList(root, reference) {
    if (!(root instanceof HTMLElement) || !reference) {
        return null;
    }

    detachList(root);

    const container = scrollContainerOf(root);
    const target = container ?? window;
    let live = true;
    let frame = 0;
    const notify = () => {
        frame = 0;
        if (!root.isConnected) {
            detachList(root);
            return;
        }
        const current = listGeometry(root, container);
        notifyDotNet(() => live, reference, 'OnViewportChangedAsync', current.scrollTop, current.viewportHeight);
    };
    const schedule = () => {
        if (frame === 0) {
            frame = window.requestAnimationFrame(notify);
        }
    };

    target.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule, { passive: true });
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(container ?? root);

    listAttachments.set(root, {
        container,
        dispose: () => {
            live = false;
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }

            target.removeEventListener('scroll', schedule);
            window.removeEventListener('resize', schedule);
            resizeObserver?.disconnect();
        }
    });

    return listGeometry(root, container);
}

export function detachList(root) {
    const attachment = listAttachments.get(root);
    if (attachment) {
        attachment.dispose();
        listAttachments.delete(root);
    }
}

/** Reads the list geometry and the height of every rendered item in one round trip. */
export function syncList(root) {
    if (!(root instanceof HTMLElement)) {
        return null;
    }

    const container = listAttachments.get(root)?.container ?? scrollContainerOf(root);
    return { ...listGeometry(root, container), rows: collectListItems(root) };
}

/**
 * Sizes the two spacers of a virtualised list through a custom property, never the style
 * attribute, so the strict CSP still holds.
 */
export function applyListLayout(root, topSpacer, bottomSpacer) {
    if (!(root instanceof HTMLElement)) {
        return;
    }

    root.querySelector(':scope > [data-omni-spacer="top"]')
        ?.style.setProperty('--omni-data-list-spacer', `${Math.max(0, topSpacer)}px`);
    root.querySelector(':scope > [data-omni-spacer="bottom"]')
        ?.style.setProperty('--omni-data-list-spacer', `${Math.max(0, bottomSpacer)}px`);
}

export function scrollToOffset(viewport, offset) {
    if (viewport instanceof HTMLElement) {
        viewport.scrollTop = Math.max(0, offset);
    }
}

const fills = new Map();

function scrollParent(element) {
    // The scrolling areas the package itself lays out come first: an ancestor can be overflow:auto
    // without having a height of its own (it grows with the grid), and sizing the grid from it
    // measured nothing, the grid collapsed to 0.
    const owned = element.closest('.omni-main--scrollable, .omni-dialog__content');
    if (owned) {
        return owned;
    }

    for (let node = element.parentElement; node; node = node.parentElement) {
        const overflow = getComputedStyle(node).overflowY;
        if (overflow === 'auto' || overflow === 'scroll') {
            return node;
        }
    }

    return document.scrollingElement ?? document.documentElement;
}

/**
 * Fill mode sized from what is really left: the distance from the top of the grid to the bottom of
 * the area that scrolls it, less that area's bottom padding. A height of 100% only works when every
 * ancestor has a definite height, which a grid nested in tabs or a detail layout rarely has, so it
 * fell back to its minimum. The value is measured from the top of the scrolled content, so it does
 * not change while the page scrolls, and it is pushed as a custom property for the strict CSP.
 */
export function attachFill(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachFill(viewport);
    const grid = viewport.closest('.omni-data-grid');
    if (!grid) {
        return;
    }

    const container = scrollParent(grid);
    let frame = 0;
    const measure = () => {
        frame = 0;
        // Not laid out yet (a detail layout keeps its body hidden while it loads): its top would read
        // as 0 and the grid would be sized for a place it is not in. The parent observer measures
        // again as soon as it shows.
        if (grid.getClientRects().length === 0) {
            return;
        }

        const page = container === document.scrollingElement || container === document.documentElement;
        const top = page ? 0 : container.getBoundingClientRect().top + container.clientTop;
        const visible = page ? window.innerHeight : container.clientHeight;
        const style = getComputedStyle(container);
        const bottom = Number.parseFloat(style.paddingBottom) || 0;
        const offset = grid.getBoundingClientRect().top - top + (page ? 0 : container.scrollTop);
        // What the boxes between the grid and the scrolling area add under it (their bottom margin,
        // padding and border): left out, the grid reached the bottom and the page still scrolled by it.
        let trailing = 0;
        for (let node = grid; node && node !== container; node = node.parentElement) {
            trailing += Number.parseFloat(getComputedStyle(node).marginBottom) || 0;
            const parent = node.parentElement;
            if (parent && parent !== container) {
                const parentStyle = getComputedStyle(parent);
                trailing += (Number.parseFloat(parentStyle.paddingBottom) || 0) + (Number.parseFloat(parentStyle.borderBottomWidth) || 0);
            }
        }
        const available = Math.floor(visible - offset - bottom - trailing);
        grid.style.setProperty('--omni-grid-fill-height', `${Math.max(0, available)}px`);
    };
    const schedule = () => {
        if (frame === 0) {
            frame = window.requestAnimationFrame(measure);
        }
    };

    const observer = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    observer?.observe(container);
    if (container.firstElementChild instanceof HTMLElement) {
        observer?.observe(container.firstElementChild);
    }
    if (grid.parentElement) {
        observer?.observe(grid.parentElement);
    }
    window.addEventListener('resize', schedule);
    measure();

    fills.set(viewport, {
        dispose: () => {
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }

            observer?.disconnect();
            window.removeEventListener('resize', schedule);
            grid.style.removeProperty('--omni-grid-fill-height');
        }
    });
}

export function detachFill(viewport) {
    const fill = fills.get(viewport);
    if (fill) {
        fill.dispose();
        fills.delete(viewport);
    }
}
