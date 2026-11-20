// Part of omni-focus.js, which re-exports openMenu, closeMenu, moveMenuFocus, openSubMenu and leaveSubMenu.
import { returnTargets, rememberTarget } from './shared.js';
import { notify } from '../shared/notify.js';
import { placeBeside, placeFloating } from '../shared/placement.js';
import { onPressOutside } from '../shared/outside-press.js';

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
//
// A menu may hold submenus (OmniSubMenu): a nested role="menu" inside the menu, owned by an entry that
// carries aria-haspopup and aria-expanded. The keys then act on one level, the one the focus is in. The
// open state of an entry belongs to .NET, so the keys that change it (the arrow toward the inline end,
// the arrow back, Escape in a submenu) press the entry, which toggles it, and the script moves the focus.
// A menu that holds submenus scrolls like any other (a long one scrolls inside itself): a submenu is a
// manual popover, drawn in the top layer so the scrolling list never clips it, and a scroll of the list
// that holds its entry closes it (.NET does, told by the scroll watch below).
const menus = new Map();
const scrollWatches = new Map();
const menuNavigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']);

// The enabled items of one level: those of a submenu inside the menu belong to that submenu.
function menuItems(menu) {
    return Array.from(menu?.querySelectorAll('[role="menuitem"]:not([disabled]):not([aria-disabled="true"])') ?? [])
        .filter(item => {
            const level = item.closest('[role="menu"]');
            return level === menu || !menu.contains(level);
        });
}

// A submenu is its own floating surface, never the panel around the menu it sits in.
function isSubMenu(level) {
    return level?.classList.contains('omni-menu__submenu') ?? false;
}

// The level of the menu the focus is in: a submenu inside the menu, else the menu itself.
function focusedLevel(menu) {
    const level = document.activeElement instanceof HTMLElement ? document.activeElement.closest('[role="menu"]') : null;
    return level && menu?.contains(level) ? level : menu;
}

