// Part of omni-focus.js, which re-exports these: the roving focus of a tab strip, the sideways
// overflow chevrons of the tab strips, scrolling OmniStack rows and OmniSelectBar, the upright ones
// of OmniSidebar, and the wheel scope of OmniTabs.

const tabHandlers = new WeakMap();
const tabOverflow = new WeakMap();

export function configureTabs(tablist, dotnet) {
    if (!tablist || tabHandlers.has(tablist)) {
        return;
    }

    const handler = event => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        const current = event.target.closest('[role="tab"]');
        if (!current || !tablist.contains(current)) {
            return;
        }
        const tabs = Array.from(tablist.querySelectorAll('[role="tab"]:not([disabled]):not([aria-disabled="true"])'));
        if (tabs.length === 0) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const index = Math.max(0, tabs.indexOf(current));
        let next = index;
        if (event.key === 'Home') next = 0;
        if (event.key === 'End') next = tabs.length - 1;
        if (event.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
        if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
        tabs[next].focus({ preventScroll: true });
        void dotnet.invokeMethodAsync('OmniTabs.SelectFromKeyboard', tabs[next].dataset.key);
    };
    tablist.addEventListener('keydown', handler);
    tabHandlers.set(tablist, handler);
}

export function disposeTabs(tablist) {
    const handler = tabHandlers.get(tablist);
    if (handler) {
        tablist.removeEventListener('keydown', handler);
        tabHandlers.delete(tablist);
    }
}

// A tab strip never wraps, so it has to say when it hides tabs off either edge. The strip carries
// data-omni-overflow-start / -end, which the stylesheet turns into the fade, and the two chevron
// buttons are unhidden with it. Both go out at the stops, so reaching an end is visible.
export function configureTabsOverflow(strip) {
    configureOverflow(strip, '.omni-tabs__viewport', '.omni-tabs__scroll--start', '.omni-tabs__scroll--end');
}

// A scrolling OmniStack row works the same way: no scrollbar, a chevron on each side that still
// holds items, gone at the stop.
export function configureScrollOverflow(strip) {
    configureOverflow(strip, ':scope > .omni-stack-scroll__viewport', ':scope > .omni-stack-scroll__button--start', ':scope > .omni-stack-scroll__button--end');
}

// The menu of an OmniSidebar taller than the window works the same way upright: no scrollbar, a
// chevron above and below while items are hidden that way, gone at the stop.
export function configureSidebarOverflow(panel) {
    configureOverflow(panel, ':scope > .omni-sidebar__viewport', ':scope > .omni-sidebar__scroll--start', ':scope > .omni-sidebar__scroll--end', true);
}

export function disposeSidebarOverflow(panel) {
    disposeTabsOverflow(panel);
}

function configureOverflow(strip, viewportSelector, startSelector, endSelector, upright = false) {
    if (!strip || tabOverflow.has(strip)) {
        return;
    }

    const viewport = strip.querySelector(viewportSelector);
    if (!viewport) {
        return;
    }

    const start = strip.querySelector(startSelector);
    const end = strip.querySelector(endSelector);

    // The browser leaves a focused item partly clipped when it is already partly in view, and a
    // chevron that appears afterwards narrows the strip over it. The item reached by the keyboard is
    // brought wholly inside the content box, at once rather than smoothly, so the measure after it is
    // the final one. Only keyboard focus: a button clicked earlier must not pull the strip back while
    // the reader scrolls by hand, so a plain scroll never calls this.
    const reveal = () => {
        const focused = document.activeElement;
        if (!focused || focused === viewport || !viewport.contains(focused) || !focused.matches(':focus-visible')) {
            return;
        }

        const box = viewport.getBoundingClientRect();
        const padding = getComputedStyle(viewport);
        const item = focused.getBoundingClientRect();
        const [low, high, itemLow, itemHigh] = upright
            ? [box.top + parseFloat(padding.paddingTop), box.bottom - parseFloat(padding.paddingBottom), item.top, item.bottom]
            : [box.left + parseFloat(padding.paddingLeft), box.right - parseFloat(padding.paddingRight), item.left, item.right];
        const delta = itemLow < low ? itemLow - low : itemHigh > high ? itemHigh - high : 0;
        if (Math.abs(delta) > 0.5) {
            viewport.scrollBy({ [upright ? 'top' : 'left']: delta, behavior: 'instant' });
        }
    };

    const update = () => {
        // Right-to-left scrolling reports scrollLeft as negative or decreasing, so the distance to
        // each edge is measured in absolute terms rather than from the raw value.
        const offset = Math.abs(upright ? viewport.scrollTop : viewport.scrollLeft);
        const hidden = upright ? viewport.scrollHeight - viewport.clientHeight : viewport.scrollWidth - viewport.clientWidth;
        // A sub-pixel remainder is not an overflow: rounding alone would keep a chevron lit on a
        // strip that has nothing left to show.
        const atStart = offset <= 1;
        const atEnd = offset >= hidden - 1;
        strip.toggleAttribute('data-omni-overflow-start', !atStart);
        strip.toggleAttribute('data-omni-overflow-end', hidden > 1 && !atEnd);
        if (start) start.hidden = atStart;
        if (end) end.hidden = hidden <= 1 || atEnd;
    };

    const scrollBy = direction => {
        // A step short of a full page keeps one tab in common between the two views, so the reader
        // never loses their place.
        const page = upright ? viewport.clientHeight : viewport.clientWidth;
        viewport.scrollBy({ [upright ? 'top' : 'left']: direction * page * 0.8, behavior: 'smooth' });
    };

    const onStart = () => scrollBy(-1);
    const onEnd = () => scrollBy(1);
    start?.addEventListener('click', onStart);
    end?.addEventListener('click', onEnd);
    viewport.addEventListener('scroll', update, { passive: true });
    viewport.addEventListener('focusin', reveal);

    // The strip also overflows when the window narrows or when a tab is added, neither of which
    // fires a scroll event. A chevron appearing narrows the strip too, which is when a focused item
    // it now covers is brought back.
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => {
        update();
        reveal();
    });
    observer?.observe(viewport);
    const mutations = typeof MutationObserver === 'undefined' ? null : new MutationObserver(update);
    mutations?.observe(viewport, { childList: true, subtree: true });

    tabOverflow.set(strip, { viewport, start, end, onStart, onEnd, update, reveal, observer, mutations });
    update();
}

