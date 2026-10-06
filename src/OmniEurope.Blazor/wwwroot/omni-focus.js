// Focus management, the module the components import (OmniModules.Focus): the focus trap and the
// focus return of dialogs and windows, the non-modal popover, the date and time pickers, the native
// <details> disclosures and fieldsets, the Escape listener of a floating sidebar and the phone width
// watch of a pushing one. The menus and the tab strips live in ./focus/ and are re-exported here, so
// every export keeps this address.
import { returnTargets, rememberTarget, viewportMargin, pressedOutside } from './focus/shared.js';

export { openMenu, closeMenu, moveMenuFocus } from './focus/menus.js';
export {
    configureTabs,
    disposeTabs,
    configureTabsOverflow,
    configureScrollOverflow,
    disposeScrollOverflow,
    configureSelectBarOverflow,
    configureSidebarOverflow,
    disposeSidebarOverflow,
    disposeTabsOverflow,
    attachTabsWheelScope,
    detachTabsWheelScope
} from './focus/tabs.js';

const dialogHandlers = new Map();

function focusableElements(container) {
    if (!container) {
        return [];
    }

    return Array.from(container.querySelectorAll(
        'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'))
        // An element under a hidden or inert ancestor, or not laid out at all (display: none), cannot
        // take the focus: keeping it would make the trap retry it on every Tab.
        .filter(element => !element.closest('[hidden], [inert]')
            && element.getAttribute('aria-hidden') !== 'true'
            && !element.hasAttribute('data-focus-sentinel')
            && element.getClientRects().length > 0);
}

export function activateDialog(dialog, key, holdBackdrop) {
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
export function activateWindow(dialog, key) {
    rememberTarget(key);
    (focusableElements(dialog)[0] ?? dialog)?.focus({ preventScroll: true });
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

// ---- non-modal popover -----------------------------------------------------------------------
// The panel does not trap focus: Tab may leave it. What closes it is a pointer press outside the
// popover (focus stays where the user clicked) or Escape inside it (focus returns to the trigger).

const popoverHandlers = new Map();

// The panel is anchored to its trigger by CSS; near an edge of the window it is shifted sideways, and
// opened above its trigger when there is no room below, so it never leaves the window.
function keepInViewport(panel) {
    if (!(panel instanceof HTMLElement)) {
        return;
    }

    panel.style.removeProperty('--omni-popover-shift-x');
    panel.classList.remove('omni-popover__panel--above');
    const rect = panel.getBoundingClientRect();
    const width = document.documentElement.clientWidth;
    const height = document.documentElement.clientHeight;
    let shift = 0;
    if (rect.right > width - viewportMargin) {
        shift = width - viewportMargin - rect.right;
    }
    if (rect.left + shift < viewportMargin) {
        shift = viewportMargin - rect.left;
    }
    if (shift !== 0) {
        panel.style.setProperty('--omni-popover-shift-x', `${Math.round(shift)}px`);
    }

    const trigger = panel.parentElement?.getBoundingClientRect();
    if (trigger && rect.bottom > height - viewportMargin && trigger.top - rect.height > viewportMargin) {
        panel.classList.add('omni-popover__panel--above');
    }
}

export function attachPopover(root, panel, dotnet, key) {
    if (!(root instanceof HTMLElement) || !dotnet || popoverHandlers.has(key)) {
        return;
    }

    rememberTarget(key);
    keepInViewport(panel);
    const items = focusableElements(panel);
    (items[0] ?? panel)?.focus({ preventScroll: true });
    const onResize = () => keepInViewport(panel);

    const onPointerDown = event => {
        if (event.target instanceof Node && !root.contains(event.target)) {
            void dotnet.invokeMethodAsync('OnDismissRequestedAsync', false);
        }
    };
    const onKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            void dotnet.invokeMethodAsync('OnDismissRequestedAsync', true);
        }
    };

    document.addEventListener('pointerdown', onPointerDown, true);
    root.addEventListener('keydown', onKeyDown);
    window.addEventListener('resize', onResize);
    popoverHandlers.set(key, { root, onPointerDown, onKeyDown, onResize });
}

