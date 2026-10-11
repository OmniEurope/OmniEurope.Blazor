// Part of omni-grid.js, which re-exports attachReorder, detachReorder and focusMoveControl: the drag
// that moves a column by its header, and the focus kept on a column moved from the column chooser.
import { notifyDotNet } from './notify.js';
import { release } from './release.js';

const reorderAttachments = new Map();

// Distance the pointer travels before a press on a header becomes a drag: below it, the press is a
// click and sorts the column as before.
const dragThreshold = 6;

// The parts of a header that keep their own pointer gestures: the resize handle, the filter menu.
const ownGestures = '[data-omni-resize], .omni-data-grid__popover, input, select, textarea';

const marks = ['omni-data-grid__drop--before', 'omni-data-grid__drop--after', 'omni-data-grid__dragged'];

/**
 * Drag-to-reorder on the column headers that carry data-omni-reorder ("frozen" or "free"): a header
 * only lands among the headers of its own group, a line on the inline start or end edge of the header
 * under the pointer marks where. The gesture stays in the browser, through classes only (no style
 * attribute, the strict CSP holds); .NET is told once, on release, and renders the new order. Escape
 * cancels. Delegation from the viewport means headers added by a later render need no re-attachment.
 */
export function attachReorder(viewport, reference) {
    if (!(viewport instanceof HTMLElement) || !reference) {
        return;
    }

    detachReorder(viewport);

    let live = true;
    let drag = null;

    const headerUnder = target => target instanceof Element
        ? target.closest('thead th[data-omni-reorder]')
        : null;

    const clearMarks = () => {
        viewport.querySelectorAll(marks.map(mark => `.${mark}`).join(', '))
            .forEach(cell => cell.classList.remove(...marks));
        viewport.classList.remove('omni-data-grid__viewport--reordering');
    };

    const onPointerDown = event => {
        const header = headerUnder(event.target);
        if (!header || !viewport.contains(header) || event.button !== 0 || event.target.closest(ownGestures)) {
            return;
        }

        drag = {
            header,
            key: header.getAttribute('data-omni-col'),
            group: header.getAttribute('data-omni-reorder'),
            startX: event.clientX,
            startY: event.clientY,
            pointerId: event.pointerId,
            moving: false,
            target: null,
            after: false
        };
    };

    const onPointerMove = event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        if (!drag.moving) {
            if (Math.hypot(event.clientX - drag.startX, event.clientY - drag.startY) < dragThreshold) {
                return;
            }

            drag.moving = true;
            drag.header.classList.add('omni-data-grid__dragged');
            viewport.classList.add('omni-data-grid__viewport--reordering');
            // The press may have begun selecting the title before the drag was recognised.
            window.getSelection()?.removeAllRanges();
        }

        viewport.querySelectorAll('.omni-data-grid__drop--before, .omni-data-grid__drop--after')
            .forEach(cell => cell.classList.remove(marks[0], marks[1]));
        drag.target = null;
        const over = headerUnder(document.elementFromPoint(event.clientX, event.clientY));
        if (!over || over === drag.header || !viewport.contains(over)
            || over.getAttribute('data-omni-reorder') !== drag.group) {
            return;
        }

        // The half of the header under the pointer: its start edge or its end edge, in the reading direction.
        const box = over.getBoundingClientRect();
        const rightToLeft = getComputedStyle(over).direction === 'rtl';
        const after = (event.clientX > box.left + box.width / 2) !== rightToLeft;
        over.classList.add(after ? marks[1] : marks[0]);
        drag.target = over.getAttribute('data-omni-col');
        drag.after = after;
    };

    const onPointerUp = event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        const finished = drag;
        drag = null;
        clearMarks();
        if (!finished.moving) {
            return;
        }

        // The release ends a drag, not a click: the header it lands on must not sort.
        swallowNextClick();
        if (event.type === 'pointerup' && finished.target) {
            notifyDotNet(() => live, reference, 'OnColumnDroppedAsync', finished.key, finished.target, finished.after);
        }
    };

    const onKeyDown = event => {
        if (drag && event.key === 'Escape') {
            const cancelled = drag;
            drag = null;
            clearMarks();
            if (cancelled.moving) {
                swallowNextClick();
            }
        }
    };

    viewport.addEventListener('pointerdown', onPointerDown);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);
    window.addEventListener('keydown', onKeyDown);

    reorderAttachments.set(viewport, {
        dispose: () => {
            live = false;
            drag = null;
            viewport.removeEventListener('pointerdown', onPointerDown);
            window.removeEventListener('pointermove', onPointerMove);
            window.removeEventListener('pointerup', onPointerUp);
            window.removeEventListener('pointercancel', onPointerUp);
            window.removeEventListener('keydown', onKeyDown);
            clearMarks();
        }
    });
}

export function detachReorder(viewport) {
    release(reorderAttachments, viewport);
}

/**
 * Once a column moved from the chooser, its row of the list was moved in the page, which takes the
 * focus away: the focus goes back to the same arrow of that column, or to its other arrow when this one
 * can no longer move it (the column reached an end).
 */
export function focusMoveControl(list, key, step) {
    if (!(list instanceof HTMLElement) || typeof key !== 'string') {
        return;
    }

    const control = [step, -step]
        .map(direction => list.querySelector(`[data-omni-move="${direction}"][data-omni-col="${CSS.escape(key)}"]`))
        .find(candidate => candidate instanceof HTMLElement && !candidate.disabled);
    control?.focus();
}

// A capture listener on the window runs before the grid's own handlers, which Blazor delegates from the
// document: the click that follows the release is stopped there, once, then the listener goes.
function swallowNextClick() {
    const swallow = event => {
        event.preventDefault();
        event.stopPropagation();
        window.removeEventListener('click', swallow, true);
    };
    window.addEventListener('click', swallow, true);
    setTimeout(() => window.removeEventListener('click', swallow, true), 0);
}
