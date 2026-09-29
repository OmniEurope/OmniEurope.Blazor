// Line 1 of OmniPageHeader: the fold of its badges and actions, and the title scroll.
//
// Fold: the badges and actions fold behind the "Show more" toggle as soon as line 1 has no room for
// them beside the title, whatever the width (the stylesheet folds them on a phone on its own). The
// module measures line 1 unfolded, the attribute removed, and marks the frame data-compact when the
// row overflows or the title is squeezed; the stylesheet reads that attribute to apply the phone fold.
// A frame without badges or actions is never marked. The "⋮" menu is outside the fold.
//
// Title scroll: a title longer than its line is never cut: it scrolls sideways on one line. The module
// marks the frame with data-title-start and data-title-end while text remains hidden on that side,
// which the stylesheet reads to show the chevron button of that side only, and scrolls the title when
// one of those buttons is pressed.
//
// Both are measured again when the frame is resized and when its content changes (a crumb or a badge
// loaded late). The module measures and toggles attributes only; it never writes a style. The flags are
// attributes because Blazor owns the class attribute, and Blazor leaves attributes it never rendered.
const frames = new WeakMap();

function titleOf(frame) {
    return frame.querySelector('.omni-page-header__title');
}

// Unfolded first: removing the attribute lays line 1 out as the stylesheet does without it, and the
// reads below force that layout before anything is painted, so the fold never flickers.
function fit(frame) {
    frame.removeAttribute('data-compact');
    const row = frame.querySelector('.omni-page-header__row');
    if (!row || !row.querySelector('.omni-page-header__details')) {
        return;
    }

    const title = titleOf(frame);
    const crowded = row.scrollWidth > row.clientWidth + 1
        || (!!title && title.scrollWidth > title.clientWidth + 1);
    frame.toggleAttribute('data-compact', crowded);
}

// scrollLeft runs negative from the start edge in a right-to-left page; its magnitude is the distance.
function mark(frame) {
    const title = titleOf(frame);
    const offset = title ? Math.abs(title.scrollLeft) : 0;
    frame.toggleAttribute('data-title-start', !!title && offset > 1);
    frame.toggleAttribute('data-title-end', !!title && offset + title.clientWidth < title.scrollWidth - 1);
}

export function attach(frame) {
    if (!frame || frames.has(frame)) {
        return;
    }

    let queued = false;
    const queue = () => {
        if (queued) {
            return;
        }

        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            if (frame.isConnected) {
                fit(frame);
                mark(frame);
            }
        });
    };

    const onScroll = event => {
        if (event.target instanceof Element && event.target.matches('.omni-page-header__title')) {
            queue();
        }
    };

    const onClick = event => {
        const button = event.target instanceof Element ? event.target.closest('.omni-page-header__scroll') : null;
        const title = button && frame.contains(button) ? titleOf(frame) : null;
        if (!title) {
            return;
        }

        const towardsStart = button.classList.contains('omni-page-header__scroll--start');
        const rightToLeft = getComputedStyle(title).direction === 'rtl';
        const step = Math.max(40, title.clientWidth * 0.8);
        title.scrollBy({ left: (towardsStart !== rightToLeft ? -step : step), behavior: 'smooth' });
    };

    // Line 1 changes width with the window and with its content (a crumb or a badge loaded late).
    const resize = typeof ResizeObserver === 'function' ? new ResizeObserver(queue) : null;
    const mutations = new MutationObserver(queue);
    resize?.observe(frame);
    mutations.observe(frame, { childList: true, subtree: true, characterData: true });
    frame.addEventListener('scroll', onScroll, true);
    frame.addEventListener('click', onClick);
    frames.set(frame, { resize, mutations, onScroll, onClick });
    fit(frame);
    mark(frame);
}

export function detach(frame) {
    const state = frame ? frames.get(frame) : undefined;
    if (!state) {
        return;
    }

    state.resize?.disconnect();
    state.mutations.disconnect();
    frame.removeEventListener('scroll', state.onScroll, true);
    frame.removeEventListener('click', state.onClick);
    frame.removeAttribute('data-compact');
    frames.delete(frame);
}
