// Part of omni-focus.js, which re-exports configureDisclosure, disposeDisclosure,
// observeFieldsetToggle and disposeFieldsetToggle: the native <details> disclosures and fieldsets.
import { notify } from './shared.js';
import { onPressOutside } from '../shared/outside-press.js';

const disclosures = new WeakMap();

// A native <details> used as a dropdown closes on Escape, on a press outside it unless the host
// turned that off, and, for a menu, once an item is chosen. The document listener only lives while
// the panel is open.
export function configureDisclosure(details, closeOnOutsideClick, closeOnItem) {
    if (!details) {
        return;
    }

    const known = disclosures.get(details);
    if (known) {
        known.outside = closeOnOutsideClick;
        known.item = closeOnItem;
        return;
    }

    const state = { outside: closeOnOutsideClick, item: closeOnItem, stopOutside: null };
    const listen = on => {
        if (on && !state.stopOutside) {
            state.stopOutside = onPressOutside(details, state.onPressOutside);
        } else if (!on && state.stopOutside) {
            state.stopOutside();
            state.stopOutside = null;
        }
    };
    state.onPressOutside = () => {
        if (!details.isConnected) {
            listen(false);
        } else if (state.outside && details.open) {
            details.open = false;
        }
    };
    state.onKeyDown = event => {
        if (event.key === 'Escape' && details.open) {
            event.stopPropagation();
            details.open = false;
            details.querySelector(':scope > summary')?.focus({ preventScroll: true });
        }
    };
    state.onClick = event => {
        const item = state.item && event.target instanceof Element ? event.target.closest('[role="menuitem"]') : null;
        if (item && details.contains(item) && !item.matches(':disabled, [aria-disabled="true"]')) {
            details.open = false;
        }
    };
    state.onToggle = () => listen(details.open);
    details.addEventListener('keydown', state.onKeyDown);
    details.addEventListener('click', state.onClick);
    details.addEventListener('toggle', state.onToggle);
    disclosures.set(details, state);
    state.onToggle();
}

export function disposeDisclosure(details) {
    const state = details ? disclosures.get(details) : undefined;
    if (!state) {
        return;
    }

    state.stopOutside?.();
    details.removeEventListener('keydown', state.onKeyDown);
    details.removeEventListener('click', state.onClick);
    details.removeEventListener('toggle', state.onToggle);
    disclosures.delete(details);
}

const fieldsetToggles = new WeakMap();

// Blazor delivers no toggle event, so a collapsible OmniFieldset that reports its state hears the
// native one here and hands the new open state back. Only added when the host asked for it.
export function observeFieldsetToggle(details, dotnet) {
    if (!(details instanceof HTMLDetailsElement) || !dotnet || fieldsetToggles.has(details)) {
        return;
    }

    const onToggle = () => notify(dotnet, 'OmniFieldset.Toggled', details.open);
    details.addEventListener('toggle', onToggle);
    fieldsetToggles.set(details, onToggle);
}

export function disposeFieldsetToggle(details) {
    const onToggle = details ? fieldsetToggles.get(details) : undefined;
    if (onToggle) {
        details.removeEventListener('toggle', onToggle);
        fieldsetToggles.delete(details);
    }
}
