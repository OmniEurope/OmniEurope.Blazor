// Title scroll of OmniPageHeader.
//
// A title longer than its line is never cut: it scrolls sideways on one line. This module marks the
// frame with data-title-start and data-title-end while text remains hidden on that side, which the
// stylesheet reads to show the chevron button of that side only, and scrolls the title when one of
// those buttons is pressed. It measures and toggles attributes only; it never writes a style.
const frames = new WeakMap();

function titleOf(frame) {
    return frame.querySelector('.omni-page-header__title');
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

    // The title changes width with the window and with its own text (a crumb loaded late).
    const resize = typeof ResizeObserver === 'function' ? new ResizeObserver(queue) : null;
    const mutations = new MutationObserver(queue);
    resize?.observe(frame);
    mutations.observe(frame, { childList: true, subtree: true, characterData: true });
    frame.addEventListener('scroll', onScroll, true);
    frame.addEventListener('click', onClick);
    frames.set(frame, { resize, mutations, onScroll, onClick });
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
    frames.delete(frame);
}
