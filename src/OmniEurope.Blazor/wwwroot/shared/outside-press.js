// The one "press outside" listener of the package: a surface that closes when the pointer goes down
// anywhere but on itself (a popover, a picker, a menu, a disclosure, a filter panel, a context menu).

/**
 * Calls onPress(event) for every press (pointerdown, captured so that a handler which stops the event
 * still lets it through) that lands outside what is inside, and returns the function that stops it.
 *
 *  inside   an element, an array of elements, or a function (event) returning one or an array; nulls
 *           are skipped (null alone: nothing is inside, every press counts). A function lets the inside change between presses (a menu found by id) or
 *           depend on the press itself (the trigger, but only for a click that toggles).
 *  onPress  not called for a press inside, nor for one inside an element marked data-omni-keep-open,
 *           which lets a panel of settings drive an open surface without closing it.
 */
export function onPressOutside(inside, onPress) {
    const listener = event => {
        const target = event.target;
        if (!(target instanceof Node)) {
            return;
        }

        const elements = [typeof inside === 'function' ? inside(event) : inside].flat();
        if (elements.some(element => element?.contains(target))
            || (target instanceof Element && target.closest('[data-omni-keep-open]'))) {
            return;
        }

        onPress(event);
    };
    document.addEventListener('pointerdown', listener, true);
    return () => document.removeEventListener('pointerdown', listener, true);
}
