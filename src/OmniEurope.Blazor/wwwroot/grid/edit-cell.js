// Part of omni-grid.js, which re-exports attachEditCell and detachEditCell: the single click that puts a
// row in edit mode when the grid's EditTrigger is Cell. The viewport listens to its own clicks; one that
// lands on an editable cell, away from any control the cell holds, is reported to .NET with the row and the
// column, and .NET opens the row (EditRowAsync's path) and asks focusEditor for that cell's field. On a row
// already being edited the click only moves the focus to the field of the cell. The keyboard does not come
// through here: each editable cell holds a real button, whose activation .NET handles by itself.
import { focusCellEditor } from './edit-focus.js';
import { notifyDotNet } from './notify.js';
import { release } from './release.js';

// What a click inside a cell leaves to its own owner: a control (button, link, field, label, summary), an
// element playing one of the widget roles, anything the keyboard can focus, and the popups a cell opens.
const CONTROL = [
    'a[href]', 'button', 'input:not([type="hidden"])', 'select', 'textarea', 'summary', 'label',
    '[contenteditable=""]', '[contenteditable="true"]', '[tabindex]:not([tabindex="-1"])',
    '[role="button"]', '[role="link"]', '[role="checkbox"]', '[role="switch"]', '[role="radio"]', '[role="tab"]',
    '[role="combobox"]', '[role="textbox"]', '[role="slider"]', '[role="menuitem"]', '[role="option"]',
    '[role="listbox"]', '[role="menu"]', '[role="dialog"]'
].join(', ');

const attachments = new Map();

/**
 * Lets the viewport report the clicks that edit a row: attached once per grid while EditTrigger is Cell,
 * detached when it goes back to Action or the grid leaves the page.
 */
export function attachEditCell(viewport, reference) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachEditCell(viewport);
    const listener = event => pressCell(viewport, reference, event);
    viewport.addEventListener('click', listener);
    attachments.set(viewport, { dispose: () => viewport.removeEventListener('click', listener) });
}

export function detachEditCell(viewport) {
    release(attachments, viewport);
}

// A plain primary click; a modified one belongs to the selection or the browser.
function isPlainClick(event) {
    return !event.shiftKey && !event.ctrlKey && !event.metaKey && !event.altKey;
}

// Whether the click ended a text selection made inside the cell: the reader was copying, not editing.
function selectedIn(cell) {
    const selection = window.getSelection();
    return selection !== null && !selection.isCollapsed && cell.contains(selection.anchorNode);
}

function pressCell(viewport, reference, event) {
    const target = event.target instanceof Element ? event.target : null;
    const cell = target?.closest('td[data-omni-col]');
    const row = cell?.parentElement;
    if (!isPlainClick(event) || !cell || !viewport.contains(cell) || !row?.hasAttribute('data-omni-row-index')) {
        return;
    }

    const control = target.closest(CONTROL);
    if ((control !== null && control !== cell && cell.contains(control)) || selectedIn(cell)) {
        return;
    }

    if (row.classList.contains('omni-data-grid__row--editing')) {
        focusCellEditor(cell);
        return;
    }

    const index = Number.parseInt(row.getAttribute('data-omni-row-index') ?? '', 10);
    if (cell.classList.contains('omni-data-grid__cell--edit-trigger') && !Number.isNaN(index)) {
        notifyDotNet(() => attachments.has(viewport), reference, 'OnEditCellAsync', index, cell.getAttribute('data-omni-col'));
    }
}
