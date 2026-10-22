// Part of omni-focus.js, which re-exports attachPopover and detachPopover: the non-modal popover.
import { returnTargets, rememberTarget, notify } from './shared.js';
import { focusableElements } from './dialog.js';
import { placeFloating } from '../shared/placement.js';
import { onPressOutside } from '../shared/outside-press.js';

// ---- non-modal popover -----------------------------------------------------------------------
// The panel does not trap focus: Tab may leave it. What closes it is a pointer press outside the
// popover (focus stays where the user clicked) or Escape inside it (focus returns to the trigger).

const popoverHandlers = new Map();

// The panel is anchored to its trigger by CSS; near an edge of the window it is shifted sideways, and
// opened above its trigger when there is no room below, so it never leaves the window. Where the
// panel sits now is the anchor of the placement (its own span, from the top of the trigger to its own
// top), so the engine only has to say how far to shift it and on which side it fits.
function keepInViewport(panel) {
    if (!(panel instanceof HTMLElement)) {
        return;
    }

    panel.style.removeProperty('--omni-popover-shift-x');
    panel.classList.remove('omni-popover__panel--above');
    const rect = panel.getBoundingClientRect();
    const trigger = panel.parentElement?.getBoundingClientRect();
    const placed = placeFloating({
        anchor: { left: rect.left, right: rect.right, top: trigger?.top ?? rect.top, bottom: rect.top },
        size: rect,
        gap: 0
    });
    const shift = placed.left - rect.left;
    if (shift !== 0) {
        panel.style.setProperty('--omni-popover-shift-x', `${Math.round(shift)}px`);
    }

    if (trigger && placed.side === 'top') {
        panel.classList.add('omni-popover__panel--above');
    }
}

export function attachPopover(root, panel, dotnet, key) {
    if (!(root instanceof HTMLElement) || !dotnet || popoverHandlers.has(key)) {
        return;
    }

    rememberTarget(key);
    keepInViewport(panel);
    // A control of the panel that already holds the focus (autofocus, a focus moved by the host) keeps it.
    if (!panel?.contains(document.activeElement)) {
        const items = focusableElements(panel);
        (items[0] ?? panel)?.focus({ preventScroll: true });
    }

    const onResize = () => keepInViewport(panel);

    const onKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            notify(dotnet, 'OnDismissRequestedAsync', true);
        }
    };

    const stopOutside = onPressOutside(root, () => notify(dotnet, 'OnDismissRequestedAsync', false));
    root.addEventListener('keydown', onKeyDown);
    window.addEventListener('resize', onResize);
    popoverHandlers.set(key, { root, stopOutside, onKeyDown, onResize });
}

export function detachPopover(key, restore) {
    const state = popoverHandlers.get(key);
    if (state) {
        state.stopOutside();
        state.root.removeEventListener('keydown', state.onKeyDown);
        window.removeEventListener('resize', state.onResize);
        popoverHandlers.delete(key);
    }

    // The trigger never leaves the page while its panel closes, so focus goes back at once, without
    // the animation frames restoreFocus waits for (a background tab would not deliver them).
    const target = returnTargets.get(key);
    returnTargets.delete(key);
    if (restore && target?.isConnected && !target.closest('[inert]')) {
        target.focus({ preventScroll: true });
    }
}
