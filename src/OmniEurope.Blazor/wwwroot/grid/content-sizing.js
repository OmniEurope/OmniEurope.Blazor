// Part of omni-grid.js, which re-exports setContentTexts: the sizing of a grid whose ColumnSizing is Content.
// Each column takes the width of its widest content, its header on one line with its icons included; the
// room left goes to the columns that grow (or is shared out); a narrower grid keeps those widths and scrolls
// sideways. The measuring is the one of the fit to content of a column (resize.js), run on every column at
// once: the cells on screen with the constraints lifted, and the texts .NET holds for the rows it loaded.
import { lengthInPixels, measureColumnTexts, measureRenderedColumns } from './resize.js';

// What a grid has measured so far, by viewport.
const states = new WeakMap();

function stateOf(viewport) {
    let state = states.get(viewport);
    if (!state) {
        state = {
            // The texts of the loaded rows by column key, as .NET handed them over.
            texts: new Map(),
            // The widest of those texts by key, laid out once: they only change with the texts or the fonts.
            textWidths: new Map(),
            // The texts added to a column since its widest was laid out, by key: only they are laid out next.
            fresh: new Map(),
            // The content width of each column, in pixels. It only grows while the rows stay the same, so
            // rows scrolling in and out never make a column jump back and forth.
            widths: new Map(),
            // CSS lengths laid out in pixels, by declaration (a minimum in rem, the column floor).
            lengths: new Map(),
            // Fonts and padding the measures were taken with.
            context: '',
            watcher: null,
            // Watches the grid while its first rows load, to size and show the table once they are in.
            loading: null,
            onChanged: null
        };
        states.set(viewport, state);
    }
    return state;
}

/** Whether the grid of this viewport sizes its columns to their content (the table says so). */
export function isContentSizing(viewport) {
    return viewport.querySelector(':scope > table.omni-data-grid__table')?.getAttribute('data-omni-sizing') === 'content';
}

/**
 * The widths of the columns, planned from the room available. Each column has a base width (its content,
 * or its floor) and a role: 'fixed' for a width the reader chose, 'flexible' for a column that takes the
 * room left (it grows, wraps or is cut), 'plain' for the others. Whatever the available width is, no
 * column goes under its base; the room left over goes to the flexible columns, to all the plain ones when
 * there are none, in equal integer shares (the last gets the remainder, so the sum is the available width).
 * Also returns the narrowest the table may become, the sum of the bases and of the control columns: under
 * it the viewport scrolls sideways.
 */
export function planColumnWidths(columns, controls, available) {
    const minimum = Math.ceil(controls) + columns.reduce((sum, column) => sum + column.base, 0);
    const widths = new Map(columns.map(column => [column.key, column.base]));
    const flexible = columns.filter(column => column.role === 'flexible');
    const recipients = flexible.length > 0 ? flexible : columns.filter(column => column.role === 'plain');
    const free = available - minimum;
    if (free > 0 && recipients.length > 0) {
        const share = Math.floor(free / recipients.length);
        for (const column of recipients) {
            widths.set(column.key, column.base + share);
        }

        const last = recipients[recipients.length - 1];
        widths.set(last.key, widths.get(last.key) + free - share * recipients.length);
    }

    return { widths, minimum };
}

/**
 * Takes the texts of the loaded rows .NET holds, by column key, for the columns it could read (not a
 * templated one, which is measured on its cells). Sent when the rows or the measured columns change; the
 * columns are measured again against them. With append, the texts are only the ones .NET did not send yet
 * (the blocks a virtualized grid fetched): they join the ones held, and the widths measured so far stand,
 * only the new texts are laid out.
 */
export function setContentTexts(viewport, texts, append) {
    if (!(viewport instanceof HTMLElement) || texts === null || typeof texts !== 'object') {
        return;
    }

    const state = stateOf(viewport);
    takeContentTexts(state, new Map(Object.entries(texts)), append);
    if (isContentSizing(viewport)) {
        sizeContent(viewport, state.onChanged);
    }
}

/**
 * Folds the texts handed over into what a state holds. Replacing forgets the texts, their widest and the
 * content widths (the rows are others); appending keeps them all and records the new texts to lay out.
 */
