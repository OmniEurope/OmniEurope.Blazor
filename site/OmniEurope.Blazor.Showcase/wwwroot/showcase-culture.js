// Language of the showcase, chosen in the header and kept in the browser.
//
// Program.cs reads the saved code before the WebAssembly host starts, because a page keeps the
// culture it started with: changing the language saves the new code and reloads the page. The code
// is then written on <html lang>, so assistive technologies read the page in the chosen language.
window.omniShowcaseCulture = {
    load(storageKey) {
        try {
            return window.localStorage.getItem(storageKey);
        } catch {
            // Private windows and blocked site data are normal: the page falls back to French.
            return null;
        }
    },

    save(storageKey, code) {
        try {
            window.localStorage.setItem(storageKey, code);
            return true;
        } catch {
            return false;
        }
    },

    mark(code) {
        document.documentElement.lang = code;
    }
};
