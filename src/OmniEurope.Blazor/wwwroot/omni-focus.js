const returnTargets = new Map();
const dialogHandlers = new Map();
const tabHandlers = new WeakMap();
const tabOverflow = new WeakMap();

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

function rememberTarget(key) {
    if (!returnTargets.has(key) && document.activeElement instanceof HTMLElement) {
        returnTargets.set(key, document.activeElement);
    }
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
const viewportMargin = 8;

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

    document.addEventListener('pointerdown', onPointerDown, true);
    root.addEventListener('keydown', onKeyDown);
    pickerHandlers.set(key, { root, dotnet, onPointerDown, onKeyDown });
    centrePickerLists(panel);
}

export function detachPicker(key, restore) {
    const state = pickerHandlers.get(key);
    if (state) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
        state.root.removeEventListener('keydown', state.onKeyDown);
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

// ---- menus ------------------------------------------------------------------------------------
// One engine for every menu of the package: the overflow ("⋮"), context, split button and profile
// menus, and the context menu of the HTML editor. It places the open menu, focuses its first or last
// item, reports a press outside and gives the focus back when the menu closes. The keys of the open
// menu are routed by .NET (OmniMenuController) and move the focus through moveMenuFocus; the script
// only keeps the page from scrolling on them, and on Tab puts the focus back on the trigger at once
// so the browser's own move goes on from there, as it does from a native menu.
//
// The menu is found by id: the overlay portal may render it far from its component, and a reference
// the portal never captured reached this script as an empty object. It lives in the portal, so no
// scrolling area or containment of the page clips it or becomes the containing block of its fixed
// position. Placements: 'start' and 'end' align the menu under its anchor by that edge, flipped in a
// right-to-left page, and above the anchor when there is no room below; 'pointer' opens at (x, y),
// towards the other side of the pointer past an edge, and under the anchor when the keyboard opened
// it. The menu is moved back inside the window either way. A second call while open only moves it.
const menus = new Map();
const menuNavigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']);

function menuItems(menu) {
    return Array.from(menu?.querySelectorAll('[role="menuitem"]:not([disabled]):not([aria-disabled="true"])') ?? []);
}

// The floating surface: the menu itself, or the panel around it when a header sits above the list.
function menuSurface(menu) {
    return menu?.closest('[data-omni-menu-surface]') ?? menu;
}

// The control that opened the menu: the anchor itself, or the button in it that announces the menu.
function menuTrigger(anchor) {
    if (!(anchor instanceof HTMLElement)) {
        return null;
    }

    return anchor.matches('[aria-haspopup]') ? anchor : anchor.querySelector('[aria-haspopup]') ?? anchor;
}

function placeMenu(surface, anchor, placement, x, y) {
    const margin = viewportMargin;
    const gap = 4;
    const width = document.documentElement.clientWidth;
    const height = document.documentElement.clientHeight;
    surface.setAttribute('data-omni-placed', '');
    const box = surface.getBoundingClientRect();
    let left;
    let top;
    if (placement === 'pointer' && typeof x === 'number' && typeof y === 'number') {
        left = x + box.width > width - margin ? x - box.width : x;
        top = y + box.height > height - margin ? y - box.height : y;
    } else {
        const rect = anchor instanceof HTMLElement ? anchor.getBoundingClientRect() : null;
        if (!rect) {
            return;
        }

        const rtl = getComputedStyle(anchor).direction === 'rtl';
        left = (placement === 'end') !== rtl ? rect.right - box.width : rect.left;
        top = rect.bottom + gap;
        if (top + box.height > height - margin) {
            const above = rect.top - gap - box.height;
            top = above >= margin ? above : height - margin - box.height;
        }
    }

    left = Math.max(margin, Math.min(left, width - margin - box.width));
    top = Math.max(margin, Math.min(top, height - margin - box.height));
    surface.style.setProperty('--omni-menu-x', `${Math.round(left)}px`);
    surface.style.setProperty('--omni-menu-y', `${Math.round(top)}px`);
}

function showMenu(key, options) {
    let state = menus.get(key);
    if (!state) {
        rememberTarget(key);
        state = { menu: null, attempts: 0, options };
        state.onPointerDown = event => {
            const { anchor, placement } = state.options;
            // A press on the trigger is left to its own click, which toggles: dismissing on that press
            // as well made the click open the menu again. A second right-click on the region of a
            // context menu moves the menu instead of closing it.
            if (anchor?.contains(event.target) && (placement !== 'pointer' || event.button === 2)) {
                return;
            }

            if (state.options.closeOnOutsideClick !== false && pressedOutside(event, menuSurface(state.menu))) {
                state.options.dismiss(false);
            }
        };
        state.onPlace = () => {
            const { anchor, placement, x, y } = state.options;
            if (state.menu?.isConnected && !(placement === 'pointer' && typeof x === 'number')) {
                placeMenu(menuSurface(state.menu), anchor, placement, x, y);
            }
        };
        state.onKeyDown = event => {
            if (menuNavigationKeys.has(event.key)) {
                event.preventDefault();
            } else if (event.key === 'Tab') {
                returnTargets.delete(key);
                menuTrigger(state.options.anchor)?.focus({ preventScroll: true });
            }
        };
        document.addEventListener('pointerdown', state.onPointerDown, true);
        window.addEventListener('resize', state.onPlace);
        window.addEventListener('scroll', state.onPlace, true);
        menus.set(key, state);
    }

    state.options = options;
    const menu = document.getElementById(options.menuId);
    if (!menu) {
        if (state.attempts++ < 10) {
            setTimeout(() => menus.get(key) === state && showMenu(key, state.options), 16);
        }

        return;
    }

    state.attempts = 0;
    if (state.menu !== menu) {
        state.menu?.removeEventListener('keydown', state.onKeyDown);
        if (options.keys !== false) {
            menu.addEventListener('keydown', state.onKeyDown);
        }

        state.menu = menu;
    }

    placeMenu(menuSurface(menu), options.anchor, options.placement, options.x, options.y);
    const items = menuItems(menu);
    ((options.focusLast ? items.at(-1) : items[0]) ?? menu).focus({ preventScroll: true });
}

export function openMenu(menuId, key, anchor, placement, x, y, focusLast, closeOnOutsideClick, dotnet) {
    showMenu(key, {
        menuId,
        anchor,
        placement,
        x,
        y,
        focusLast,
        closeOnOutsideClick,
        dismiss: restore => void dotnet.invokeMethodAsync('OmniMenu.Dismiss', restore)
    });
}

// The focus goes back to the trigger with restore (Escape, an item chosen, the trigger pressed
// again), or when it was still in the menu, which is about to leave the page; a press outside that
// moved it to another control leaves it there. A menu opened at the pointer gives it back to where it
// was before, a menu with a trigger to that trigger (a click does not focus a button everywhere).
export function closeMenu(key, restore) {
    const state = menus.get(key);
    if (state) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
        window.removeEventListener('resize', state.onPlace);
        window.removeEventListener('scroll', state.onPlace, true);
        state.menu?.removeEventListener('keydown', state.onKeyDown);
        menus.delete(key);
    }

    const before = returnTargets.get(key);
    returnTargets.delete(key);
    const active = document.activeElement;
    const surface = menuSurface(state?.menu);
    const focusLeft = active instanceof HTMLElement && active !== document.body && active.isConnected && !surface?.contains(active);
    if (!restore && focusLeft) {
        return;
    }

    const trigger = menuTrigger(state?.options.anchor);
    const target = state?.options.placement === 'pointer' ? before ?? trigger : trigger ?? before;
    if (target?.isConnected && !target.closest('[inert]')) {
        target.focus({ preventScroll: true });
    }
}

