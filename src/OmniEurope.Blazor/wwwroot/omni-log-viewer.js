// The log viewer virtualises its lines with the grid's own mechanics (measured rows, two spacers sized
// through a CSS custom property) and adds one thing of its own: following the tail. While following,
// every render pins the viewport to its end; a scroll up by the reader releases it at once, here,
// before .NET hears of it, so a pin already on its way cannot drag the reader back down. Scrolling
// back to the very end follows again.
import { sync, applyLayout, scrollToOffset } from './omni-grid.js';

export { sync, applyLayout, scrollToOffset };

const viewers = new Map();

// A pixel of rounding separates "at the end" from "just short of it" on fractional zoom levels.
const bottomTolerance = 4;

function atBottom(viewport) {
    return viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight <= bottomTolerance;
}

export function attach(viewport, reference, following) {
    if (!(viewport instanceof HTMLElement) || !reference) {
        return;
    }

    detach(viewport);

    const state = {
        following: following === true,
        lastTop: viewport.scrollTop,
        lastHeight: viewport.scrollHeight,
        frame: 0
    };

    const notify = () => {
        state.frame = 0;
        reference.invokeMethodAsync('OnViewportChangedAsync', viewport.scrollTop, viewport.clientHeight, atBottom(viewport), state.following);
    };
    const schedule = () => {
        if (state.frame === 0) {
            state.frame = window.requestAnimationFrame(notify);
        }
    };
    const onScroll = () => {
        const top = viewport.scrollTop;
        const height = viewport.scrollHeight;
        // Moving up while the content did not shrink is the reader's doing: content that shrinks
        // (a filter, a cleared log) clamps the position up without anyone asking to leave the tail.
        if (state.following && top < state.lastTop - 1 && height >= state.lastHeight) {
            state.following = false;
        } else if (!state.following && top > state.lastTop && atBottom(viewport)) {
            state.following = true;
        }

        state.lastTop = top;
        state.lastHeight = height;
        schedule();
    };
    // A wheel turned up at a position the viewport cannot leave (already at the top of a short log)
    // produces no scroll event, yet it still says the reader wants to stop following.
    const onWheel = event => {
        if (state.following && event.deltaY < 0) {
            state.following = false;
            schedule();
        }
    };

    viewport.addEventListener('scroll', onScroll, { passive: true });
    viewport.addEventListener('wheel', onWheel, { passive: true });
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(viewport);

    state.dispose = () => {
        if (state.frame !== 0) {
            window.cancelAnimationFrame(state.frame);
        }

        viewport.removeEventListener('scroll', onScroll);
        viewport.removeEventListener('wheel', onWheel);
        resizeObserver?.disconnect();
    };
    viewers.set(viewport, state);
}

export function detach(viewport) {
    const state = viewers.get(viewport);
    if (state) {
        state.dispose();
        viewers.delete(viewport);
    }
}

/** Follows the tail again, or stops, when .NET decides it (the jump button, the follow toggle). */
export function setFollowing(viewport, following) {
    const state = viewers.get(viewport);
    if (state) {
        state.following = following === true;
    }
}

/**
 * Pins the viewport to its end, unless the reader has let go of the tail in the meantime. The
 * position is read back so the pin itself never counts as a move by the reader.
 */
export function pin(viewport) {
    const state = viewers.get(viewport);
    if (!state || !state.following) {
        return false;
    }

    viewport.scrollTop = viewport.scrollHeight;
    state.lastTop = viewport.scrollTop;
    state.lastHeight = viewport.scrollHeight;
    return true;
}

/** Scrolls to an offset (a search match) and stops following, which is what reading history means. */
export function reveal(viewport, offset) {
    const state = viewers.get(viewport);
    if (state) {
        state.following = false;
    }

    scrollToOffset(viewport, offset);
    if (state) {
        state.lastTop = viewport.scrollTop;
        state.lastHeight = viewport.scrollHeight;
    }
}