export function takeContentTexts(state, texts, append) {
    if (!append) {
        state.texts = texts;
        state.textWidths.clear();
        state.fresh.clear();
        state.widths.clear();
        return;
    }

    for (const [key, added] of texts) {
        state.texts.set(key, [...(state.texts.get(key) ?? []), ...added]);
        state.fresh.set(key, [...(state.fresh.get(key) ?? []), ...added]);
    }
}

/**
 * The widest of the texts handed over for a column, laid out by measureTexts: all of them the first time, and
 * again after the fonts changed, only the ones added since otherwise. 0 when there are none.
 */
export function widestTextOf(state, key, measureTexts) {
    const texts = state.texts.get(key);
    if (!Array.isArray(texts) || texts.length === 0) {
        return 0;
    }

    const added = state.fresh.get(key);
    state.fresh.delete(key);
    if (!state.textWidths.has(key)) {
        state.textWidths.set(key, measureTexts(texts));
    } else if (added) {
        state.textWidths.set(key, Math.max(state.textWidths.get(key), measureTexts(added)));
    }

    return state.textWidths.get(key);
}

/** Forgets what a grid measured: it no longer sizes to its content, or it went. */
export function forgetContentSizing(viewport) {
    const state = states.get(viewport);
    if (!state) {
        return;
    }

    state.watcher?.disconnect();
    state.loading?.disconnect();
    states.delete(viewport);
    viewport.style.removeProperty('--omni-grid-fit-min');
    viewport.querySelector(':scope > table.omni-data-grid__table')?.removeAttribute('data-omni-sized');
}

// Whether the table already shows its content widths (the stylesheet hides it until then).
function isShown(viewport) {
    return viewport.querySelector(':scope > table.omni-data-grid__table')?.hasAttribute('data-omni-sized') === true;
}

// Whether the grid is loading: its own request, the host's (Busy) or its first virtual block.
function isLoading(viewport) {
    return viewport.closest('.omni-data-grid')?.getAttribute('aria-busy') === 'true';
}

// Sizes the grid again once it stops loading: the rows are in the DOM by then, measured before the paint.
function awaitRows(viewport, state) {
    const grid = viewport.closest('.omni-data-grid');
    if (state.loading || !grid || typeof MutationObserver !== 'function') {
        return;
    }

    state.loading = new MutationObserver(() => {
        if (isLoading(viewport)) {
            return;
        }

        state.loading.disconnect();
        state.loading = null;
        if (viewport.isConnected && isContentSizing(viewport)) {
            sizeContent(viewport, state.onChanged);
        }
    });
    state.loading.observe(grid, { attributes: true, attributeFilter: ['aria-busy'] });
}

/** A CSS length in pixels, laid out once per context (a probe in the grid, so it follows the grid's font). */
function pixelsOf(viewport, state, declared) {
    const text = (declared ?? '').trim();
    if (text.length === 0) {
        return 0;
    }

    if (!state.lengths.has(text)) {
        state.lengths.set(text, Math.ceil(lengthInPixels(viewport, text)));
    }
    return state.lengths.get(text);
}

// A new text size, density or font changes every measure: they are taken again.
function syncContext(viewport, header, state) {
    const grid = getComputedStyle(viewport);
    const cell = header.children[0] ? getComputedStyle(header.children[0]) : grid;
    const context = [grid.fontSize, grid.fontFamily, cell.fontSize, cell.fontWeight, cell.letterSpacing, cell.paddingInlineStart].join('|');
    if (context !== state.context) {
        state.context = context;
        state.textWidths.clear();
        state.widths.clear();
        state.lengths.clear();
    }
}

// The room of the grid follows its window: the widths are shared out again from what was measured.
function watchViewport(viewport, state) {
    if (state.watcher || typeof ResizeObserver !== 'function') {
        return;
    }

    state.watcher = new ResizeObserver(() => {
        if (viewport.isConnected && isContentSizing(viewport)) {
            sizeContent(viewport, state.onChanged, false);
        }
    });
    state.watcher.observe(viewport);
}

