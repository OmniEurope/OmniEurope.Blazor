// Part of omni-grid.js, which re-exports applyColumns, attachContentFit, detachContentFit, applyFrozen,
// attachFrozenScroll and detachFrozenScroll: column widths, columns sized to their content, headers
// never narrower than their title, frozen columns and their detach control.
import { notifyDotNet } from './notify.js';
import { release } from './release.js';
import { lengthInPixels } from './resize.js';

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

    fitColumns(viewport, true);
    watchHeaders(viewport);
    applyFrozen(viewport);
}

// What a grid has fitted so far, by viewport: the width each content-sized column reached, in pixels.
// Widths only grow while the grid lives, so rows scrolling in and out never make a column jump back
// and forth; a width declared by the host or chosen by the reader replaces it.
const fitStates = new WeakMap();

function fitStateOf(viewport) {
    let state = fitStates.get(viewport);
    if (!state) {
        state = { widths: new Map() };
        fitStates.set(viewport, state);
    }
    return state;
}

/**
 * A column sizes itself to its content when the host gave it no width, the reader did not resize it
 * and it did not ask for Truncate: the markup says so with the two attributes of its <col>.
 */
function fitsContent(col) {
    return col.hasAttribute('data-omni-col')
        && !col.hasAttribute('data-omni-width')
        && !col.hasAttribute('data-omni-truncate');
}

/**
 * Sizes the columns so nothing is cut: a header is never narrower than its title and its icons, and a
 * column that fits its content is never narrower than its longest rendered cell. A cell that ends in
 * an ellipsis reports how much it needs through its own overflow; the column grows by that, and a
 * wider column is left alone. A declared or chosen width still wins over the content (those cells
 * truncate, and show their whole value in the tooltip); a title is only fitted when the headers are
 * not meant to truncate. Returns whether a width changed. Everything is read before anything is
 * written, so a pass over a screenful of rows costs one layout.
 */
function fitColumns(viewport, refresh = false) {
    const header = viewport.querySelector('thead > tr.omni-data-grid__header-row');
    const cols = [...viewport.querySelectorAll('colgroup > col')];
    // A grid that is not laid out (a hidden tab, display: none) measures every cell as empty and zero
    // wide: fitting then would shrink the columns to their padding. The title observer runs the fit
    // again when the grid is shown.
    if (!header || cols.length !== header.children.length || viewport.getClientRects().length === 0) {
        return false;
    }

    const state = fitStateOf(viewport);
    const eligible = cols.map(fitsContent);
    const indexes = new Map();
    cols.forEach((col, index) => {
        const key = col.getAttribute('data-omni-col');
        if (key !== null) {
            indexes.set(key, index);
        }
    });

    // Fitted widths survive a new column layout; one that is no longer content-sized is forgotten, one
    // on a column element created since (a column shown again) is applied to it.
    for (const key of [...state.widths.keys()]) {
        if (!eligible[indexes.get(key) ?? -1]) {
            state.widths.delete(key);
        }
    }
    cols.forEach((col, index) => {
        const width = eligible[index] ? state.widths.get(col.getAttribute('data-omni-col')) : undefined;
        if (width !== undefined && !col.style.getPropertyValue('--omni-col-width')) {
            col.style.setProperty('--omni-col-width', `${width}px`);
        }
    });

    // Titles are meant to end in an ellipsis under HeaderWrap.Truncate: they do not widen the column.
    const fitTitles = !viewport.closest('.omni-data-grid--header-truncate');
    const fitCells = eligible.some(Boolean);
    let changed = false;
    // The title shrinks inside the header's flex row and the row's other items take part of any width
    // added, and a widened column narrows the others: a few passes until nothing is cut.
    for (let pass = 0; pass < 8; pass++) {
        const wanted = new Array(cols.length).fill(0);
        if (fitTitles) {
            [...header.children].forEach((cell, index) => {
                const overflow = Math.max(0, ...[...cell.querySelectorAll('.omni-data-grid__header, .omni-data-grid__title')]
                    .map(element => element.scrollWidth - element.clientWidth));
                if (overflow > 0) {
                    wanted[index] = Math.ceil(cell.getBoundingClientRect().width + overflow);
                }
            });
        }

        if (fitCells) {
            const measure = cellMeasure();
            for (const cell of viewport.querySelectorAll('tbody td.omni-data-grid__cell--text[data-omni-col]')) {
                const index = indexes.get(cell.getAttribute('data-omni-col'));
                if (index !== undefined && eligible[index]) {
                    const width = measure(cell);
                    if (width > 0) {
                        // One pixel more than the content, so a cell never loses its last fraction.
                        wanted[index] = Math.max(wanted[index], width + 1);
                    }
                }
            }
        }

        let grown = false;
        wanted.forEach((width, index) => {
            if (width > 0) {
                cols[index].style.setProperty('--omni-col-width', `${width}px`);
                if (eligible[index]) {
                    state.widths.set(cols[index].getAttribute('data-omni-col'), width);
                }
                grown = true;
            }
        });
        if (!grown) {
            break;
        }

        changed = true;
    }

    // Read once the widths are settled; a new column layout (the declared widths) asks for it again.
    if (changed || refresh) {
        reserveFittedWidths(viewport, header, cols, state);
    }
    return changed;
}

