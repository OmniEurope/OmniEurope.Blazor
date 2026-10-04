// Boot script of an OmniEurope.Blazor application: include it in the head of the page, before
// Blazor starts, as a plain script file (no module, nothing inline, so it runs under
// `script-src 'self'`):
//
//   <script src="_content/OmniEurope.Blazor/omni-boot.js"
//           data-theme-snapshot-key="my-app:theme"
//           data-text-size-key="my-app:text-size"
//           data-control-size-key="my-app:control-size"
//           data-language-key="my-app:language"></script>
//
// Before the first paint it reads the keys the host names from localStorage and applies them to the
// document element. Every key is optional and a value that is not well formed is ignored.
//
// - data-theme-snapshot-key: the snapshot an OmniThemeScope with the same SnapshotKey wrote of what it
//   painted (version, appearance, density, backdrop motion, light and dark tokens). The root takes the
//   scope's attributes (data-omni-theme, data-omni-theme-resolved, data-omni-density,
//   data-omni-backdrop-motion) and the tokens of the half the mode resolves to, plus
//   data-omni-theme-boot, on which the package stylesheet paints the page in that look. The same tokens
//   are laid on the scope as soon as Blazor renders it, when it draws the same half, so its first frame
//   is already the stored look. Nothing is computed here: the values are the ones the scope painted.
//   Hand-over: when the scope paints for the first time it calls OmniBoot.handOverTheme(); the copy
//   leaves the scope at once, and the root once no element matches data-theme-hold (the package boot
//   splash, `.omni-boot-splash`, by default; an empty value releases at once). The root attributes
//   get back the value they had before, unless something else changed them meanwhile.
// - data-appearance-key: an appearance ("light", "dark" or "system") kept as data-omni-theme on the
//   root for good, which the library's stylesheet reads, so a dark page never flashes light.
// - data-theme-key: a theme name kept as data-omni-theme-preset, for the host's own rules.
// - data-text-size-key, data-control-size-key: a level from 1 to 10, kept as data-oe-text-size and
//   data-oe-control-size, as omni-appearance.js sets them.
// - data-language-key: the language, as the lang attribute, falling back to the browser's language.
//
// It also publishes window.OmniBoot: `culture`, the validated saved language (or null) for the host's
// own `Blazor.start({ applicationCulture })`; `hideSplash(id)`, which fades out and removes the boot
// splash, as the OmniBootSplash component does once the application has rendered; `handOverTheme()`,
// called by the package; and `releaseTheme()`, which removes the boot copy of the theme at once.
(function () {
    'use strict';

    var script = document.currentScript;
    var data = (script && script.dataset) || {};
    var root = document.documentElement;
    var appearances = { light: true, dark: true, system: true };
    var densities = { compact: true, comfortable: true, spacious: true };
    var languagePattern = /^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$/;
    var themePattern = /^[A-Za-z0-9_-]{1,64}$/;
    var levelPattern = /^(10|[1-9])$/;
    var snapshotVersion = 1;
    var darkQuery = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

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

    // The half a mode is drawn in, as omni-theme.js picks it.
    function resolve(appearance) {
        return appearance === 'dark' || (appearance === 'system' && darkQuery !== null && darkQuery.matches) ? 'dark' : 'light';
    }

    var appearance = read(data.appearanceKey);
    if (appearance && appearances[appearance] === true) {
        root.setAttribute('data-omni-theme', appearance);
    }

    var preset = read(data.themeKey);
    if (preset && themePattern.test(preset)) {
        root.setAttribute('data-omni-theme-preset', preset);
    }

    [['textSizeKey', 'data-oe-text-size'], ['controlSizeKey', 'data-oe-control-size']].forEach(function (scale) {
        var level = read(data[scale[0]]);
        if (level && levelPattern.test(level)) {
            root.setAttribute(scale[1], level);
        }
    });

    var saved = read(data.languageKey);
    var culture = saved && languagePattern.test(saved) ? saved : null;
    if (data.languageKey) {
        var browser = navigator.language && languagePattern.test(navigator.language) ? navigator.language : null;
        var language = culture || browser;
        if (language) {
            root.lang = language;
        }
    }

    // ---- The boot copy of the theme ----

    // Only custom properties with a text value pass: the snapshot is data, never markup.
    function tokensOf(half) {
        if (!half || typeof half !== 'object') {
            return null;
        }

        var tokens = {};
        var count = 0;
        Object.keys(half).forEach(function (name) {
            if (name.indexOf('--') === 0 && typeof half[name] === 'string') {
                tokens[name] = half[name];
                count++;
            }
        });
        return count > 0 ? tokens : null;
    }

    // Lays tokens on an element through the CSSOM, after removing the ones laid there before.
    function lay(target, tokens) {
        target.names.forEach(function (name) { target.element.style.removeProperty(name); });
        target.names = [];
        if (!tokens) {
            return;
        }

        Object.keys(tokens).forEach(function (name) {
            target.element.style.setProperty(name, tokens[name]);
            target.names.push(name);
        });
    }

    function readSnapshot(key) {
        var raw = read(key);
        if (!raw) {
            return null;
        }

        var entry;
        try {
            entry = JSON.parse(raw);
        } catch (error) {
            return null;
        }

        if (!entry || entry.version !== snapshotVersion || appearances[entry.appearance] !== true) {
            return null;
        }

        return {
            key: key,
            appearance: entry.appearance,
            density: densities[entry.density] === true ? entry.density : null,
            backdropMotion: entry.backdropMotion !== false,
            halves: { light: tokensOf(entry.light), dark: tokensOf(entry.dark) },
            attributes: [],
            root: { element: root, names: [] },
            scopes: [],
            scopeObserver: null,
            holdObserver: null,
            scopesReleased: false,
            released: false
        };
    }

    var theme = readSnapshot(data.themeSnapshotKey);

    // Sets a root attribute, remembering the value it had before the boot copy for the release.
    function setOnRoot(name, value) {
        var record = null;
        theme.attributes.forEach(function (entry) {
            if (entry.name === name) {
                record = entry;
            }
        });
        if (!record) {
            record = { name: name, previous: root.getAttribute(name) };
            theme.attributes.push(record);
        }

        record.value = value;
        root.setAttribute(name, value);
    }

    // A scope takes the copy only when it draws the same half: its attributes, rendered by Blazor, win.
    function paintScope(target) {
        var mode = target.element.getAttribute('data-omni-theme');
        var half = resolve(theme.appearance);
        lay(target, appearances[mode] === true && resolve(mode) === half ? theme.halves[half] : null);
        if (mode === 'system') {
            // The markup leaves it out under the system mode; omni-theme.js keeps it from now on.
            target.element.setAttribute('data-omni-theme-resolved', resolve(mode));
        }
    }

    function adoptScopes() {
        var found = document.querySelectorAll('[data-omni-theme-snapshot]');
        for (var i = 0; i < found.length; i++) {
            var element = found[i];
            var known = theme.scopes.some(function (target) { return target.element === element; });
            if (!known && element.getAttribute('data-omni-theme-snapshot') === theme.key) {
                var target = { element: element, names: [] };
                theme.scopes.push(target);
                paintScope(target);
            }
        }
    }

    function paintAll() {
        var half = resolve(theme.appearance);
        setOnRoot('data-omni-theme-resolved', half);
        lay(theme.root, theme.halves[half]);
        if (!theme.scopesReleased) {
            theme.scopes.forEach(paintScope);
        }
    }

    function releaseScopes() {
        if (theme.scopesReleased) {
            return;
        }

        theme.scopesReleased = true;
        if (theme.scopeObserver) {
            theme.scopeObserver.disconnect();
            theme.scopeObserver = null;
        }

        theme.scopes.forEach(function (target) { lay(target, null); });
        theme.scopes = [];
    }

    // Removes the boot copy now: from the scopes, then the root tokens and attributes.
    function releaseTheme() {
        if (!theme || theme.released) {
            return false;
        }

        releaseScopes();
        if (theme.holdObserver) {
            theme.holdObserver.disconnect();
            theme.holdObserver = null;
        }

        theme.released = true;
        lay(theme.root, null);
        theme.attributes.slice().reverse().forEach(function (record) {
            // An attribute something else changed since keeps that value.
            if (root.getAttribute(record.name) !== record.value) {
                return;
            }

            if (record.previous === null) {
                root.removeAttribute(record.name);
            } else {
                root.setAttribute(record.name, record.previous);
            }
        });
        return true;
    }

    function held(selector) {
        try {
            return document.querySelector(selector) !== null;
        } catch (error) {
            // A selector the browser cannot read holds nothing.
            return false;
        }
    }

    // The scope paints from now on: the copy leaves it at once, and the root once nothing holds it.
    function handOverTheme() {
        if (!theme || theme.scopesReleased) {
            return false;
        }

        releaseScopes();
        var hold = typeof data.themeHold === 'string' ? data.themeHold : '.omni-boot-splash';
        if (!hold || !held(hold) || !window.MutationObserver) {
            releaseTheme();
            return true;
        }

        theme.holdObserver = new MutationObserver(function () {
            if (!held(hold)) {
                releaseTheme();
            }
        });
        theme.holdObserver.observe(root, { childList: true, subtree: true });
        return true;
    }

    if (theme) {
        setOnRoot('data-omni-theme', theme.appearance);
        if (theme.density) {
            setOnRoot('data-omni-density', theme.density);
        }

        if (!theme.backdropMotion) {
            setOnRoot('data-omni-backdrop-motion', 'off');
        }

        setOnRoot('data-omni-theme-boot', '');
        paintAll();
        if (window.MutationObserver) {
            theme.scopeObserver = new MutationObserver(adoptScopes);
            theme.scopeObserver.observe(root, { childList: true, subtree: true });
        }

        if (theme.appearance === 'system' && darkQuery !== null) {
            var follow = function () {
                if (theme.released) {
                    darkQuery.removeEventListener('change', follow);
                    return;
                }

                paintAll();
            };
            darkQuery.addEventListener('change', follow);
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

    window.OmniBoot = {
        culture: culture,
        hideSplash: hideSplash,
        handOverTheme: handOverTheme,
        releaseTheme: releaseTheme
    };
})();
