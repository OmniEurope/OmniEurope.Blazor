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
    configureOverflow(strip, '.omni-tabs__viewport', '.omni-tabs__scroll--start', '.omni-tabs__scroll--end');
}

// A scrolling OmniStack row works the same way: no scrollbar, a chevron on each side that still
// holds items, gone at the stop.
export function configureScrollOverflow(strip) {
    configureOverflow(strip, ':scope > .omni-stack-scroll__viewport', ':scope > .omni-stack-scroll__button--start', ':scope > .omni-stack-scroll__button--end');
}

function configureOverflow(strip, viewportSelector, startSelector, endSelector) {
    if (!strip || tabOverflow.has(strip)) {
        return;
    }

    const viewport = strip.querySelector(viewportSelector);
    if (!viewport) {
        return;
    }

    const start = strip.querySelector(startSelector);
    const end = strip.querySelector(endSelector);

    // The browser leaves a focused item partly clipped when it is already partly in view, and a
    // chevron that appears afterwards narrows the strip over it. The item reached by the keyboard is
    // brought wholly inside the content box, at once rather than smoothly, so the measure after it is
    // the final one. Only keyboard focus: a button clicked earlier must not pull the strip back while
    // the reader scrolls by hand, so a plain scroll never calls this.
    const reveal = () => {
        const focused = document.activeElement;
        if (!focused || focused === viewport || !viewport.contains(focused) || !focused.matches(':focus-visible')) {
            return;
        }

        const box = viewport.getBoundingClientRect();
        const padding = getComputedStyle(viewport);
        const left = box.left + parseFloat(padding.paddingLeft);
        const right = box.right - parseFloat(padding.paddingRight);
        const item = focused.getBoundingClientRect();
        const delta = item.left < left ? item.left - left : item.right > right ? item.right - right : 0;
        if (Math.abs(delta) > 0.5) {
            viewport.scrollBy({ left: delta, behavior: 'instant' });
        }
    };

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
    viewport.addEventListener('focusin', reveal);

    // The strip also overflows when the window narrows or when a tab is added, neither of which
    // fires a scroll event. A chevron appearing narrows the strip too, which is when a focused item
    // it now covers is brought back.
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => {
        update();
        reveal();
    });
    observer?.observe(viewport);
    const mutations = typeof MutationObserver === 'undefined' ? null : new MutationObserver(update);
    mutations?.observe(viewport, { childList: true, subtree: true });

    tabOverflow.set(strip, { viewport, start, end, onStart, onEnd, update, reveal, observer, mutations });
    update();
}

export function disposeScrollOverflow(strip) {
    disposeTabsOverflow(strip);
}

// OmniSelectBar: same chevrons, no scrollbar.
export function configureSelectBarOverflow(strip) {
    configureOverflow(strip, ':scope > .omni-select-bar__viewport', ':scope > .omni-select-bar__scroll--start', ':scope > .omni-select-bar__scroll--end');
}

// A press inside an element marked data-omni-keep-open never counts as outside: it lets a panel of
// settings drive an open menu without closing it.
function pressedOutside(event, ...inside) {
    const target = event.target;
    if (!(target instanceof Node) || inside.some(element => element?.contains(target))) {
        return false;
    }

    return !(target instanceof Element && target.closest('[data-omni-keep-open]'));
}

const disclosures = new WeakMap();

// A native <details> used as a dropdown closes on Escape, on a press outside it unless the host
// turned that off, and, for a menu, once an item is chosen. The document listener only lives while
// the panel is open.
export function configureDisclosure(details, closeOnOutsideClick, closeOnItem) {
    if (!details) {
        return;
    }

    const known = disclosures.get(details);
    if (known) {
        known.outside = closeOnOutsideClick;
        known.item = closeOnItem;
        return;
    }

    const state = { outside: closeOnOutsideClick, item: closeOnItem, listening: false };
    const listen = on => {
        if (on !== state.listening) {
            document[on ? 'addEventListener' : 'removeEventListener']('pointerdown', state.onPointerDown, true);
            state.listening = on;
        }
    };
    state.onPointerDown = event => {
        if (!details.isConnected) {
            listen(false);
        } else if (state.outside && details.open && pressedOutside(event, details)) {
            details.open = false;
        }
    };
    state.onKeyDown = event => {
        if (event.key === 'Escape' && details.open) {
            event.stopPropagation();
            details.open = false;
            details.querySelector(':scope > summary')?.focus({ preventScroll: true });
        }
    };
    state.onClick = event => {
        const item = state.item && event.target instanceof Element ? event.target.closest('[role="menuitem"]') : null;
        if (item && details.contains(item) && !item.matches(':disabled, [aria-disabled="true"]')) {
            details.open = false;
        }
    };
    state.onToggle = () => listen(details.open);
    details.addEventListener('keydown', state.onKeyDown);
    details.addEventListener('click', state.onClick);
    details.addEventListener('toggle', state.onToggle);
    disclosures.set(details, state);
    state.onToggle();
}

