// Palette application for OmniThemeScope.
//
// Values go through the CSSOM: the library forbids style attributes in markup, and setProperty is
// the path a strict style-src leaves open. They are set on the scope element itself, so only that
// scope and its content repaint.
const scopes = new WeakMap();
const follows = new WeakMap();

// The scope that keeps the snapshot (OmniThemeScope.SnapshotKey) takes over from the copy omni-boot.js
// laid before Blazor started: that copy leaves the scope now and the document root once the boot
// splash has left. Without omni-boot.js, or without a snapshot replayed, there is nothing to release.
// The outermost scope (fromPreferences) takes over the copy omni-boot.js laid from the package's own
// preference cache, never a host snapshot another scope keeps.
function handOver(fromPreferences) {
    const boot = globalThis.OmniBoot;
    if (!boot || typeof boot.handOverTheme !== 'function') {
        return;
    }

    if (fromPreferences === true) {
        boot.handOverTheme(true);
    } else {
        boot.handOverTheme();
    }
}

export function apply(element, light, dark, appearance, takeOver, fromPreferences) {
    if (!element) {
        return;
    }

    clear(element, takeOver, fromPreferences);
    const media = appearance === 'system' ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    const state = { media, names: new Set(), paint: null };
    state.paint = () => {
        const tokens = appearance === 'dark' || (media && media.matches) ? dark : light;
        for (const name of state.names) {
            element.style.removeProperty(name);
        }

        state.names.clear();
        for (const [name, value] of Object.entries(tokens)) {
            element.style.setProperty(name, value);
            state.names.add(name);
        }
    };

    if (media) {
        media.addEventListener('change', state.paint);
    }

    scopes.set(element, state);
    state.paint();
}

export function clear(element, takeOver, fromPreferences) {
    if (takeOver) {
        handOver();
    } else if (fromPreferences === true) {
        handOver(true);
    }

    const state = element ? scopes.get(element) : undefined;
    if (!state) {
        return;
    }

    if (state.media) {
        state.media.removeEventListener('change', state.paint);
    }

    for (const name of state.names) {
        element.style.removeProperty(name);
    }

    scopes.delete(element);
}

// data-omni-theme-resolved under the system mode: the markup cannot know the system setting, so the
// script writes "light" or "dark" and rewrites it when the setting changes. Under light or dark the
// markup carries the value itself; the listener then leaves it alone, and unfollowSystem stops it.
export function followSystem(element) {
    if (!element || follows.has(element) || !window.matchMedia) {
        return;
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const sync = () => {
        if (element.getAttribute('data-omni-theme') === 'system') {
            element.setAttribute('data-omni-theme-resolved', media.matches ? 'dark' : 'light');
        }
    };

    media.addEventListener('change', sync);
    follows.set(element, { media, sync });
    sync();
}

export function unfollowSystem(element) {
    const state = element ? follows.get(element) : undefined;
    if (!state) {
        return;
    }

    state.media.removeEventListener('change', state.sync);
    follows.delete(element);
}

// The snapshot omni-boot.js replays before Blazor starts (data-theme-snapshot-key): the attributes and
// both halves of the tokens exactly as the scope paints them, so the boot copy computes nothing. A scope
// that follows the user's appearance state also gives the text and control size levels it lays on the root.
export function snapshot(element, key, version, appearance, density, backdropMotion, light, dark, textSize, controlSize) {
    const entry = { version, appearance, density, backdropMotion, light: light ?? null, dark: dark ?? null };
    if (textSize !== undefined) {
        entry.textSize = textSize;
        entry.controlSize = controlSize;
    }

    try {
        window.localStorage.setItem(key, JSON.stringify(entry));
    } catch {
        // Private windows, a full or blocked storage: the next boot keeps the shipped look.
    }

    if (element) {
        handOver();
    }
}
