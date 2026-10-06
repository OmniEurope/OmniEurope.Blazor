// Part of omni-grid.js, which re-exports attachList, detachList, syncList and applyListLayout.
import { notifyDotNet } from './notify.js';
import { release } from './release.js';

// ---- virtualised data list ------------------------------------------------------------------
// A data list has no viewport of its own: like the page flow it lives in, it scrolls inside the
// nearest scrolling ancestor, or the page. The geometry sent to .NET is therefore the part of that
// scroll area which overlaps the list, expressed as an offset from the list's own top edge.

const listAttachments = new Map();

function scrollContainerOf(element) {
    for (let node = element.parentElement; node && node !== document.body; node = node.parentElement) {
        const overflow = getComputedStyle(node).overflowY;
        if (overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay') {
            return node;
        }
    }

    return null;
}

function listGeometry(root, container) {
    const rect = root.getBoundingClientRect();
    let viewTop = 0;
    let viewHeight = window.innerHeight || document.documentElement.clientHeight;
    if (container) {
        viewTop = container.getBoundingClientRect().top + container.clientTop;
        viewHeight = container.clientHeight;
    }

    return {
        scrollTop: Math.max(0, viewTop - rect.top),
        viewportHeight: viewHeight,
        scrollHeight: root.scrollHeight
    };
}

// Whether a scroll area stands at its end. Items are placed on an estimated height until they are
// measured: a jump to the end (the scrollbar dragged down, End) renders the last items, which then
// prove taller than estimated, the content grows and the end moves away while the scroll stays put,
// leaving the last item out of sight. applyListLayout keeps such an area at its end, as the grid does.
function atEndOf(scroller) {
    return scroller.scrollTop > 0 && scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 2;
}

function collectListItems(root) {
    // The list lays its items out on a grid with a row gap: an item occupies its own height plus
    // that gap, which is what the offset index must count.
    const gap = Number.parseFloat(getComputedStyle(root).rowGap) || 0;
    const rows = [];
    for (const item of root.querySelectorAll(':scope > [data-omni-row-index]')) {
        const index = Number.parseInt(item.getAttribute('data-omni-row-index') ?? '', 10);
        if (!Number.isNaN(index)) {
            rows.push({ index, height: item.getBoundingClientRect().height + gap });
        }
    }

    return rows;
}

/**
 * Starts following the scroll area of a virtualised list. Notifications are coalesced on the next
 * animation frame, one .NET round trip per frame at most.
 */
export function attachList(root, reference) {
    if (!(root instanceof HTMLElement) || !reference) {
        return null;
    }

    detachList(root);

    const container = scrollContainerOf(root);
    const target = container ?? window;
    const scroller = container ?? document.scrollingElement ?? document.documentElement;
    const state = { atEnd: false };
    const track = () => {
        state.atEnd = atEndOf(scroller);
    };
    let live = true;
    let frame = 0;
    const notify = () => {
        frame = 0;
        if (!root.isConnected) {
            detachList(root);
            return;
        }
        const current = listGeometry(root, container);
        notifyDotNet(() => live, reference, 'OnViewportChangedAsync', current.scrollTop, current.viewportHeight);
    };
    const schedule = () => {
        if (frame === 0) {
            frame = window.requestAnimationFrame(notify);
        }
    };

    target.addEventListener('scroll', track, { passive: true });
    target.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule, { passive: true });
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(container ?? root);

    listAttachments.set(root, {
        container,
        scroller,
        state,
        dispose: () => {
            live = false;
            if (frame !== 0) {
                window.cancelAnimationFrame(frame);
            }

            target.removeEventListener('scroll', track);
            target.removeEventListener('scroll', schedule);
            window.removeEventListener('resize', schedule);
            resizeObserver?.disconnect();
        }
    });

    return listGeometry(root, container);
}

export function detachList(root) {
    release(listAttachments, root);
}

/** Reads the list geometry and the height of every rendered item in one round trip. */
export function syncList(root) {
    if (!(root instanceof HTMLElement)) {
        return null;
    }

    const container = listAttachments.get(root)?.container ?? scrollContainerOf(root);
    return { ...listGeometry(root, container), rows: collectListItems(root) };
}

/**
 * Sizes the two spacers of a virtualised list through a custom property, never the style
 * attribute, so the strict CSP still holds. A scroll area left at its end stays there while the
 * items it reached are measured: the scroll this sets notifies .NET again, which renders what the
 * new end shows, until nothing grows.
 */
export function applyListLayout(root, topSpacer, bottomSpacer) {
    if (!(root instanceof HTMLElement)) {
        return;
    }

    root.querySelector(':scope > [data-omni-spacer="top"]')
        ?.style.setProperty('--omni-data-list-spacer', `${Math.max(0, topSpacer)}px`);
    root.querySelector(':scope > [data-omni-spacer="bottom"]')
        ?.style.setProperty('--omni-data-list-spacer', `${Math.max(0, bottomSpacer)}px`);

    const attachment = listAttachments.get(root);
    if (attachment?.state.atEnd) {
        const { scroller } = attachment;
        const end = scroller.scrollHeight - scroller.clientHeight;
        if (end - scroller.scrollTop > 1) {
            scroller.scrollTop = end;
        }
    }
}
