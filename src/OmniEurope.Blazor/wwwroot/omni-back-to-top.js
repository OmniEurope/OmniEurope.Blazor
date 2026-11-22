// OmniBackToTop: shows its button once the content has scrolled past a threshold, and brings the reader back to the
// top of that content.
//
// The content is the element named by the component's TargetId; without one, the content scroller of the OE layout
// (an OmniMain with Scrollable: the one holding the button, else the first of the page), else the document. It is
// looked up again at every scroll rather than kept: a page that navigates may replace its main area, or make it
// scrollable later. One capturing scroll listener on the document hears the document and every element that scrolls
// (a scroll event does not bubble, but it is captured), and the check runs once per animation frame.
//
// The state is an attribute (data-shown) on the component's frame, which the stylesheet reads to show the button:
// Blazor owns the class attribute and leaves an attribute it never rendered. Nothing is reported to .NET, so a lost
// circuit stops nothing and the page never waits on a round trip while it scrolls. The module writes no style.
//
// Going to the top is instant under prefers-reduced-motion and smooth otherwise ('instant' rather than 'auto', which
// would follow a host's CSS scroll-behavior). The focus then goes to the top of the content, so the next Tab starts
// there instead of on a button that hides: the content itself when it is an element (an OmniMain takes the focus from
// script, tabindex -1, by default), the first main landmark for the document, given tabindex -1 when it could not take
// the focus, as OmniSkipLink does; with no main landmark the button only lets go of the focus, which leaves the next
// Tab at the start of the document. The focus is moved with preventScroll, so it never interrupts the scroll.
const attached = new WeakMap();

const reducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;

function documentScroller() {
    return document.scrollingElement ?? document.documentElement;
}

function isDocument(scroller) {
    return scroller === documentScroller() || scroller === document.documentElement || scroller === document.body;
}

// The element whose scroll the button follows, or null when the TargetId names nothing (the button stays hidden).
function scrollerOf(frame, targetId) {
    if (targetId) {
        return document.getElementById(targetId);
    }

    return frame.closest('.omni-main--scrollable')
        ?? document.querySelector('.omni-main--scrollable')
        ?? documentScroller();
}

function update(frame) {
    const state = attached.get(frame);
    if (!state || !frame.isConnected) {
        return;
    }

    const scroller = scrollerOf(frame, state.targetId);
    const past = !!scroller && (scroller.scrollTop ?? 0) > state.threshold;
    // A button that holds the focus stays shown: hiding it would drop the focus on the document.
    const focused = frame.contains(document.activeElement) && document.activeElement !== frame;
    if (past || focused) {
        frame.setAttribute('data-shown', '');
    } else {
        frame.removeAttribute('data-shown');
    }
}

// Starts following the content's scroll, or, for a frame already attached, takes its new target and threshold.
export function attach(frame, targetId, threshold) {
    if (!frame) {
        return;
    }

    const known = attached.get(frame);
    if (known) {
        known.targetId = targetId || null;
        known.threshold = Math.max(0, Number(threshold) || 0);
        update(frame);
        return;
    }

    const state = { targetId: targetId || null, threshold: Math.max(0, Number(threshold) || 0), frameId: 0 };
    state.queue = () => {
        if (state.frameId) {
            return;
        }

        state.frameId = window.requestAnimationFrame(() => {
            state.frameId = 0;
            update(frame);
        });
    };
    document.addEventListener('scroll', state.queue, { capture: true, passive: true });
    window.addEventListener('resize', state.queue, { passive: true });
    attached.set(frame, state);
    update(frame);
}

// Brings the content back to its top and moves the focus there. Returns false when there is no content to scroll.
export function scrollToTop(frame) {
    const state = frame ? attached.get(frame) : undefined;
    const scroller = frame ? scrollerOf(frame, state?.targetId ?? null) : null;
    if (!scroller) {
        return false;
    }

    const target = isDocument(scroller) ? document.querySelector('main') : scroller;
    if (target) {
        if (!(target.tabIndex >= 0) && !target.hasAttribute('tabindex')) {
            target.setAttribute('tabindex', '-1');
        }

        target.focus({ preventScroll: true });
    } else if (frame.contains(document.activeElement)) {
        document.activeElement.blur?.();
    }

    scroller.scrollTo({ top: 0, behavior: reducedMotion() ? 'instant' : 'smooth' });
    return true;
}

// Stops following the scroll and hides the button.
export function detach(frame) {
    const state = frame ? attached.get(frame) : undefined;
    if (!state) {
        return;
    }

    document.removeEventListener('scroll', state.queue, { capture: true });
    window.removeEventListener('resize', state.queue);
    if (state.frameId) {
        window.cancelAnimationFrame(state.frameId);
    }

    frame.removeAttribute('data-shown');
    attached.delete(frame);
}
