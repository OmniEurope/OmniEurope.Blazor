// The editor a row takes the focus in when it enters edit mode (recette R1-5): the editable field of the
// cell that was just pressed in that row, otherwise the first editable field of the row, its text selected
// whole so that typing replaces it. Enter and Escape typed in that row then save and cancel it (below).

// The last press anywhere in the page, read when a row enters edit mode right after it (a double click,
// a click a host turns into EditRowAsync). Captured, so a handler that stops the event still lets it through.
// The column is kept by its key: entering edit mode renders the row's cells anew, so the pressed cell
// itself is gone by then; the row element stays.
let lastPress = { row: null, column: null, time: 0 };
// A press older than this did not open the row: the row was opened from code or the keyboard.
const PRESS_WINDOW_MS = 1500;

document.addEventListener('pointerdown', event => {
    const cell = event.target instanceof Element ? event.target.closest('td[data-omni-col]') : null;
    lastPress = { row: cell?.parentElement ?? null, column: cell?.dataset.omniCol ?? null, time: performance.now() };
}, { capture: true, passive: true });

const EDITABLE = [
    'input:not([type=hidden]):not([type=button]):not([type=submit]):not([type=reset]):not([disabled]):not([readonly])',
    'textarea:not([disabled]):not([readonly])',
    'select:not([disabled])',
    '[contenteditable=""], [contenteditable="true"]'
].join(', ');

// Text-like fields whose content select() can take whole.
const SELECTABLE = new Set(['text', 'search', 'tel', 'url', 'email', 'password', 'number', '']);

const editorIn = cell => cell?.querySelector(EDITABLE) ?? null;

/**
 * Focuses the editor of the row marked data-omni-edit-focus in the viewport: the pressed cell's when it
 * has one, otherwise the first one of the row, and selects its text. Returns false while no row is marked
 * (the render that marks it has not reached the page yet), true once the marked row was handled.
 */
export function focusEditor(viewport) {
    const row = viewport instanceof Element ? viewport.querySelector('tr[data-omni-edit-focus]') : null;
    if (!row) {
        return false;
    }

    const pressed = lastPress.row === row && performance.now() - lastPress.time < PRESS_WINDOW_MS
        ? editorIn([...row.children].find(cell => cell.dataset?.omniCol === lastPress.column))
        : null;
    const editor = pressed ?? [...row.querySelectorAll('td[data-omni-col]')].map(editorIn).find(Boolean);
    if (!editor) {
        return true;
    }

    editor.focus();
    const type = editor instanceof HTMLInputElement ? editor.getAttribute('type')?.toLowerCase() ?? '' : null;
    if (editor instanceof HTMLTextAreaElement || (type !== null && SELECTABLE.has(type))) {
        editor.select();
    }
    return true;
}

// Enter saves a row in edit mode and Escape cancels it (G-D29-03), through the row's own save and cancel
// buttons: the keys take the very path of a click (OnRowUpdate with the host's validation, OnRowEditCancel).
// Listened to on the document in the capture phase, before any handler of the page, so an editor is read
// in the state the key found it in: a list, a calendar or a disclosure open in the cell keeps both keys,
// Enter stays a new line in a multi-line field and the press of a button focused in the cell. A row whose grid
// shows no edit column has no such buttons: its keys are left to the host.
const OPEN_POPUP = '[aria-expanded="true"]:not(.omni-data-grid__tree-toggle), details[open]';
// The row's own buttons, in its edit column.
const SAVE = ':scope > td.omni-data-grid__actions .omni-data-grid__action--save';
const CANCEL = ':scope > td.omni-data-grid__actions .omni-data-grid__action--cancel';
const PRESSES_ENTER = 'button, a[href], summary, [role="button"], [role="link"], input[type=button], input[type=submit], input[type=reset]';

const keepsEnter = target => target instanceof HTMLTextAreaElement
    || target.isContentEditable
    || target.closest('[aria-multiline="true"]') !== null
    || target.matches(PRESSES_ENTER);

/** The row in edit mode and the button the key presses in it, or null when the key is not the grid's. */
function editAction(event) {
    if ((event.key !== 'Enter' && event.key !== 'Escape') || event.defaultPrevented || event.isComposing
        || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
        return null;
    }

    const target = event.target instanceof HTMLElement ? event.target : null;
    const cell = target?.closest('td[data-omni-col]');
    const row = cell?.parentElement;
    if (!row?.classList.contains('omni-data-grid__row--editing') || cell.querySelector(OPEN_POPUP)) {
        return null;
    }

    if (event.key === 'Enter' && keepsEnter(target)) {
        return null;
    }

    const button = row.querySelector(event.key === 'Enter' ? SAVE : CANCEL);
    return button && !button.disabled ? { target, row, button } : null;
}

// The focus left with the editors when the row left edit mode: it goes to the row when the row takes it,
// otherwise to the row's edit button, so the keyboard stays in the grid. A row still in edit mode (a
// host that opens it again from OnRowUpdate) keeps the focus. One row is watched at a time, a few seconds.
let watched = null;
const WATCH_MS = 5000;

function refocusWhenLeft(row) {
    watched?.disconnect();
    const observer = new MutationObserver(() => {
        if (!row.isConnected) {
            observer.disconnect();
            return;
        }
        if (row.classList.contains('omni-data-grid__row--editing')) {
            return;
        }

        observer.disconnect();
        const active = document.activeElement;
        if (active === null || active === document.body || !active.isConnected) {
            const target = row.hasAttribute('tabindex') ? row : row.querySelector(':scope > td.omni-data-grid__actions button');
            target?.focus({ preventScroll: true });
        }
    });
    observer.observe(row, { attributes: true, attributeFilter: ['class'] });
    watched = observer;
    window.setTimeout(() => observer.disconnect(), WATCH_MS);
}

document.addEventListener('keydown', event => {
    const action = editAction(event);
    if (!action) {
        return;
    }

    event.preventDefault();
    // A click on the save button first takes the focus from the field, whose change event commits the
    // typed value; a key does not, so the field commits it here, before the save reads the item.
    if (event.key === 'Enter' && (action.target instanceof HTMLInputElement || action.target instanceof HTMLSelectElement)) {
        action.target.dispatchEvent(new Event('change', { bubbles: true }));
    }
    // Watched before the click: on WebAssembly the click renders synchronously, so a row watched after it
    // would already have left edit mode unseen.
    refocusWhenLeft(action.row);
    action.button.click();
}, { capture: true });
