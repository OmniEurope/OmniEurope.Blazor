// State and helpers shared by the focus modules (omni-focus.js and its menus): the element each
// open surface gives the focus back to, keyed like the surface, the margin kept from the window
// edges, and what counts as a press outside.

export const returnTargets = new Map();

export const viewportMargin = 8;

export function rememberTarget(key) {
    if (!returnTargets.has(key) && document.activeElement instanceof HTMLElement) {
        returnTargets.set(key, document.activeElement);
    }
}

// A press inside an element marked data-omni-keep-open never counts as outside: it lets a panel of
// settings drive an open menu without closing it.
export function pressedOutside(event, ...inside) {
    const target = event.target;
    if (!(target instanceof Node) || inside.some(element => element?.contains(target))) {
        return false;
    }

    return !(target instanceof Element && target.closest('[data-omni-keep-open]'));
}
