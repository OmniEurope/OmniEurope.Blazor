const attachments = new Map();

const navigationKeys = new Set([
    'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown',
    'Enter', 'F2', 'Delete', 'Backspace'
]);
const arrowKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']);

/**
 * Which keys keep their browser default. Blazor can only cancel every key of an element or none,
 * while a spreadsheet needs it per key: on the sheet, the navigation keys and typed characters must
 * not scroll the page, but Tab at the end of a row must still leave the sheet; in a cell editor,
 * Enter, Tab and Escape end the edit instead of doing what a text box does. The .NET side receives
 * the same keys and does the moving; this only decides the default.
 */
export function attach(root) {
    if (!(root instanceof HTMLElement)) {
        return;
    }

    detach(root);
    const onKeyDown = event => {
        const target = event.target instanceof Element ? event.target : null;
        if (!target || event.isComposing) {
            return;
        }

        if (target.matches('[data-omni-sheet-editor]')) {
            const replacing = target.getAttribute('data-omni-sheet-mode') === 'replace';
            if (['Enter', 'Tab', 'Escape'].includes(event.key) || (replacing && arrowKeys.has(event.key))) {
                event.preventDefault();
            }

            return;
        }

        if (target.matches('[data-omni-sheet-bar]')) {
            if (event.key === 'Enter' || event.key === 'Escape') {
                event.preventDefault();
            }

            return;
        }

        if (!target.matches('[data-omni-sheet-grid]') || event.altKey || event.metaKey) {
            return;
        }

        if (event.key === 'Tab') {
            const column = Number(target.getAttribute('data-active-column'));
            const last = Number(target.getAttribute('data-column-count')) - 1;
            if (event.shiftKey ? column > 0 : column < last) {
                event.preventDefault();
            }

            return;
        }

        // Ctrl with a letter stays the browser's (copy, find, reload); Ctrl with a move is ours.
        if (navigationKeys.has(event.key) || (!event.ctrlKey && event.key.length === 1)) {
            event.preventDefault();
        }
    };

    root.addEventListener('keydown', onKeyDown, true);
    attachments.set(root, () => root.removeEventListener('keydown', onKeyDown, true));
}

export function detach(root) {
    const dispose = attachments.get(root);
    if (dispose) {
        dispose();
        attachments.delete(root);
    }
}

/** Focuses the cell editor with the caret after what it holds, ready to go on typing. */
export function focusEditor(input) {
    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    input.focus({ preventScroll: true });
    const end = input.value.length;
    input.setSelectionRange(end, end);
}

/** Brings the active cell into view inside the sheet's own scroll box. */
export function reveal(root, cellId) {
    if (!(root instanceof HTMLElement) || typeof cellId !== 'string') {
        return;
    }

    const cell = root.querySelector(`#${CSS.escape(cellId)}`);
    const viewport = root.querySelector('[data-omni-sheet-grid]');
    if (!cell || !viewport) {
        return;
    }

    // Measured against the viewport under its sticky headers, so a cell is never left behind them.
    const header = viewport.querySelector('thead');
    const rowHeader = cell.parentElement?.querySelector('th');
    const box = viewport.getBoundingClientRect();
    const target = cell.getBoundingClientRect();
    const top = box.top + (header ? header.getBoundingClientRect().height : 0);
    const left = box.left + (rowHeader ? rowHeader.getBoundingClientRect().width : 0);
    if (target.top < top) {
        viewport.scrollTop -= top - target.top;
    } else if (target.bottom > box.bottom) {
        viewport.scrollTop += target.bottom - box.bottom;
    }

    if (target.left < left) {
        viewport.scrollLeft -= left - target.left;
    } else if (target.right > box.right) {
        viewport.scrollLeft += target.right - box.right;
    }
}