export function moveMenuFocus(menuOrId, key) {
    const menu = typeof menuOrId === 'string' ? document.getElementById(menuOrId) : menuOrId;
    const items = menuItems(menu);
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

// The context menu of OmniHtmlEditor, which draws its own list and routes its own keys: the same
// engine, opened at the pointer, its press outside reported without an argument.
export function openContextMenu(popupId, key, trigger, x, y, dotnet) {
    showMenu(key, {
        menuId: popupId,
        anchor: trigger,
        placement: 'pointer',
        x,
        y,
        focusLast: false,
        closeOnOutsideClick: true,
        keys: false,
        dismiss: () => void dotnet.invokeMethodAsync('OmniContextMenu.Dismiss')
    });
}

export function moveContextMenuFocus(popupId, key) {
    moveMenuFocus(popupId, key);
}

export function closeContextMenu(key) {
    closeMenu(key, false);
}

const tabsWheelScopes = new Map();

function scrollsVertically(element, deltaY) {
    if (!/(auto|scroll)/.test(getComputedStyle(element).overflowY) || element.scrollHeight <= element.clientHeight) {
        return false;
    }

    return deltaY < 0
        ? element.scrollTop > 0
        : element.scrollTop + element.clientHeight < element.scrollHeight - 1;
}

/**
 * OmniTabs WheelScrollScope: a vertical wheel turn over the named ancestor, outside the selected
 * panel, scrolls that panel. Anything under the pointer that can still scroll that way keeps the
 * wheel, Shift and Ctrl are never taken, and a panel that cannot scroll further lets the event go.
 */
export function attachTabsWheelScope(tabs, selector) {
    if (!(tabs instanceof HTMLElement)) {
        return;
    }

    detachTabsWheelScope(tabs);
    const scope = typeof selector === 'string' && selector ? tabs.closest(selector) : null;
    if (!scope) {
        return;
    }

    const onWheel = event => {
        if (event.defaultPrevented || event.ctrlKey || event.shiftKey || Math.abs(event.deltaY) <= Math.abs(event.deltaX)) {
            return;
        }

        const panel = [...tabs.children].find(child => child.classList.contains('omni-tabs__panel') && !child.hidden);
        if (!panel || !tabs.isConnected || panel.getClientRects().length === 0) {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        if (target && panel.contains(target)) {
            return;
        }

        for (let node = target; node && node !== scope; node = node.parentElement) {
            if (scrollsVertically(node, event.deltaY)) {
                return;
            }
        }

        if (!scrollsVertically(panel, event.deltaY)) {
            return;
        }

        const unit = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? panel.clientHeight : 1;
        event.preventDefault();
        panel.scrollTop += event.deltaY * unit;
    };

    scope.addEventListener('wheel', onWheel, { passive: false });
    tabsWheelScopes.set(tabs, () => scope.removeEventListener('wheel', onWheel));
}

export function detachTabsWheelScope(tabs) {
    const dispose = tabsWheelScopes.get(tabs);
    if (dispose) {
        dispose();
        tabsWheelScopes.delete(tabs);
    }
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
