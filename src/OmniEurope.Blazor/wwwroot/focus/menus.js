// Part of omni-focus.js, which re-exports openMenu, closeMenu and moveMenuFocus.
import { returnTargets, rememberTarget, viewportMargin, pressedOutside } from './shared.js';

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
        menu.addEventListener('keydown', state.onKeyDown);
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
