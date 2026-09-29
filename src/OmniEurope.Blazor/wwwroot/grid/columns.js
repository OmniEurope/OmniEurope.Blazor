// Part of omni-grid.js, which re-exports applyColumns, applyFrozen, attachFrozenScroll and
// detachFrozenScroll: column widths, headers never narrower than their title, frozen columns and
// their detach control.
import { notifyDotNet } from './notify.js';

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

    fitHeaders(viewport);
    watchHeaders(viewport);
    applyFrozen(viewport);
}

/**
 * A column is never narrower than its header: a title that is one word ("Exécuteur") cannot wrap, so a
 * declared width smaller than the title and its sort and filter icons cut it. The title's own
 * overflow says by how much; the column grows by that, and a wider width is left alone.
 */
function fitHeaders(viewport) {
    // Truncate is a choice: those titles are meant to end in an ellipsis, not to widen the column.
    if (viewport.closest('.omni-data-grid--header-truncate')) {
        return;
    }
    const header = viewport.querySelector('thead > tr.omni-data-grid__header-row');
    const cols = viewport.querySelectorAll('colgroup > col');
    if (!header || cols.length !== header.children.length) {
        return;
    }

    // The title shrinks inside the header's flex row, so its own overflow is the one that shows; the
    // row's other items take part of any width added, hence a few passes until nothing is cut.
    for (let pass = 0; pass < 4; pass++) {
        let grown = false;
        [...header.children].forEach((cell, index) => {
            const overflow = Math.max(0, ...[...cell.querySelectorAll('.omni-data-grid__header, .omni-data-grid__title')]
                .map(element => element.scrollWidth - element.clientWidth));
            if (overflow > 0) {
                const width = Math.ceil(cell.getBoundingClientRect().width + overflow);
                cols[index].style.setProperty('--omni-col-width', `${width}px`);
                grown = true;
            }
        });
        if (!grown) {
            break;
        }
    }
}

const headerWatchers = new WeakMap();

/**
 * A title fitted once can be cut later: a larger text size or another density widens it without a new
 * column layout. Watching the size of the titles refits the columns whenever that happens.
 */
function watchHeaders(viewport) {
    if (typeof ResizeObserver !== 'function') {
        return;
    }
    let observer = headerWatchers.get(viewport);
    if (!observer) {
        let frame = 0;
        observer = new ResizeObserver(() => {
            if (!viewport.isConnected) {
                observer.disconnect();
                headerWatchers.delete(viewport);
                return;
            }
            if (frame === 0) {
                frame = window.requestAnimationFrame(() => {
                    frame = 0;
                    fitHeaders(viewport);
                });
            }
        });
        headerWatchers.set(viewport, observer);
    }
    // A new column layout can bring new title elements: watch the ones on the page now.
    observer.disconnect();
    viewport.querySelectorAll('thead .omni-data-grid__title').forEach(title => observer.observe(title));
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

    followHorizontalScroll(viewport);
    for (const previous of viewport.querySelectorAll('.omni-data-grid__column--frozen-last')) {
        previous.classList.remove('omni-data-grid__column--frozen-last');
    }

    let offset = 0;
    let lastSelector = null;
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

        lastSelector = selector;
        offset += cell.getBoundingClientRect().width;
    }

    // The last frozen column draws the edge shadow while rows scroll under it.
    if (lastSelector !== null) {
        for (const target of viewport.querySelectorAll(lastSelector)) {
            target.classList.add('omni-data-grid__column--frozen-last');
        }
    }

    placeFrozenToggle(viewport);
}

/**
 * Whether the viewport has left its horizontal start. Right-to-left content scrolls to negative
 * values, and a sub-pixel remainder after a zoom does not count as scrolled.
 */
function isScrolledSideways(scrollLeft) {
    return Math.abs(Number(scrollLeft) || 0) >= 1;
}

/**
 * Where the detach control of the frozen columns sits, relative to the grid root: on the trailing
 * edge of the frozen block, halfway down the header row. Pure arithmetic on measured boxes, so the
 * right-to-left mirror is one branch.
 */
