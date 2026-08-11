const returnTargets = new Map();
const dialogHandlers = new Map();
const tabHandlers = new WeakMap();

function focusableElements(container) {
    if (!container) {
        return [];
    }

    return Array.from(container.querySelectorAll(
        'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'))
        .filter(element => !element.hidden
            && element.getAttribute('aria-hidden') !== 'true'
            && !element.hasAttribute('data-focus-sentinel'));
}

function rememberTarget(key) {
    if (!returnTargets.has(key) && document.activeElement instanceof HTMLElement) {
        returnTargets.set(key, document.activeElement);
    }
}

export function activateMenu(menu, key) {
    rememberTarget(key);
    const items = Array.from(menu?.querySelectorAll('[role="menuitem"]:not([disabled])') ?? []);
    (items[0] ?? menu)?.focus({ preventScroll: true });
}

export function moveMenuFocus(menu, key) {
    const items = Array.from(menu?.querySelectorAll('[role="menuitem"]:not([disabled])') ?? []);
    if (items.length === 0) {
        menu?.focus();
        return;
    }

    const current = Math.max(0, items.indexOf(document.activeElement));
    let next = current;
    if (key === 'ArrowDown') next = (current + 1) % items.length;
    if (key === 'ArrowUp') next = (current - 1 + items.length) % items.length;
    if (key === 'Home') next = 0;
    if (key === 'End') next = items.length - 1;
    items[next].focus();
}

export function activateDialog(dialog, key) {
    rememberTarget(key);
    const items = focusableElements(dialog);
    (items[0] ?? dialog)?.focus({ preventScroll: true });

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

        const current = currentItems.indexOf(document.activeElement);
        const start = current < 0 ? 0 : current;
        const next = event.shiftKey
            ? (start - 1 + currentItems.length) % currentItems.length
            : (start + 1) % currentItems.length;
        currentItems[next].focus();
    };

    dialog.addEventListener('keydown', handler);
    dialogHandlers.set(key, { dialog, handler });
}

export function trapDialogTab(dialog, shiftKey) {
    const items = focusableElements(dialog);
    if (items.length === 0) {
        dialog?.focus();
        return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    if (shiftKey && document.activeElement === first) {
        last.focus();
    } else if (!shiftKey && document.activeElement === last) {
        first.focus();
    }
}

export function focusBoundary(dialog, last) {
    const items = focusableElements(dialog);
    (last ? items.at(-1) : items[0] ?? dialog)?.focus();
}

export function restoreFocus(key) {
    const dialogState = dialogHandlers.get(key);
    if (dialogState) {
        dialogState.dialog.removeEventListener('keydown', dialogState.handler);
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

export function configureTabs(tablist, dotnet) {
    if (!tablist || tabHandlers.has(tablist)) {
        return;
    }

    const handler = event => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        const current = event.target.closest('[role="tab"]');
        if (!current || !tablist.contains(current)) {
            return;
        }
        const tabs = Array.from(tablist.querySelectorAll('[role="tab"]:not([disabled]):not([aria-disabled="true"])'));
        if (tabs.length === 0) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const index = Math.max(0, tabs.indexOf(current));
        let next = index;
        if (event.key === 'Home') next = 0;
        if (event.key === 'End') next = tabs.length - 1;
        if (event.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
        if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
        tabs[next].focus({ preventScroll: true });
        void dotnet.invokeMethodAsync('OmniTabs.SelectFromKeyboard', tabs[next].dataset.key);
    };
    tablist.addEventListener('keydown', handler);
    tabHandlers.set(tablist, handler);
}

export function disposeTabs(tablist) {
    const handler = tabHandlers.get(tablist);
    if (handler) {
        tablist.removeEventListener('keydown', handler);
        tabHandlers.delete(tablist);
    }
}
