// Palette application for OmniThemeScope.
//
// Values go through the CSSOM: the library forbids style attributes in markup, and setProperty is
// the path a strict style-src leaves open. They are set on the scope element itself, so only that
// scope and its content repaint.
const scopes = new WeakMap();
const follows = new WeakMap();
const watches = new WeakMap();

// A moving CSS field (--omni-scope-motion: Bokeh, Aquarelle, Quadrillage...) is painted again whole at
// every frame. A renderer without a GPU took 130 to 500 ms per frame at 1440 x 900 (headless Edge with
// --disable-gpu, 2026-11-24), holding the page's clicks and keys that long for as long as the field moved.
// The rule of the canvas fields (omni-black-hole.js) applies: three frames in a row further apart than
// SLOW_FRAME_MS and the scope is marked data-omni-backdrop-slow, which holds its field still (see the
// stylesheet) for as long as the scope lives: the renderer does not get faster.
const SLOW_FRAME_MS = 100;
const SLOW_FRAMES = 3;

// Whether the scope's own field is moving now: the animation runs only while a backdrop sets a motion,
// the scope lets it move and the system does not ask for less motion.
function fieldMoving(element) {
    return element.isConnected && element.getAnimations().some(animation => animation.animationName === 'omni-scope-turn' && animation.playState === 'running');
}

// Watches the frames while the field moves; the loop stops with the field and starts again with it
// (animationstart), and the time spent with the page hidden is no slow frame.
function watchMotion(element) {
    if (watches.has(element) || element.hasAttribute('data-omni-backdrop-slow')) {
        return;
    }

    const watch = { frame: 0, last: 0, slow: 0, start: null };
    const loop = now => {
        watch.frame = 0;
        if (!fieldMoving(element)) {
            return;
        }

        watch.slow = watch.last && now - watch.last > SLOW_FRAME_MS ? watch.slow + 1 : 0;
        watch.last = now;
        if (watch.slow >= SLOW_FRAMES) {
            element.setAttribute('data-omni-backdrop-slow', '');
            unwatchMotion(element);
            return;
        }

        watch.frame = requestAnimationFrame(loop);
    };
    watch.start = () => {
        watch.last = 0;
        watch.slow = 0;
        if (!watch.frame && !document.hidden) {
            watch.frame = requestAnimationFrame(loop);
        }
    };
    element.addEventListener('animationstart', watch.start);
    document.addEventListener('visibilitychange', watch.start);
    watches.set(element, watch);
    watch.start();
}

function unwatchMotion(element) {
    const watch = watches.get(element);
    if (!watch) {
        return;
    }

    if (watch.frame) {
        cancelAnimationFrame(watch.frame);
    }
    element.removeEventListener('animationstart', watch.start);
    document.removeEventListener('visibilitychange', watch.start);
    watches.delete(element);
}

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
    watchMotion(element);
}

export function clear(element, takeOver, fromPreferences) {
    if (takeOver) {
        handOver();
    } else if (fromPreferences === true) {
        handOver(true);
    }

    if (element) {
        unwatchMotion(element);
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