function frozenTogglePosition(rootRect, viewportRect, borderStart, borderTop, frozenWidth, headerHeight, rtl) {
    const start = (rtl ? rootRect.right - viewportRect.right : viewportRect.left - rootRect.left) + borderStart + frozenWidth;
    const top = viewportRect.top - rootRect.top + borderTop + headerHeight / 2;
    return { start: Math.round(start), top: Math.round(top) };
}

/**
 * Places the detach control of the frozen columns, again through custom properties only. The
 * control lives outside the scrolled viewport, so it stays at the same spot whether the columns are
 * frozen or detached, and a second click lands on it.
 */
function placeFrozenToggle(viewport) {
    const root = viewport.closest('.omni-data-grid');
    const toggle = root?.querySelector(':scope > .omni-data-grid__frozen-toggle');
    const header = viewport.querySelector('thead > tr.omni-data-grid__header-row');
    if (!toggle || !header) {
        return;
    }

    let frozenWidth = 0;
    for (const cell of header.children) {
        if (cell.classList.contains('omni-data-grid__column--frozen')) {
            frozenWidth += cell.getBoundingClientRect().width;
        }
    }

    const viewportStyle = getComputedStyle(viewport);
    const rtl = viewportStyle.direction === 'rtl';
    const borderStart = Number.parseFloat(rtl ? viewportStyle.borderRightWidth : viewportStyle.borderLeftWidth) || 0;
    const position = frozenTogglePosition(
        root.getBoundingClientRect(),
        viewport.getBoundingClientRect(),
        borderStart,
        viewport.clientTop,
        frozenWidth,
        header.getBoundingClientRect().height,
        rtl);
    toggle.style.setProperty('--omni-frozen-toggle-start', `${position.start}px`);
    toggle.style.setProperty('--omni-frozen-toggle-top', `${position.top}px`);
}

const frozenScrollAttachments = new Map();

/**
 * Follows the horizontal scroll of a grid with frozen columns for the detach cycle documented in
 * docs/data-components.md. .NET owns the state; the script only reports the viewport leaving or
 * reaching its start, once per crossing, and keeps the control on the frozen edge meanwhile.
 * Returns whether the viewport is already scrolled sideways.
 */
export function attachFrozenScroll(viewport, reference) {
    if (!(viewport instanceof HTMLElement) || !reference) {
        return false;
    }

    detachFrozenScroll(viewport);

    let live = true;
    let frame = 0;
    let scrolled = isScrolledSideways(viewport.scrollLeft);
    const onScroll = () => {
        if (!viewport.isConnected) {
            detachFrozenScroll(viewport);
            return;
        }

        const now = isScrolledSideways(viewport.scrollLeft);
        if (now !== scrolled) {
            scrolled = now;
            notifyDotNet(() => live, reference, 'OnHorizontalScrollChangedAsync', now);
        }

        if (now && frame === 0) {
            frame = window.requestAnimationFrame(() => {
                frame = 0;
                placeFrozenToggle(viewport);
            });
        }
    };

    viewport.addEventListener('scroll', onScroll, { passive: true });
    placeFrozenToggle(viewport);
    frozenScrollAttachments.set(viewport, {
        dispose: () => {
            live = false;
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }

            viewport.removeEventListener('scroll', onScroll);
        }
    });

    return scrolled;
}

export function detachFrozenScroll(viewport) {
    const attachment = frozenScrollAttachments.get(viewport);
    if (attachment) {
        attachment.dispose();
        frozenScrollAttachments.delete(viewport);
    }
}

const scrollFollowers = new WeakSet();

/**
 * Marks the viewport while its content is scrolled sideways, so frozen columns can show that the rest
 * of the row passes under them. One passive listener per viewport; it dies with the element.
 */
function followHorizontalScroll(viewport) {
    const update = () => viewport.classList.toggle('omni-data-grid__viewport--scrolled-x', viewport.scrollLeft > 0);
    update();
    if (scrollFollowers.has(viewport)) {
        return;
    }

    scrollFollowers.add(viewport);
    viewport.addEventListener('scroll', update, { passive: true });
}