export function detachPopover(key, restore) {
    const state = popoverHandlers.get(key);
    if (state) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
        state.root.removeEventListener('keydown', state.onKeyDown);
        window.removeEventListener('resize', state.onResize);
        popoverHandlers.delete(key);
    }

    // The trigger never leaves the page while its panel closes, so focus goes back at once, without
    // the animation frames restoreFocus waits for (a background tab would not deliver them).
    const target = returnTargets.get(key);
    returnTargets.delete(key);
    if (restore && target?.isConnected && !target.closest('[inert]')) {
        target.focus({ preventScroll: true });
    }
}

// ---- date and time pickers ------------------------------------------------------------------
// One picker panel is open at a time: opening one asks the others to close. A press outside the
// picker closes it and leaves the focus where it was put; Escape closes it and gives the focus back
// to its toggle, and goes no further, so a dialog holding the picker stays open. While a day of the
// grid or an item of a time column has the focus, the arrow, page, Home and End keys move inside the
// panel (.NET does it) instead of scrolling the page.

const pickerHandlers = new Map();
const pickerKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End']);

function centrePickerLists(panel) {
    for (const list of panel.querySelectorAll('.omni-time__list')) {
        const chosen = list.querySelector('[aria-selected="true"]');
        if (chosen) {
            list.scrollTop = chosen.offsetTop - list.offsetTop - (list.clientHeight / 2) + (chosen.offsetHeight / 2);
        }
    }
}

// The panel is fixed to the window and placed here, under its field, so whatever scrolls or clips around
// the picker (a dialog, a card, a grid) can neither cut it nor grow a scrollbar for it. It goes above
// the field when there is no room below, and stays whole inside the window. Custom properties on the
// panel only, which leaves the page with it.
function placePicker(root, panel) {
    const anchor = (root.querySelector('.omni-date__field') ?? root).getBoundingClientRect();
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    const gap = 6;
    const start = getComputedStyle(root).direction === 'rtl' ? anchor.right - width : anchor.left;
    const left = Math.max(viewportMargin, Math.min(start, window.innerWidth - width - viewportMargin));
    const below = anchor.bottom + gap;
    const top = below + height > window.innerHeight - viewportMargin && anchor.top - height - gap >= viewportMargin
        ? anchor.top - height - gap
        : below;
    const set = (x, y) => {
        panel.style.setProperty('--omni-picker-x', `${Math.round(x)}px`);
        panel.style.setProperty('--omni-picker-y', `${Math.round(y)}px`);
    };
    set(left, top);
    // An ancestor with a transform, a filter or a containment is the origin of position: fixed instead
    // of the window: the panel is then drawn off by that origin, measured here and taken back.
    const drawn = panel.getBoundingClientRect();
    if (Math.abs(drawn.left - left) > 1 || Math.abs(drawn.top - top) > 1) {
        set(2 * left - drawn.left, 2 * top - drawn.top);
    }
}