/**
 * The width a cell needs to show its whole content, or 0 when it already does. scrollWidth is a
 * rounded integer, so a cell cut by a fraction of a pixel (an ellipsis all the same) reads as whole:
 * the content is also measured as a range, which keeps its fractions, against the room the cell
 * leaves it. That room is the cell's width less its padding and borders, read once per column.
 */
function cellMeasure() {
    const range = document.createRange();
    const chromes = new Map();
    return cell => {
        const width = cell.getBoundingClientRect().width;
        if (width === 0 || !cell.hasChildNodes()) {
            return 0;
        }

        let chrome = chromes.get(cell.getAttribute('data-omni-col'));
        if (chrome === undefined) {
            const computed = getComputedStyle(cell);
            chrome = ['paddingInlineStart', 'paddingInlineEnd', 'borderInlineStartWidth', 'borderInlineEndWidth']
                .reduce((sum, property) => sum + (parseFloat(computed[property]) || 0), 0);
            chromes.set(cell.getAttribute('data-omni-col'), chrome);
        }

        range.selectNodeContents(cell);
        const content = range.getBoundingClientRect().width + chrome;
        const overflow = Math.max(cell.scrollWidth - cell.clientWidth, content - width);
        return overflow > 0.01 ? Math.ceil(width + overflow) : 0;
    };
}

const absoluteLength =/^[+-]?(\d+\.?\d*|\.\d+)(px|rem|em|ch|pt|vw)$/i;

/**
 * Keeps the table from getting narrower than its fitted columns need. .NET supplies the narrowest
 * width of a table whose columns carry a declared width, and none to a grid that declared nothing;
 * once a column fitted its content that is not enough either: the columns that kept no width (a
 * Truncate column, a templated one, one whose content is short) would be squeezed to nothing as the
 * fitted ones outgrew the viewport, instead of the viewport scrolling sideways. The sum is the
 * fitted widths, the control columns, and for every other column what it declared or the floor of an
 * auto column; the stylesheet takes the larger of it and .NET's. Nothing is set while no column is
 * fitted, so a grid that fits nothing keeps exactly its former minimum.
 */
function reserveFittedWidths(viewport, header, cols, state) {
    if (state.widths.size === 0) {
        viewport.style.removeProperty('--omni-grid-fit-min');
        return;
    }

    const floor = lengthInPixels(viewport, 'var(--omni-data-grid-column-min-width)');
    let total = 0;
    cols.forEach((col, index) => {
        const key = col.getAttribute('data-omni-col');
        if (key === null) {
            total += header.children[index].getBoundingClientRect().width;
            return;
        }

        const declared = (col.getAttribute('data-omni-width') ?? col.style.getPropertyValue('--omni-col-min')).trim();
        total += state.widths.get(key) ?? (absoluteLength.test(declared) ? lengthInPixels(viewport, declared) : floor);
    });
    viewport.style.setProperty('--omni-grid-fit-min', `${Math.ceil(total)}px`);
}

// A width that changed moves the columns after it, and with them the frozen offsets.
function fitAndAnchor(viewport) {
    if (fitColumns(viewport) && viewport.querySelector('thead > tr.omni-data-grid__header-row > .omni-data-grid__column--frozen')) {
        applyFrozen(viewport);
    }
}

const contentFits = new Map();

/**
 * Keeps the columns sized to their content while the grid lives: a render that brings other rows (a
 * page, a sort, a filter, the window of a virtualized grid) or text, or that changes what sizes a
 * column, is followed by one pass in the next frame, however many mutations it made. The first pass
 * runs now. Nothing is attached to the rows themselves, so rows added by later renders need no
 * re-attachment.
 */
export function attachContentFit(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachContentFit(viewport);
    let frame = 0;
    const observer = new MutationObserver(() => {
        if (frame === 0) {
            frame = window.requestAnimationFrame(() => {
                frame = 0;
                if (viewport.isConnected) {
                    fitAndAnchor(viewport);
                }
            });
        }
    });
    observer.observe(viewport, {
        childList: true,
        characterData: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['data-omni-width', 'data-omni-truncate']
    });
    contentFits.set(viewport, {
        dispose: () => {
            observer.disconnect();
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }
        }
    });

    watchHeaders(viewport);
    fitAndAnchor(viewport);
}

export function detachContentFit(viewport) {
    release(contentFits, viewport);
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
                    fitAndAnchor(viewport);
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
    release(frozenScrollAttachments, viewport);
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
