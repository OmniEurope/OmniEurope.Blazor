// Part of omni-focus.js, which re-exports attachFloatingList and detachFloatingList: the open list of an
// autocomplete. It is fixed to the window and placed here, under its field, so whatever scrolls or clips
// around the field (a dialog, a card) can neither cut it nor grow a scrollbar for it. It goes above the
// field when there is no room below. Custom properties on the list only, which leaves the page with it.
import { placeFloating } from '../shared/placement.js';

const floatingLists = new Map();

function placeList(root, list) {
    const anchor = (root.querySelector('input') ?? root).getBoundingClientRect();
    list.style.setProperty('--omni-autocomplete-width', `${Math.round(anchor.width)}px`);
    const { left, top } = placeFloating({
        anchor,
        size: { width: anchor.width, height: list.offsetHeight },
        rtl: getComputedStyle(root).direction === 'rtl'
    });
    const set = (x, y) => {
        list.style.setProperty('--omni-autocomplete-x', `${Math.round(x)}px`);
        list.style.setProperty('--omni-autocomplete-y', `${Math.round(y)}px`);
    };
    set(left, top);
    // An ancestor with a transform, a filter or a containment is the origin of position: fixed instead
    // of the window: the list is then drawn off by that origin, measured here and taken back.
    const drawn = list.getBoundingClientRect();
    if (Math.abs(drawn.left - left) > 1 || Math.abs(drawn.top - top) > 1) {
        set(2 * left - drawn.left, 2 * top - drawn.top);
    }
}

export function attachFloatingList(root, list, key) {
    if (!(root instanceof HTMLElement) || !(list instanceof HTMLElement) || floatingLists.has(key)) {
        return;
    }

    // The field moves with any scroll, the page's or a dialog's, and the list changes height with its results.
    const onMove = () => {
        if (list.isConnected) {
            placeList(root, list);
        }
    };
    const resized = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(onMove);
    list.classList.add('omni-autocomplete__results--floating');
    onMove();
    resized?.observe(list);
    window.addEventListener('scroll', onMove, true);
    window.addEventListener('resize', onMove);
    floatingLists.set(key, { onMove, resized });
}

export function detachFloatingList(key) {
    const state = floatingLists.get(key);
    if (!state) {
        return;
    }

    state.resized?.disconnect();
    window.removeEventListener('scroll', state.onMove, true);
    window.removeEventListener('resize', state.onMove);
    floatingLists.delete(key);
}