export function attachPicker(root, panel, toggle, dotnet, key) {
    if (!(root instanceof HTMLElement) || !(panel instanceof HTMLElement) || !dotnet || pickerHandlers.has(key)) {
        return;
    }

    for (const [otherKey, other] of pickerHandlers) {
        if (otherKey !== key) {
            void other.dotnet.invokeMethodAsync('OnDismissRequestedAsync', false);
        }
    }

    if (toggle instanceof HTMLElement) {
        returnTargets.set(key, toggle);
    }

    const onPointerDown = event => {
        if (event.target instanceof Node && !root.contains(event.target)) {
            void dotnet.invokeMethodAsync('OnDismissRequestedAsync', false);
        }
    };
    const onKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            void dotnet.invokeMethodAsync('OnDismissRequestedAsync', true);
            return;
        }
        if (pickerKeys.has(event.key) && event.target instanceof Element && event.target.closest('[role="grid"], [role="listbox"]')) {
            event.preventDefault();
        }
    };

    // The field moves with any scroll, the page's or a dialog's, and the panel changes height with
    // the month shown: it is placed again each time.
    const onMove = () => {
        if (panel.isConnected) {
            placePicker(root, panel);
        }
    };
    const resized = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(onMove);

    document.addEventListener('pointerdown', onPointerDown, true);
    root.addEventListener('keydown', onKeyDown);
    window.addEventListener('scroll', onMove, true);
    window.addEventListener('resize', onMove);
    resized?.observe(panel);
    pickerHandlers.set(key, { root, dotnet, onPointerDown, onKeyDown, onMove, resized });
    placePicker(root, panel);
    centrePickerLists(panel);
}

export function detachPicker(key, restore) {
    const state = pickerHandlers.get(key);
    if (state) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
        state.root.removeEventListener('keydown', state.onKeyDown);
        window.removeEventListener('scroll', state.onMove, true);
        window.removeEventListener('resize', state.onMove);
        state.resized?.disconnect();
        pickerHandlers.delete(key);
    }

    const target = returnTargets.get(key);
    returnTargets.delete(key);
    if (restore && target?.isConnected && !target.closest('[inert]')) {
        target.focus({ preventScroll: true });
    }
}

export function focusPickerItem(panel, selector) {
    const item = panel?.querySelector(selector);
    if (item instanceof HTMLElement) {
        item.focus({ preventScroll: false });
    }
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

const fieldsetToggles = new WeakMap();

// Blazor delivers no toggle event, so a collapsible OmniFieldset that reports its state hears the
// native one here and hands the new open state back. Only added when the host asked for it.
export function observeFieldsetToggle(details, dotnet) {
    if (!(details instanceof HTMLDetailsElement) || !dotnet || fieldsetToggles.has(details)) {
        return;
    }

    const onToggle = () => void dotnet.invokeMethodAsync('OmniFieldset.Toggled', details.open);
    details.addEventListener('toggle', onToggle);
    fieldsetToggles.set(details, onToggle);
}

export function disposeFieldsetToggle(details) {
    const onToggle = details ? fieldsetToggles.get(details) : undefined;
    if (onToggle) {
        details.removeEventListener('toggle', onToggle);
        fieldsetToggles.delete(details);
    }
}

// A floating sidebar closes on Escape wherever the focus is: after the toggle opened it, the focus
// stays on the toggle in the header, outside the panel, where a key handler on the panel never hears.
const escapeListeners = new Map();

export function attachEscape(owner, dotnet) {
    detachEscape(owner);
    const listener = event => {
        if (event.key === 'Escape' && !event.defaultPrevented) {
            dotnet.invokeMethodAsync('CloseFromEscapeAsync');
        }
    };
    document.addEventListener('keydown', listener);
    escapeListeners.set(owner, listener);
}

export function detachEscape(owner) {
    const listener = escapeListeners.get(owner);
    if (listener) {
        document.removeEventListener('keydown', listener);
        escapeListeners.delete(owner);
    }
}

// A pushing sidebar floats over the page on a phone, under the same 40rem threshold as the stylesheet:
// the sidebar is told the width class now and on every change.
const narrowWatchers = new Map();

export function watchNarrow(owner, dotnet) {
    unwatchNarrow(owner);
    const query = window.matchMedia('(max-width: 39.99rem)');
    const listener = () => dotnet.invokeMethodAsync('SetNarrowAsync', query.matches);
    query.addEventListener('change', listener);
    narrowWatchers.set(owner, { query, listener });
    listener();
}

export function unwatchNarrow(owner) {
    const watcher = narrowWatchers.get(owner);
    if (watcher) {
        watcher.query.removeEventListener('change', watcher.listener);
        narrowWatchers.delete(owner);
    }
}