export function disposeDisclosure(details) {
    const state = details ? disclosures.get(details) : undefined;
    if (!state) {
        return;
    }

    document.removeEventListener('pointerdown', state.onPointerDown, true);
    details.removeEventListener('keydown', state.onKeyDown);
    details.removeEventListener('click', state.onClick);
    details.removeEventListener('toggle', state.onToggle);
    disclosures.delete(details);
}

const contextMenus = new Map();

// A context menu opens where it was asked for: at the pointer, or under its trigger when the
// keyboard opened it, and moves back inside the viewport rather than past its edge. The popup is
// found by id because the overlay portal may render it far from the component, which is also why no
// element reference is passed: a reference the portal never captured reached this script as an
// empty object and threw. A second call while open only moves the menu.
export function openContextMenu(popupId, key, trigger, x, y, dotnet) {
    let state = contextMenus.get(key);
    if (!state) {
        rememberTarget(key);
        state = { popup: null, attempts: 0 };
        state.onPointerDown = event => {
            // A second right-click on the trigger moves the menu instead of closing it.
            if (event.button === 2 && trigger?.contains(event.target)) {
                return;
            }

            if (pressedOutside(event, state.popup)) {
                void dotnet.invokeMethodAsync('OmniContextMenu.Dismiss');
            }
        };
        document.addEventListener('pointerdown', state.onPointerDown, true);
        contextMenus.set(key, state);
    }

    const popup = document.getElementById(popupId);
    if (!popup) {
        if (state.attempts++ < 10) {
            setTimeout(() => contextMenus.get(key) === state && openContextMenu(popupId, key, trigger, x, y, dotnet), 16);
        }

        return;
    }

    state.attempts = 0;
    state.popup = popup;
    placeMenu(popup, trigger, x, y);
    const items = Array.from(popup.querySelectorAll('[role="menuitem"]:not([disabled])'));
    (items[0] ?? popup).focus({ preventScroll: true });
}

function placeMenu(popup, trigger, x, y) {
    let left = x;
    let top = y;
    if (typeof left !== 'number' || typeof top !== 'number') {
        const anchor = trigger?.getBoundingClientRect();
        left = anchor?.left ?? 0;
        top = anchor?.bottom ?? 0;
    }

    const place = () => {
        popup.style.setProperty('--omni-menu-x', `${Math.round(left)}px`);
        popup.style.setProperty('--omni-menu-y', `${Math.round(top)}px`);
    };
    popup.setAttribute('data-omni-placed', '');
    place();

    // Past the right or bottom edge, the menu opens towards the other side of the pointer, as a
    // native one does, and never starts outside the viewport.
    const margin = 8;
    const box = popup.getBoundingClientRect();
    const width = document.documentElement.clientWidth;
    const height = document.documentElement.clientHeight;
    if (box.right > width - margin) {
        left = Math.max(margin, Math.min(left - box.width, width - box.width - margin));
    }

    if (box.bottom > height - margin) {
        top = Math.max(margin, Math.min(top - box.height, height - box.height - margin));
    }

    place();
}

export function moveContextMenuFocus(popupId, key) {
    moveMenuFocus(document.getElementById(popupId), key);
}

// Focus goes back to where it was only when the menu still held it: a press outside that moved it
// to another control leaves it there.
export function closeContextMenu(key) {
    const state = contextMenus.get(key);
    if (state) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
        contextMenus.delete(key);
    }

    const active = document.activeElement;
    if (active instanceof HTMLElement && active !== document.body && active.isConnected && !state?.popup?.contains(active)) {
        returnTargets.delete(key);
        return;
    }

    return restoreFocus(key);
}

export function disposeTabsOverflow(strip) {
    const state = tabOverflow.get(strip);
    if (!state) {
        return;
    }

    state.start?.removeEventListener('click', state.onStart);
    state.end?.removeEventListener('click', state.onEnd);
    state.viewport.removeEventListener('scroll', state.update);
    state.viewport.removeEventListener('focusin', state.reveal);
    state.observer?.disconnect();
    state.mutations?.disconnect();
    tabOverflow.delete(strip);
}
