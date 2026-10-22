// Part of omni-focus.js, which re-exports activateDialog, activateWindow, focusBoundary and
// restoreFocus: the focus trap and the focus return of dialogs and windows.
import { returnTargets, rememberTarget } from './shared.js';

export function focusableElements(container) {
    if (!container) {
        return [];
    }

    return Array.from(container.querySelectorAll(
        'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'))
        // An element under a hidden or inert ancestor, or not laid out at all (display: none), cannot
        // take the focus: keeping it would make the trap retry it on every Tab.
        // Out of the tab order too: tabindex -1 on a button (the options of a radio group, which
        // keeps one tab stop) takes it out of the cycle as it does the browser's.
        .filter(element => element.getAttribute('tabindex') !== '-1'
            && !element.closest('[hidden], [inert]')
            && element.getAttribute('aria-hidden') !== 'true'
            && !element.hasAttribute('data-focus-sentinel')
            && element.getClientRects().length > 0);
}

const dialogHandlers = new Map();

// Where a dialog puts the focus as it opens: the panel itself ('panel'), the first element of its
// content that takes the focus ('content'), or by default the first in the dialog (the close button).
function focusInitial(dialog, initialFocus) {
    const items = focusableElements(dialog);
    const target = initialFocus === 'panel'
        ? dialog
        : (initialFocus === 'content' ? focusableElements(dialog?.querySelector('.omni-dialog__content'))[0] : null)
            ?? items[0] ?? dialog;
    target?.focus({ preventScroll: true });
}

export function activateDialog(dialog, key, holdBackdrop, initialFocus) {
    rememberTarget(key);
    focusInitial(dialog, initialFocus);

    const handler = event => {
        if (event.key !== 'Tab') {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const currentItems = focusableElements(dialog);
        if (currentItems.length === 0) {
            dialog?.focus();
            return;
        }

        // From the panel itself (or anything out of the cycle), Tab goes to the first element and
        // Shift+Tab to the last, as the browser does from the start of a page.
        const current = currentItems.indexOf(document.activeElement);
        const next = current < 0
            ? (event.shiftKey ? currentItems.length - 1 : 0)
            : event.shiftKey
                ? (current - 1 + currentItems.length) % currentItems.length
                : (current + 1) % currentItems.length;
        currentItems[next].focus();
    };

    dialog.addEventListener('keydown', handler);

    // A press on a backdrop that closes nothing would otherwise move focus to the body, out of the
    // trap, where Escape and Tab no longer reach the dialog. Only asked for by such a dialog.
    const overlay = holdBackdrop === true ? dialog.parentElement : null;
    const onBackdropDown = event => {
        if (event.target === overlay) {
            event.preventDefault();
        }
    };
    overlay?.addEventListener('mousedown', onBackdropDown);
    dialogHandlers.set(key, { dialog, handler, overlay, onBackdropDown });
}

// Explicitly opened modeless windows restore focus, but let Tab leave and the page remain usable.
export function activateWindow(dialog, key, initialFocus) {
    rememberTarget(key);
    focusInitial(dialog, initialFocus);
}

export function focusBoundary(dialog, last) {
    const items = focusableElements(dialog);
    (last ? items.at(-1) : items[0] ?? dialog)?.focus();
}

export function restoreFocus(key) {
    const dialogState = dialogHandlers.get(key);
    if (dialogState) {
        dialogState.dialog.removeEventListener('keydown', dialogState.handler);
        dialogState.overlay?.removeEventListener('mousedown', dialogState.onBackdropDown);
        dialogHandlers.delete(key);
    }

    const target = returnTargets.get(key);
    returnTargets.delete(key);
    if (!target) {
        return;
    }

    return new Promise(resolve => {
        let attempts = 0;
        const restore = () => {
            if (target.isConnected && !target.closest('[inert]')) {
                target.focus({ preventScroll: true });
                resolve();
                return;
            }

            attempts++;
            if (attempts < 5) {
                requestAnimationFrame(restore);
            } else {
                resolve();
            }
        };

        requestAnimationFrame(restore);
    });
}
