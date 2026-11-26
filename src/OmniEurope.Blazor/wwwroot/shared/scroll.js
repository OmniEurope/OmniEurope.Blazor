// Scroll helpers shared by the modules that hand a wheel turn over to an area (the tab panels of
// omni-focus.js and the rows of omni-grid.js).

// Whether a wheel turn is one an area may take: nobody handled it, no Ctrl (the zoom) nor Shift (the
// horizontal scroll), and it turns more vertically than horizontally.
export function isPlainVerticalWheel(event) {
    return !event.defaultPrevented && !event.ctrlKey && !event.shiftKey && Math.abs(event.deltaY) > Math.abs(event.deltaX);
}

// Whether the element scrolls vertically and can still move in the direction of deltaY.
export function canScrollVertically(element, deltaY) {
    if (!/(auto|scroll)/.test(getComputedStyle(element).overflowY) || element.scrollHeight <= element.clientHeight) {
        return false;
    }

    return deltaY < 0
        ? element.scrollTop > 0
        : element.scrollTop + element.clientHeight < element.scrollHeight - 1;
}
