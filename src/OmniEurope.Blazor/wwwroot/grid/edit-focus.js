// The editor a row takes the focus in when it enters edit mode (recette R1-5): the editable field of the
// cell that was just pressed in that row, otherwise the first editable field of the row, its text selected
// whole so that typing replaces it.

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
