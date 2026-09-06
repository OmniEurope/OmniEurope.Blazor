// Theme application for the showcase.
//
// Values go through the CSSOM. The site ships a policy with `style-src 'self'`, which rules out a
// style attribute and an injected style element; setProperty on the document element is the path
// that policy leaves open, and it repaints every component at once because the whole library reads
// its values from custom properties.
const previous = new Set();

window.omniShowcaseTheme = {
    apply(overrides, mode, storageKey, serialized) {
        const root = document.documentElement;

        // Clear tokens dropped since the last push, otherwise a removed override would stay lit.
        for (const name of previous) {
            if (!(name in overrides)) {
                root.style.removeProperty(name);
            }
        }

        previous.clear();
        for (const [name, value] of Object.entries(overrides)) {
            root.style.setProperty(name, value);
            previous.add(name);
        }

        root.setAttribute('data-omni-theme', mode);

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
