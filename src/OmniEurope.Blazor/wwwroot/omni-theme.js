// Palette application for OmniThemeScope.
//
// Values go through the CSSOM: the library forbids style attributes in markup, and setProperty is
// the path a strict style-src leaves open. They are set on the scope element itself, so only that
// scope and its content repaint.
const scopes = new WeakMap();

export function apply(element, light, dark, appearance) {
    if (!element) {
        return;
    }

    clear(element);
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

export function clear(element) {
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
