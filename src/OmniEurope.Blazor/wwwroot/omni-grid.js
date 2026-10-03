// The data grid and the data list, the module the components import (OmniModules.Grid; the log
// viewer imports sync, applyLayout and scrollToOffset): viewport observation and virtualisation,
// spacers and viewport sizes, fill mode, the wheel scope and the wait for the first images. The
// columns, the resize and auto-fit gestures, the filter popovers and the virtualised data list live
// in ./grid/ and are re-exported here, so every export keeps this address.
import { notifyDotNet } from './grid/notify.js';

export { applyColumns, applyFrozen, attachFrozenScroll, detachFrozenScroll } from './grid/columns.js';
export { attachResize, detachResize, autoFitColumn } from './grid/resize.js';
export { attachFilterMenus, closeFilterMenus, detachFilterMenus } from './grid/filter-menus.js';
export { attachList, detachList, syncList, applyListLayout } from './grid/list.js';
// A text cell cut by its ellipsis shows its whole value in the package tooltip.
export { installPackageTooltips, uninstallPackageTooltips } from './omni-tooltip.js';

const attachments = new Map();

// Reported instead of the scroll position by a viewport held at its end; .NET clamps it to the end of
// its own row model.
const endOfContent = Number.MAX_VALUE;

function metrics(viewport) {
    // A render that drops the first rows of the window shortens the content until applyLayout grows the
    // top spacer: a viewport at its end is pulled back meanwhile (Firefox does so at once). Read then,
    // that position would bring the rows back, whose render pulls the end away again, endlessly. A
    // viewport at its end therefore reports the end itself, which no transient height can move.
    const atEnd = attachments.get(viewport)?.state.atEnd === true;
    return {
        scrollTop: atEnd ? endOfContent : viewport.scrollTop,
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
    // Whether the last scroll reached the end. Rows are placed on estimated heights until they are
    // measured: a jump to the end (the scrollbar dragged down, End) renders the last rows, which then
    // prove taller than estimated, the content grows and the end moves away while the scroll stays
    // put, leaving the last row out of sight. applyLayout keeps such a viewport at its end.
    const state = { atEnd: false, reported: new Map() };
    const track = () => {
        state.atEnd = viewport.scrollTop > 0
            && viewport.scrollTop + viewport.clientHeight >= viewport.scrollHeight - 2;
    };
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

    // A viewport at its end follows the end whatever moves it: rows measured taller, a row that grows
    // after it was drawn (an image, a font, a column made narrower), or the viewport itself made
    // shorter. Done here, at once, rather than after a .NET round trip that may not come: when the
    // rows in view do not change, .NET has nothing to render and would never move the scroll.
    const stick = () => {
        if (state.atEnd) {
            const end = viewport.scrollHeight - viewport.clientHeight;
            if (end - viewport.scrollTop > 1) {
                viewport.scrollTop = end;
            }
        }
    };

    // The rows in view changed size without any scroll: .NET measures them again on its next render,
    // so it is asked for one. Their new heights move the spacers, and the end with them.
    let contentFrame = 0;
    const remeasure = () => {
        contentFrame = 0;
        if (viewport.isConnected) {
            notifyDotNet(() => live, reference, 'OnContentResizedAsync');
        }
    };

    viewport.addEventListener('scroll', track, { passive: true });
    viewport.addEventListener('scroll', schedule, { passive: true });
    const table = viewport.querySelector('table');
    const resizeObserver = typeof ResizeObserver === 'function'
        ? new ResizeObserver(entries => {
            stick();
            schedule();
            if (entries.some(entry => entry.target === table) && contentFrame === 0 && rowsChanged(state.reported, viewport)) {
                contentFrame = window.requestAnimationFrame(remeasure);
            }
        })
        : null;
    resizeObserver?.observe(viewport);
    if (table) {
        resizeObserver?.observe(table);
    }

    attachments.set(viewport, {
        state,
        dispose: () => {
            live = false;
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }
            if (contentFrame !== 0) {
                window.cancelAnimationFrame(contentFrame);
            }

            viewport.removeEventListener('scroll', track);
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
 * replace its row-height estimates with real measurements. When given, the spacers of the rows just
 * rendered are set first.
 */
export function sync(viewport, measureRows = true, topSpacer = null, bottomSpacer = null) {
    if (!(viewport instanceof HTMLElement)) {
        return null;
    }

    // A render that drops the first rows of the window shortens the content until the top spacer
    // takes their place. Read in between, the layout pulls the scroll back by the rows dropped
    // (Firefox does so at once), and the window computed from that position brings them back: a
    // short wheel step down never gets past them. The spacers of this render land before any read.
    if (Number.isFinite(topSpacer) && Number.isFinite(bottomSpacer)) {
        viewport.querySelector('[data-omni-spacer="top"]')?.style.setProperty('--omni-grid-spacer', `${Math.max(0, topSpacer)}px`);
        viewport.querySelector('[data-omni-spacer="bottom"]')?.style.setProperty('--omni-grid-spacer', `${Math.max(0, bottomSpacer)}px`);
    }

    // Measuring every rendered row forces a layout on each scroll frame. A grid with a known row
    // height throws those measurements away anyway, so it does not pay for them.
    const rawEstimate = getComputedStyle(viewport).getPropertyValue('--omni-grid-row-estimate').trim();
    const match = /^(\d+(?:\.\d+)?)(px|rem)$/.exec(rawEstimate);
    const rootSize = match?.[2] === 'rem' ? Number.parseFloat(getComputedStyle(document.documentElement).fontSize) : 1;
    const estimate = match ? Number(match[1]) * rootSize : null;
    const rows = measureRows ? collectRows(viewport) : null;
    // What .NET now knows, so a later change of size is only reported when a row really moved.
    const reported = attachments.get(viewport)?.state.reported;
    if (reported && rows) {
        reported.clear();
        for (const row of rows) {
            reported.set(row.index, row.height);
        }
    }

    return {
        ...metrics(viewport),
        rowEstimate: Number.isFinite(estimate) && estimate > 0 ? estimate : null,
        rows
    };
}

// Whether a row already measured by .NET is no longer that height. A row not measured yet is left to
// the sync that follows its render.
function rowsChanged(reported, viewport) {
    if (!reported || reported.size === 0) {
        return false;
    }

    return collectRows(viewport).some(row => {
        const known = reported.get(row.index);
        return known !== undefined && Math.abs(known - row.height) > 0.5;
    });
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

    // A viewport scrolled to its end stays there while the rows it reached are measured: the scroll
    // this sets notifies .NET again, which renders what the new end shows, until nothing grows.
    if (attachments.get(viewport)?.state.atEnd) {
        const end = viewport.scrollHeight - viewport.clientHeight;
        if (end - viewport.scrollTop > 1) {
            viewport.scrollTop = end;
        }
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
        const computedStyle = getComputedStyle(container);
        const bottom = Number.parseFloat(computedStyle.paddingBottom) || 0;
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

const wheelScopes = new Map();

function canScrollVertically(element, deltaY) {
    if (!/(auto|scroll)/.test(getComputedStyle(element).overflowY) || element.scrollHeight <= element.clientHeight) {
        return false;
    }

    return deltaY < 0
        ? element.scrollTop > 0
        : element.scrollTop + element.clientHeight < element.scrollHeight - 1;
}

/**
 * WheelScrollScope: a vertical wheel turn over the named ancestor scrolls the grid's rows. The grid
 * keeps its own wheel, and so does any other area under the pointer that can still scroll that way;
 * Shift (horizontal intent) and Ctrl (zoom) are never taken. A grid that is not laid out, such as one
 * in a hidden tab sharing the same ancestor, lets the event through for the visible one.
 */
export function attachWheelScope(viewport, selector) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachWheelScope(viewport);
    const scope = typeof selector === 'string' && selector ? viewport.closest(selector) : null;
    if (!scope) {
        return;
    }

    const onWheel = event => {
        if (event.defaultPrevented || event.ctrlKey || event.shiftKey || Math.abs(event.deltaY) <= Math.abs(event.deltaX)) {
            return;
        }

        if (!viewport.isConnected || viewport.getClientRects().length === 0) {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        if (target && viewport.contains(target)) {
            return;
        }

        for (let node = target; node && node !== scope; node = node.parentElement) {
            if (canScrollVertically(node, event.deltaY)) {
                return;
            }
        }

        const unit = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? viewport.clientHeight : 1;
        event.preventDefault();
        viewport.scrollTop += event.deltaY * unit;
    };

    scope.addEventListener('wheel', onWheel, { passive: false });
    wheelScopes.set(viewport, () => scope.removeEventListener('wheel', onWheel));
}

export function detachWheelScope(viewport) {
    const dispose = wheelScopes.get(viewport);
    if (dispose) {
        dispose();
        wheelScopes.delete(viewport);
    }
}

const readyImageTimeout = 2000;

/** Wait for the visible initial row images, at most readyImageTimeout ms, and a completed browser layout. */
export async function waitForReady(viewport) {
    if (!viewport?.isConnected || !viewport.getClientRects().length) return false;
    const bounds = viewport.getBoundingClientRect();
    const visible = [...viewport.querySelectorAll('img')].filter(image => {
        const rect = image.getBoundingClientRect();
        return rect.bottom > bounds.top && rect.top < bounds.bottom;
    });
    // An image whose request never ends must not keep the grid hidden: past the timeout it shows as it is.
    let timer;
    const images = Promise.all(visible.map(image => image.complete ? Promise.resolve() : new Promise(resolve => {
        image.addEventListener('load', resolve, { once: true });
        image.addEventListener('error', resolve, { once: true });
    })));
    await Promise.race([images, new Promise(resolve => { timer = setTimeout(resolve, readyImageTimeout); })]);
    clearTimeout(timer);
    await new Promise(requestAnimationFrame);
    return viewport.isConnected;
}