function levelSurface(level) {
    return isSubMenu(level) ? level : menuSurface(level);
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

// Scrolls a menu taller than the window (it scrolls inside itself) so the focused row is seen.
function revealItem(surface, item) {
    if (!item || surface.scrollHeight <= surface.clientHeight) {
        return;
    }

    const box = surface.getBoundingClientRect();
    const row = item.getBoundingClientRect();
    if (row.top < box.top) {
        surface.scrollTop -= box.top - row.top;
    } else if (row.bottom > box.bottom) {
        surface.scrollTop += row.bottom - box.bottom;
    }
}

function placeMenu(surface, anchor, placement, x, y) {
    surface.setAttribute('data-omni-placed', '');
    const size = surface.getBoundingClientRect();
    const atPointer = placement === 'pointer' && typeof x === 'number' && typeof y === 'number';
    if (!atPointer && !(anchor instanceof HTMLElement)) {
        return;
    }

    // At the pointer the menu is anchored to a point and turns to the other side of it past an edge;
    // under an anchor it aligns with the anchor's edge and is only kept inside the window.
    const { left, top } = placeFloating({
        anchor: atPointer ? { left: x, right: x, top: y, bottom: y } : anchor.getBoundingClientRect(),
        size,
        align: placement === 'end' ? 'end' : 'start',
        rtl: !atPointer && getComputedStyle(anchor).direction === 'rtl',
        gap: atPointer ? 0 : 4,
        flipInline: atPointer
    });
    surface.style.setProperty('--omni-menu-x', `${Math.round(left)}px`);
    surface.style.setProperty('--omni-menu-y', `${Math.round(top)}px`);
}

// Places a submenu beside its entry: at the inline end of the menu that holds the entry (the other side
// when there is no room), its first row level with the entry, kept inside the window. The submenu is
// first set at the origin so that its size is measured without the room it would otherwise lack; it may
// sit in a box that is the containing block of its fixed position (a menu with a backdrop filter), so
// the corner is measured once set and corrected by what is left.
function placeSubMenu(parent, submenu) {
    submenu.setAttribute('data-omni-placed', '');
    submenu.style.setProperty('--omni-menu-x', '0px');
    submenu.style.setProperty('--omni-menu-y', '0px');
    const level = parent.closest('[role="menu"]');
    const holder = (level ? levelSurface(level) : parent).getBoundingClientRect();
    const row = parent.getBoundingClientRect();
    const size = submenu.getBoundingClientRect();
    const computed = getComputedStyle(submenu);
    const inset = (parseFloat(computed.paddingTop) || 0) + (parseFloat(computed.borderTopWidth) || 0);
    const { left, top } = placeBeside({
        anchor: { left: holder.left, right: holder.right, top: row.top, bottom: row.bottom },
        size,
        rtl: getComputedStyle(parent).direction === 'rtl',
        offset: -inset
    });
    const set = (x, y) => {
        submenu.style.setProperty('--omni-menu-x', `${Math.round(x)}px`);
        submenu.style.setProperty('--omni-menu-y', `${Math.round(y)}px`);
    };
    set(left, top);
    const placed = submenu.getBoundingClientRect();
    const dx = Math.round(left - placed.left);
    const dy = Math.round(top - placed.top);
    if (dx !== 0 || dy !== 0) {
        set(left + dx, top + dy);
    }
}

// Places again every submenu open in the menu, the outer ones first.
function placeOpenSubMenus(menu) {
    for (const submenu of menu.querySelectorAll('.omni-menu__submenu[data-omni-placed]')) {
        const parent = document.getElementById(submenu.getAttribute('aria-labelledby') ?? '');
        if (parent) {
            placeSubMenu(parent, submenu);
        }
    }
}

function focusFirstItem(level) {
    const first = menuItems(level)[0];
    (first ?? level).focus({ preventScroll: true });
    revealItem(levelSurface(level), first);
}

// The keys that open and close a level. Returns whether the key was handled (the page is then kept
// from scrolling and the key from reaching .NET, whose own handler would close the whole menu on
// Escape): the arrow toward the inline end on an entry that opens a submenu opens it and focuses its
// first item, the arrow back or Escape inside a submenu closes that submenu and focuses its entry.
export function handleSubMenuKey(event, menu) {
    const target = event.target;
    if (!(target instanceof HTMLElement) || !menu) {
        return false;
    }

    const level = target.closest('[role="menu"]');
    if (!level || !menu.contains(level)) {
        return false;
    }

    const rtl = getComputedStyle(level).direction === 'rtl';
    const opens = event.key === (rtl ? 'ArrowLeft' : 'ArrowRight');
    const closes = event.key === (rtl ? 'ArrowRight' : 'ArrowLeft') || event.key === 'Escape';
    if (opens && target.matches('[role="menuitem"][aria-haspopup]')) {
        event.preventDefault();
        event.stopPropagation();
        const submenu = document.getElementById(target.getAttribute('aria-controls') ?? '');
        if (target.getAttribute('aria-expanded') === 'true') {
            if (submenu) {
                focusFirstItem(submenu);
            }
        } else {
            // A click with no pointer behind it (detail 0): .NET opens the submenu and focuses its first item.
            target.click();
        }

        return true;
    }

    if (closes && level !== menu) {
        event.preventDefault();
        event.stopPropagation();
        const parent = document.getElementById(level.getAttribute('aria-labelledby') ?? '');
        parent?.focus({ preventScroll: true });
        if (parent?.getAttribute('aria-expanded') === 'true') {
            parent.click();
        }

        return true;
    }

    return false;
}

function showMenu(key, options) {
    let state = menus.get(key);
    if (!state) {
        rememberTarget(key);
        state = { menu: null, attempts: 0, options };
        // A press on the trigger is left to its own click, which toggles: dismissing on that press
        // as well made the click open the menu again. A second right-click on the region of a
        // context menu moves the menu instead of closing it.
        const inside = event => {
            const { anchor, placement } = state.options;
            return [menuSurface(state.menu), placement !== 'pointer' || event.button === 2 ? anchor : null];
        };
        state.stopOutside = onPressOutside(inside, () => {
            if (state.options.closeOnOutsideClick !== false) {
                state.options.dismiss(false);
            }
        });
        state.onPlace = () => {
            const { anchor, placement, x, y } = state.options;
            if (state.menu?.isConnected && !(placement === 'pointer' && typeof x === 'number')) {
                placeMenu(menuSurface(state.menu), anchor, placement, x, y);
            }

            if (state.menu?.isConnected) {
                placeOpenSubMenus(state.menu);
            }
        };
        state.onKeyDown = event => {
            if (handleSubMenuKey(event, state.menu)) {
                return;
            }

            if (menuNavigationKeys.has(event.key)) {
                event.preventDefault();
            } else if (event.key === 'Tab') {
                returnTargets.delete(key);
                menuTrigger(state.options.anchor)?.focus({ preventScroll: true });
            }
        };
        window.addEventListener('resize', state.onPlace);
        window.addEventListener('scroll', state.onPlace, true);
        menus.set(key, state);
    }

    state.options = options;
    const menu = document.getElementById(options.menuId);
    if (!menu) {
        if (state.attempts++ < 10) {
            setTimeout(() => menus.get(key) === state && showMenu(key, state.options), 16);
        } else {
            // The menu never reached the page: a menu is invisible until it is placed, so .NET must not
            // keep it open (and its trigger announcing it) for a menu nobody can see. The script lets go
            // of it the way a close does, then reports the dismissal.
            closeMenu(key, false);
            state.options.dismiss(false);
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
    const first = options.focusLast ? items.at(-1) : items[0];
    (first ?? menu).focus({ preventScroll: true });
    revealItem(menuSurface(menu), first);
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
        dismiss: restore => notify(dotnet, 'OmniMenu.Dismiss', restore)
    });
}

// The focus goes back to the trigger with restore (Escape, an item chosen, the trigger pressed
// again), or when it was still in the menu, which is about to leave the page; a press outside that
// moved it to another control leaves it there. A menu opened at the pointer gives it back to where it
// was before, a menu with a trigger to that trigger (a click does not focus a button everywhere).
export function closeMenu(key, restore) {
    const state = menus.get(key);
    if (state) {
        state.stopOutside();
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

// Puts the submenu in the top layer, out of reach of the clipping of any scrolling ancestor. A browser
// without popovers keeps it fixed in place, as before.
function liftSubMenu(submenu) {
    if (typeof submenu.showPopover !== 'function') {
        return;
    }

    try {
        submenu.showPopover();
    } catch {
        // Not connected or already shown: it stays where it is.
    }
}

// Stops listening to the scroll of the list that holds a submenu's entry, and forgets stale watches
// (the lists of menus that left the page without telling).
function unwatchScroll(menuId) {
    const watch = scrollWatches.get(menuId);
    if (watch) {
        watch.targets.forEach(target => target.removeEventListener('scroll', watch.onScroll));
        scrollWatches.delete(menuId);
    }
}

// Tells .NET when the list that holds the entry of a submenu scrolls: the submenu, drawn beside its
// entry, no longer faces it and closes. A scroll event does not bubble, so each scrolling box of the list
// (the list, and the panel around it when a header sits above) is listened to itself, passively; the
// scroll of a submenu nested inside it is another event and does not count. Told once: the close that
// follows removes the watch.
function watchScroll(parent, menuId, dotnet) {
    unwatchScroll(menuId);
    for (const [id, watch] of scrollWatches) {
        if (!watch.targets.every(target => target.isConnected)) {
            unwatchScroll(id);
        }
    }

    const level = parent.closest('[role="menu"]');
    if (!level || !dotnet) {
        return;
    }

    const targets = [...new Set([levelSurface(level), level])];
    const onScroll = event => {
        if (targets.includes(event.target)) {
            unwatchScroll(menuId);
            notify(dotnet, 'OmniSubMenu.Scrolled');
        }
    };
    targets.forEach(target => target.addEventListener('scroll', onScroll, { passive: true }));
    scrollWatches.set(menuId, { targets, onScroll });
}

// Opens a submenu the render just drew: lifted into the top layer, placed beside its entry, its first item
// focused when the keyboard (or a click with no pointer) asked for it, never when the pointer did. The
// scroll of the list that holds the entry is reported to dotnet, which closes the submenu.
export function openSubMenu(parentId, menuId, focusFirst, dotnet) {
    const parent = document.getElementById(parentId);
    const submenu = document.getElementById(menuId);
    if (!parent || !submenu) {
        return;
    }

    liftSubMenu(submenu);
    placeSubMenu(parent, submenu);
    watchScroll(parent, menuId, dotnet);
    if (focusFirst) {
        focusFirstItem(submenu);
    }
}

// A submenu is about to close: the focus it holds goes back to its entry, not to the page.
export function leaveSubMenu(menuId, parentId) {
    unwatchScroll(menuId);
    const active = document.activeElement;
    if (active instanceof HTMLElement && document.getElementById(menuId)?.contains(active)) {
        document.getElementById(parentId)?.focus({ preventScroll: true });
    }
}

// The keys move through the level the focus is in, a submenu or the menu itself, wrapping at its ends.
export function moveMenuFocus(menuOrId, key) {
    const root = typeof menuOrId === 'string' ? document.getElementById(menuOrId) : menuOrId;
    const menu = focusedLevel(root);
    const items = menuItems(menu);
    if (items.length === 0) {
        menu?.focus();
        return;
    }

    // The focus may be on no item of the level (a press on the padding of the menu or on a disabled row puts
    // it on the menu itself): ArrowDown then goes to the first item and ArrowUp to the last, as from the trigger.
    const current = items.indexOf(document.activeElement);
    let next = Math.max(0, current);
    if (key === 'ArrowDown') next = (current + 1) % items.length;
    if (key === 'ArrowUp') next = current < 0 ? items.length - 1 : (current - 1 + items.length) % items.length;
    if (key === 'Home') next = 0;
    if (key === 'End') next = items.length - 1;
    items[next].focus({ preventScroll: true });
    revealItem(levelSurface(menu), items[next]);
}
