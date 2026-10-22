// Moving a selection with the mouse inside the visual surface of OmniHtmlEditor.
//
// The drop handler always cancels the browser's own drop (the content has to be sanitised in .NET before
// it is inserted), so the browser never removes the dragged source and a move would turn into a copy.
// This module remembers the selection a drag starts from, tells where the content lands, and lets the
// caller take the source away once the sanitised content is ready to be inserted.
import { caretFromPoint } from './model.js';

// The selection a drag starts from, when it is a non-collapsed one inside the surface.
export function startDrag(state) {
    const selection = document.getSelection();
    state.drag = null;
    if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
        return;
    }

    const range = selection.getRangeAt(0);
    if (state.surface.contains(range.commonAncestorContainer)) {
        state.drag = range.cloneRange();
    }
}

export function endDrag(state) {
    state.drag = null;
}

// Where a drop lands. The drop point becomes the selection, so the insertion happens there and not at the
// caret the surface had before the drag. Returns the range to remove after a move (null for a copy, for a
// drop from outside the surface, and for a drop that cannot be placed), or false when the drop falls
// inside the dragged selection itself and must change nothing.
export function placeDrop(state, event) {
    const source = state.drag;
    state.drag = null;
    const point = caretFromPoint(event.clientX, event.clientY);
    if (!point || !state.surface.contains(point.startContainer)) {
        return null;
    }

    if (source && source.comparePoint(point.startContainer, point.startOffset) === 0) {
        return false;
    }

    const selection = document.getSelection();
    selection.removeAllRanges();
    selection.addRange(point);
    return source && event.dataTransfer?.dropEffect !== 'copy' ? source : null;
}