export function disposeScrollOverflow(strip) {
    disposeTabsOverflow(strip);
}

// OmniSelectBar: same chevrons, no scrollbar.
export function configureSelectBarOverflow(strip) {
    configureOverflow(strip, ':scope > .omni-select-bar__viewport', ':scope > .omni-select-bar__scroll--start', ':scope > .omni-select-bar__scroll--end');
}

export function disposeTabsOverflow(strip) {
    const state = tabOverflow.get(strip);
    if (!state) {
        return;
    }

    state.start?.removeEventListener('click', state.onStart);
    state.end?.removeEventListener('click', state.onEnd);
    state.viewport.removeEventListener('scroll', state.update);
    state.viewport.removeEventListener('focusin', state.reveal);
    state.observer?.disconnect();
    state.mutations?.disconnect();
    tabOverflow.delete(strip);
}

const tabsWheelScopes = new Map();

function scrollsVertically(element, deltaY) {
    if (!/(auto|scroll)/.test(getComputedStyle(element).overflowY) || element.scrollHeight <= element.clientHeight) {
        return false;
    }

    return deltaY < 0
        ? element.scrollTop > 0
        : element.scrollTop + element.clientHeight < element.scrollHeight - 1;
}

/**
 * OmniTabs WheelScrollScope: a vertical wheel turn over the named ancestor, outside the selected
 * panel, scrolls that panel. Anything under the pointer that can still scroll that way keeps the
 * wheel, Shift and Ctrl are never taken, and a panel that cannot scroll further lets the event go.
 */
export function attachTabsWheelScope(tabs, selector) {
    if (!(tabs instanceof HTMLElement)) {
        return;
    }

    detachTabsWheelScope(tabs);
    const scope = typeof selector === 'string' && selector ? tabs.closest(selector) : null;
    if (!scope) {
        return;
    }

    const onWheel = event => {
        if (event.defaultPrevented || event.ctrlKey || event.shiftKey || Math.abs(event.deltaY) <= Math.abs(event.deltaX)) {
            return;
        }

        const panel = [...tabs.children].find(child => child.classList.contains('omni-tabs__panel') && !child.hidden);
        if (!panel || !tabs.isConnected || panel.getClientRects().length === 0) {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        if (target && panel.contains(target)) {
            return;
        }

        for (let node = target; node && node !== scope; node = node.parentElement) {
            if (scrollsVertically(node, event.deltaY)) {
                return;
            }
        }

        if (!scrollsVertically(panel, event.deltaY)) {
            return;
        }

        const unit = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? panel.clientHeight : 1;
        event.preventDefault();
        panel.scrollTop += event.deltaY * unit;
    };

    scope.addEventListener('wheel', onWheel, { passive: false });
    tabsWheelScopes.set(tabs, () => scope.removeEventListener('wheel', onWheel));
}

export function detachTabsWheelScope(tabs) {
    const dispose = tabsWheelScopes.get(tabs);
    if (dispose) {
        dispose();
        tabsWheelScopes.delete(tabs);
    }
}
