// The one placement engine of the floating surfaces of the package: menus, pickers, filter panels,
// popovers and tooltips. It turns the box of an anchor, the size of the floating box and the room of
// the window into the top-left corner of the box, so that every surface prefers the same side, keeps
// the same margin, flips the same way and follows the reading direction of its anchor.

// The room kept between a floating surface and the edges of the window.
export const viewportMargin = 8;

// The window as the page lays it out, without the scrollbars.
export function viewportSize() {
    return { width: document.documentElement.clientWidth, height: document.documentElement.clientHeight };
}

// Moves the start of a span of the given size back inside [floor, limit - margin]. The floor wins
// when the span is larger than the room, so its start stays reachable.
export function fitAxis(start, size, limit, margin = viewportMargin, floor = margin) {
    return Math.max(floor, Math.min(start, limit - margin - size));
}

/**
 * Places a floating box against an anchor and returns { left, top, side } (the corner of the box, in
 * window coordinates, and the side of the anchor it ended up on).
 *
 *  anchor   the box of the anchor { left, top, right, bottom }: a DOMRect, or a point (left = right,
 *           top = bottom) for a surface that opens at the pointer.
 *  size     { width, height } of the floating box.
 *  side     'bottom' (default) or 'top': the side tried first. When the box does not fit there it goes
 *           to the other side if it fits; when it fits on neither, it stays on the preferred side and
 *           is pulled back inside the window.
 *  align    'start' (default), 'end' or 'center': the edge of the anchor the box lines up with, in the
 *           reading direction: with rtl, start is the right edge.
 *  rtl      the reading direction of the anchor.
 *  flipInline  when the box would leave the window on the side it extends to, line it up with the
 *           opposite edge of the anchor instead (a menu at the pointer opens to its left past the
 *           right edge); otherwise the box is only pulled back inside.
 *  gap      the space left between the anchor and the box.
 *  margin   the room kept from the edges of the window.
 *  ceiling  the smallest top the box may take (default: margin), for a window whose top is covered by a
 *           sticky header.
 *  viewport { width, height } of the room (default: the window without its scrollbars).
 */
export function placeFloating({ anchor, size, side = 'bottom', align = 'start', rtl = false, flipInline = false, gap = 4, margin = viewportMargin, ceiling = margin, viewport = viewportSize() }) {
    // The edge of the anchor the box lines up with, as a side of the window.
    let edge = align === 'center' ? 'center' : ((align === 'start') !== rtl ? 'left' : 'right');
    const startAt = at => at === 'left' ? anchor.left : at === 'right' ? anchor.right - size.width : (anchor.left + anchor.right - size.width) / 2;
    if (flipInline && edge === 'left' && startAt('left') + size.width > viewport.width - margin) {
        edge = 'right';
    } else if (flipInline && edge === 'right' && startAt('right') < margin) {
        edge = 'left';
    }

    const room = viewport.height - margin;
    const below = anchor.bottom + gap;
    const above = anchor.top - gap - size.height;
    const fitsBelow = below + size.height <= room;
    const fitsAbove = above >= ceiling;
    const preferBottom = side !== 'top';
    const onBottom = preferBottom ? fitsBelow || !fitsAbove : !fitsAbove && fitsBelow;
    return {
        left: fitAxis(startAt(edge), size.width, viewport.width, margin),
        top: fitAxis(onBottom ? below : above, size.height, viewport.height, margin, ceiling),
        side: onBottom ? 'bottom' : 'top'
    };
}
