// HighlightColumnOnHover: the body cell under the pointer tints its whole column, header and footer
// included. Only a class is toggled on the cells (omni-data-grid__column--hover), never a style, so it
// holds under a strict CSP. The grid root carries omni-data-grid--column-hover while the option is on;
// a column that opts out carries omni-data-grid__column--no-hover and marks nothing.

import { release } from './release.js';

const HOVER = 'omni-data-grid__column--hover';
const hovers = new Map();

// The cells of one column of this grid, nested grids (a detail row) left out.
function columnCells(viewport, key) {
    const selector = `:is(th, td)[data-omni-col="${CSS.escape(key)}"]`;
    return [...viewport.querySelectorAll(selector)]
        .filter(cell => cell.closest('.omni-data-grid__viewport') === viewport);
}

export function attachColumnHover(viewport) {
    if (!(viewport instanceof HTMLElement) || hovers.has(viewport)) {
        return;
    }

    let key = null;
    const clear = () => {
        if (key === null) {
            return;
        }

        for (const cell of columnCells(viewport, key)) {
            cell.classList.remove(HOVER);
        }
        key = null;
    };
    const mark = next => {
        clear();
        key = next;
        for (const cell of columnCells(viewport, next)) {
            cell.classList.add(HOVER);
        }
    };
    const over = event => {
        const cell = event.target instanceof Element ? event.target.closest('td[data-omni-col]') : null;
        const enabled = viewport.closest('.omni-data-grid')?.classList.contains('omni-data-grid--column-hover');
        if (!enabled || !cell || cell.closest('.omni-data-grid__viewport') !== viewport || !cell.closest('tbody')
            || cell.classList.contains('omni-data-grid__column--no-hover')) {
            clear();
            return;
        }

        // The same column again: marked once, unless a render replaced the classes of its cells.
        if (cell.dataset.omniCol !== key || !cell.classList.contains(HOVER)) {
            mark(cell.dataset.omniCol);
        }
    };

    viewport.addEventListener('pointerover', over);
    viewport.addEventListener('pointerleave', clear);
    hovers.set(viewport, () => {
        viewport.removeEventListener('pointerover', over);
        viewport.removeEventListener('pointerleave', clear);
        clear();
    });
}

export function detachColumnHover(viewport) {
    release(hovers, viewport, run => run());
}
