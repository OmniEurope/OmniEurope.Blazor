// Boot script of an OmniEurope.Blazor application: include it in the head of the page, before
// Blazor starts, as a plain script file (no module, nothing inline, so it runs under
// `script-src 'self'`):
//
//   <script src="_content/OmniEurope.Blazor/omni-boot.js"
//           data-appearance-key="my-app:appearance"
//           data-theme-key="my-app:theme"
//           data-language-key="my-app:language"></script>
//
// Before the first paint it reads the keys the host names from localStorage and applies them to the
// document element: the appearance ("light", "dark" or "system") as data-omni-theme, which the
// library's stylesheet reads, so a dark page never flashes light; a theme name as
// data-omni-theme-preset, for the host's own rules; the language as the lang attribute, falling back
// to the browser's language. Values that are not well formed are ignored. Every key is optional.
//
// It also publishes window.OmniBoot: `culture`, the validated saved language (or null) for the host's
// own `Blazor.start({ applicationCulture })`, and `hideSplash(id)`, which fades out and removes the
// boot splash, as the OmniBootSplash component does once the application has rendered.
(function () {
    'use strict';

    var script = document.currentScript;
    var data = (script && script.dataset) || {};
    var root = document.documentElement;
    var appearances = { light: true, dark: true, system: true };
    var languagePattern = /^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$/;
    var themePattern = /^[A-Za-z0-9_-]{1,64}$/;

    function read(key) {
        if (!key) {
            return null;
        }

        try {
            return window.localStorage.getItem(key);
        } catch (error) {
            // Private windows and blocked site data are normal: the page keeps its defaults.
            return null;
        }
    }

    var appearance = read(data.appearanceKey);
    if (appearance && appearances[appearance] === true) {
        root.setAttribute('data-omni-theme', appearance);
    }

    var theme = read(data.themeKey);
    if (theme && themePattern.test(theme)) {
        root.setAttribute('data-omni-theme-preset', theme);
    }

    var saved = read(data.languageKey);
    var culture = saved && languagePattern.test(saved) ? saved : null;
    if (data.languageKey) {
        var browser = navigator.language && languagePattern.test(navigator.language) ? navigator.language : null;
        var language = culture || browser;
        if (language) {
            root.lang = language;
        }
    }

    function hideSplash(id) {
        var splash = document.getElementById(id || 'omni-boot-splash');
        if (!splash || splash.classList.contains('omni-boot-splash--leaving')) {
            return false;
        }

        var remove = function () { splash.remove(); };
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            remove();
            return true;
        }

        splash.classList.add('omni-boot-splash--leaving');
        splash.addEventListener('transitionend', remove, { once: true });
        window.setTimeout(remove, 600);
        return true;
    }

    window.OmniBoot = { culture: culture, hideSplash: hideSplash };
})();