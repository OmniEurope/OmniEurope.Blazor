// Theme application for the showcase.
//
// Values go through the CSSOM. The site ships a policy with `style-src 'self'`, which rules out a
// style attribute and an injected style element; setProperty is the path that policy leaves open,
// and it repaints every component at once because the whole library reads its values from custom
// properties.
//
// They are set on the layout's own theme scope, not on the document element. The scope carries
// `data-omni-theme`, and the stylesheet redeclares the surface, text, border and accent colours on
// that attribute: a value written higher up, on <html>, is shadowed by those declarations and the
// page keeps the shipped palette whatever theme was applied. An inline value on the scope itself
// beats them.
//
// Both halves arrive with every push. The light or the dark one is painted according to the mode;
// in the system mode the choice follows the system setting, and changes with it.
const previous = new Set();
const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');
let painted = null;
let current = null;

function target() {
    return document.getElementById('showcase-theme') ?? document.documentElement;
}

function paint() {
    if (!current) {
        return;
    }

    const root = target();
    const dark = current.mode === 'dark' || (current.mode === 'system' && darkQuery.matches);
    const tokens = dark ? current.dark : current.light;
    if (painted && painted !== root) {
        for (const name of previous) {
            painted.style.removeProperty(name);
        }

        previous.clear();
    }

    // Clear tokens dropped since the last push, otherwise a removed value would stay lit.
    for (const name of previous) {
        if (!(name in tokens)) {
            root.style.removeProperty(name);
        }
    }

    previous.clear();
    for (const [name, value] of Object.entries(tokens)) {
        root.style.setProperty(name, value);
        previous.add(name);
    }

    painted = root;
}

darkQuery.addEventListener('change', paint);

window.omniShowcaseTheme = {
    apply(light, dark, mode, storageKey, serialized) {
        current = { light, dark, mode };
        paint();
        document.documentElement.setAttribute('data-omni-theme', mode);

        try {
            window.localStorage.setItem(storageKey, serialized);
        } catch {
            // Private windows and blocked site data are normal; the preview still works, it simply
            // does not survive a reload.
        }
    },

    load(storageKey) {
        try {
            return window.localStorage.getItem(storageKey);
        } catch {
            return null;
        }
    },

    async copy(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },

    download(fileName, text) {
        try {
            const blob = new Blob([text], { type: 'text/css' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(url);
            return true;
        } catch {
            return false;
        }
    }
};
