const returnTargets = new Map();
const dialogHandlers = new Map();
const tabHandlers = new WeakMap();
const tabOverflow = new WeakMap();

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

// A tab strip never wraps, so it has to say when it hides tabs off either edge. The strip carries
// data-omni-overflow-start / -end, which the stylesheet turns into the fade, and the two chevron
// buttons are unhidden with it. Both go out at the stops, so reaching an end is visible.
export function configureTabsOverflow(strip) {
    if (!strip || tabOverflow.has(strip)) {
        return;
    }

    const viewport = strip.querySelector('.omni-tabs__viewport');
    if (!viewport) {
        return;
    }

    const start = strip.querySelector('.omni-tabs__scroll--start');
    const end = strip.querySelector('.omni-tabs__scroll--end');

    const update = () => {
        // Right-to-left scrolling reports scrollLeft as negative or decreasing, so the distance to
        // each edge is measured in absolute terms rather than from the raw value.
        const offset = Math.abs(viewport.scrollLeft);
        const hidden = viewport.scrollWidth - viewport.clientWidth;
        // A sub-pixel remainder is not an overflow: rounding alone would keep a chevron lit on a
        // strip that has nothing left to show.
        const atStart = offset <= 1;
        const atEnd = offset >= hidden - 1;
        strip.toggleAttribute('data-omni-overflow-start', !atStart);
        strip.toggleAttribute('data-omni-overflow-end', hidden > 1 && !atEnd);
        if (start) start.hidden = atStart;
        if (end) end.hidden = hidden <= 1 || atEnd;
    };

    const scrollBy = direction => {
        // A step short of a full page keeps one tab in common between the two views, so the reader
        // never loses their place.
        viewport.scrollBy({ left: direction * viewport.clientWidth * 0.8, behavior: 'smooth' });
    };

    const onStart = () => scrollBy(-1);
    const onEnd = () => scrollBy(1);
    start?.addEventListener('click', onStart);
    end?.addEventListener('click', onEnd);
    viewport.addEventListener('scroll', update, { passive: true });

    // The strip also overflows when the window narrows or when a tab is added, neither of which
    // fires a scroll event.
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(update);
    observer?.observe(viewport);
    const mutations = typeof MutationObserver === 'undefined' ? null : new MutationObserver(update);
    mutations?.observe(viewport, { childList: true, subtree: true });

    tabOverflow.set(strip, { viewport, start, end, onStart, onEnd, update, observer, mutations });
    update();
}

export function disposeTabsOverflow(strip) {
    const state = tabOverflow.get(strip);
    if (!state) {
        return;
    }

    state.start?.removeEventListener('click', state.onStart);
    state.end?.removeEventListener('click', state.onEnd);
    state.viewport.removeEventListener('scroll', state.update);
    state.observer?.disconnect();
    state.mutations?.disconnect();
    tabOverflow.delete(strip);
}
