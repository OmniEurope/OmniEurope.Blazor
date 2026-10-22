// Part of omni-focus.js, which re-exports attachPicker, detachPicker and focusPickerItem: the date
// and time pickers.
import { returnTargets, notify } from './shared.js';
import { placeFloating } from '../shared/placement.js';
import { onPressOutside } from '../shared/outside-press.js';

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
    const { left, top } = placeFloating({
        anchor: (root.querySelector('.omni-date__field') ?? root).getBoundingClientRect(),
        size: { width: panel.offsetWidth, height: panel.offsetHeight },
        rtl: getComputedStyle(root).direction === 'rtl',
        gap: 6
    });
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
            notify(other.dotnet, 'OnDismissRequestedAsync', false);
        }
    }

    if (toggle instanceof HTMLElement) {
        returnTargets.set(key, toggle);
    }

    const onKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            notify(dotnet, 'OnDismissRequestedAsync', true);
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

    const stopOutside = onPressOutside(root, () => notify(dotnet, 'OnDismissRequestedAsync', false));
    root.addEventListener('keydown', onKeyDown);
    window.addEventListener('scroll', onMove, true);
    window.addEventListener('resize', onMove);
    resized?.observe(panel);
    pickerHandlers.set(key, { root, dotnet, stopOutside, onKeyDown, onMove, resized });
    placePicker(root, panel);
    centrePickerLists(panel);
}

export function detachPicker(key, restore) {
    const state = pickerHandlers.get(key);
    if (state) {
        state.stopOutside();
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