// What a column is for the plan: its key, the width it cannot go under and who gets the room left.
function describe(viewport, state, col) {
    const key = col.getAttribute('data-omni-col');
    const minimum = pixelsOf(viewport, state, col.style.getPropertyValue('--omni-col-min'));
    const chosen = col.getAttribute('data-omni-width');
    const flexible = col.hasAttribute('data-omni-truncate') || col.hasAttribute('data-omni-wrap');
    return {
        key,
        col,
        minimum,
        chosen: chosen ? pixelsOf(viewport, state, chosen) : null,
        // Truncate and Wrap have no width of their own; Grow measures its content like any other column.
        measured: !flexible && !chosen,
        role: chosen ? 'fixed' : flexible || col.hasAttribute('data-omni-grow') ? 'flexible' : 'plain'
    };
}

// Measures the columns that are measured and keeps the widest each one has been.
function measure(viewport, state, described) {
    const keys = described.filter(column => column.measured).map(column => column.key);
    if (keys.length === 0) {
        return;
    }

    const rendered = measureRenderedColumns(viewport, keys, true);
    for (const key of keys) {
        const widest = Math.max(rendered.get(key) ?? 0, widestTextOf(state, key, texts => measureColumnTexts(viewport, key, texts)));

        // One pixel more than the content, so a cell never loses its last fraction.
        state.widths.set(key, Math.max(state.widths.get(key) ?? 0, widest > 0 ? Math.ceil(widest) + 1 : 0));
    }
}

/**
 * Sizes the columns of the grid to their content and shares out the room left. With remeasure false, only
 * the sharing is redone (the window changed, the content did not). Returns whether a width changed;
 * onChanged is told too, for the offsets of frozen columns, and is the one a later resize of the window
 * calls. Nothing happens while the grid is not laid out (a hidden tab): the title watcher runs it again
 * when it is shown.
 */
export function sizeContent(viewport, onChanged, remeasure = true) {
    const header = viewport.querySelector('thead > tr.omni-data-grid__header-row');
    const cols = [...viewport.querySelectorAll('colgroup > col')];
    if (!header || cols.length !== header.children.length || viewport.getClientRects().length === 0) {
        return false;
    }

    const state = stateOf(viewport);
    state.onChanged = onChanged ?? state.onChanged;
    syncContext(viewport, header, state);
    watchViewport(viewport, state);

    // A grid that has not shown yet and is loading (aria-busy) holds loading rows, not its own: it is sized
    // and shown once, on its first real rows, so the reader never sees the widths of the loading rows jump.
    if (!isShown(viewport) && isLoading(viewport)) {
        awaitRows(viewport, state);
        return false;
    }

    const described = cols.map(col => describe(viewport, state, col)).filter(column => column.key !== null);
    if (remeasure || described.some(column => column.measured && !state.widths.has(column.key))) {
        measure(viewport, state, described);
    }

    const floor = pixelsOf(viewport, state, 'var(--omni-data-grid-column-min-width)');
    const planned = described.map(column => ({
        key: column.key,
        role: column.role,
        base: column.chosen ?? Math.max(column.measured ? state.widths.get(column.key) ?? floor : floor, column.minimum)
    }));
    const controls = cols.reduce((sum, col, index) => sum + (col.hasAttribute('data-omni-col') ? 0 : header.children[index].getBoundingClientRect().width), 0);
    const plan = planColumnWidths(planned, controls, viewport.clientWidth);

    let changed = false;
    for (const column of described) {
        const value = `${plan.widths.get(column.key)}px`;
        if (column.col.style.getPropertyValue('--omni-col-width') !== value) {
            column.col.style.setProperty('--omni-col-width', value);
            changed = true;
        }
    }

    // Below the sum of the bases the viewport scrolls sideways instead of squeezing a column.
    viewport.style.setProperty('--omni-grid-fit-min', `${plan.minimum}px`);
    // The widths are in place: the stylesheet shows the table it kept hidden since its first paint.
    viewport.querySelector(':scope > table.omni-data-grid__table')?.setAttribute('data-omni-sized', '');
    if (changed) {
        state.onChanged?.();
    }
    return changed;
}
