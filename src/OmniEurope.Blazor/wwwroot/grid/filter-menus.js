// Part of omni-grid.js, which re-exports attachFilterMenus, closeFilterMenus and detachFilterMenus.

import { release } from './release.js';
const menuAttachments = new Map();

const popoverSelector = 'details[data-omni-popover]';

/**
 * Places a popover panel under its trigger. The panel is position: fixed, so the viewport that
 * scrolls the table cannot clip it; it is kept inside the window and flips above the trigger when
 * there is no room below. Custom properties only, never the style attribute.
 */
function placePopover(popover) {
    const trigger = popover.querySelector(':scope > summary');
    const panel = popover.querySelector(':scope > .omni-data-grid__popover-panel');
    if (!trigger || !panel) {
        return;
    }

    const anchor = trigger.getBoundingClientRect();
    popover.style.setProperty('--omni-popover-min', `${Math.round(anchor.width)}px`);
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    const margin = 8;
    const left = Math.max(margin, Math.min(anchor.left, window.innerWidth - width - margin));
    const below = anchor.bottom + 4;
    const top = below + height > window.innerHeight - margin && anchor.top - height - 4 > margin
        ? anchor.top - height - 4
        : below;
    popover.style.setProperty('--omni-popover-x', `${Math.round(left)}px`);
    popover.style.setProperty('--omni-popover-y', `${Math.round(top)}px`);
}

/** Places the suggestion list of a text filter under its input, for the same reason. */
function placeCombo(combo) {
    const input = combo.querySelector('.omni-combo__input');
    if (!input) {
        return;
    }

    const anchor = input.getBoundingClientRect();
    combo.style.setProperty('--omni-anchor-x', `${Math.round(anchor.left)}px`);
    combo.style.setProperty('--omni-anchor-y', `${Math.round(anchor.bottom + 2)}px`);
    combo.style.setProperty('--omni-anchor-w', `${Math.round(anchor.width)}px`);
}

/**
 * Behaviour of the grid's popovers: header filter menus, advanced filter panels and folded
 * checkable lists, all <details data-omni-popover>. A <details> only closes on its own summary, so a
 * click anywhere else or the Escape key is handled here, one popover open at a time; with
 * hideOnSelect a header menu also closes as soon as a value is picked. Opening and closing is not
 * state .NET needs to hear about, so all of it stays in the browser.
 */
export function attachFilterMenus(viewport, hideOnSelect) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    detachFilterMenus(viewport);

    // A popover nested in another (a checkable list inside a menu) keeps its parent open.
    const close = keep => {
        for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
            if (!keep || !popover.contains(keep)) {
                popover.open = false;
            }
        }
    };

    const onDocumentPointerDown = event => {
        const target = event.target instanceof Element ? event.target : null;
        close(target?.closest(popoverSelector) ? target : null);
    };

    const onKeyDown = event => {
        if (event.key === 'Escape' && viewport.querySelector(`${popoverSelector}[open]`)) {
            close(null);
        }
    };

    // toggle does not bubble: listened to in the capture phase, for every popover present or future.
    const onToggle = event => {
        const popover = event.target;
        if (popover instanceof HTMLDetailsElement && popover.matches(popoverSelector) && popover.open) {
            close(popover);
            placePopover(popover);
        }
    };

    const onFocusIn = event => {
        const combo = event.target instanceof Element ? event.target.closest('.omni-combo') : null;
        if (combo && viewport.contains(combo)) {
            placeCombo(combo);
        }
    };

    // The trigger moves with any scroll, the page's or the grid's own: the open panels follow it.
    const onMove = () => {
        for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
            placePopover(popover);
        }

        const combo = document.activeElement instanceof Element ? document.activeElement.closest('.omni-combo') : null;
        if (combo && viewport.contains(combo)) {
            placeCombo(combo);
        }
    };

    // Native controls report their pick through change. The combo suggestion list is Blazor markup
    // that disappears on selection, so its own pick is reported from .NET through closeFilterMenus.
    const onPicked = event => {
        const target = event.target instanceof Element ? event.target : null;
        // A checkable list takes several ticks, an advanced filter waits for its apply button, and an
        // operator is only the first half of a condition whose value is still to be typed.
        if (!target || target.matches('.omni-data-grid__filter-operator, .omni-data-grid__filter-logical')
            || target.closest('.omni-multi-select, .omni-data-grid__multi') || target.closest('.omni-data-grid__filter-editor')?.querySelector('.omni-data-grid__filter-apply')) {
            return;
        }

        const menu = target.closest('details.omni-data-grid__filter-menu');
        if (menu) {
            menu.open = false;
        }
    };

    // Applying or clearing from a panel is the end of the task the panel was opened for.
    const onClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const action = target?.closest('.omni-data-grid__filter-apply, .omni-data-grid__filter-clear');
        const popover = action?.closest(popoverSelector);
        if (popover) {
            popover.open = false;
        }
    };

    document.addEventListener('pointerdown', onDocumentPointerDown, true);
    document.addEventListener('keydown', onKeyDown, true);
    viewport.addEventListener('toggle', onToggle, true);
    viewport.addEventListener('focusin', onFocusIn);
    // Typing also opens the list: placed again on input, whatever the focus events did.
    viewport.addEventListener('input', onFocusIn);
    viewport.addEventListener('click', onClick);
    window.addEventListener('scroll', onMove, true);
    window.addEventListener('resize', onMove);
    if (hideOnSelect) {
        viewport.addEventListener('change', onPicked);
    }

    menuAttachments.set(viewport, {
        dispose: () => {
            document.removeEventListener('pointerdown', onDocumentPointerDown, true);
            document.removeEventListener('keydown', onKeyDown, true);
            viewport.removeEventListener('toggle', onToggle, true);
            viewport.removeEventListener('focusin', onFocusIn);
            viewport.removeEventListener('input', onFocusIn);
            viewport.removeEventListener('click', onClick);
            window.removeEventListener('scroll', onMove, true);
            window.removeEventListener('resize', onMove);
            viewport.removeEventListener('change', onPicked);
        }
    });
}

/** Closes every open filter popover of a grid, whatever put them there. */
export function closeFilterMenus(viewport) {
    if (!(viewport instanceof HTMLElement)) {
        return;
    }

    for (const popover of viewport.querySelectorAll(`${popoverSelector}[open]`)) {
        popover.open = false;
    }
}

export function detachFilterMenus(viewport) {
    release(menuAttachments, viewport);
}
