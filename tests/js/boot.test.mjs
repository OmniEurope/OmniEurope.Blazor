import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { test } from 'node:test';
import vm from 'node:vm';

// omni-boot.js is a plain script a host puts in the head of its page, before Blazor starts. Its logic is
// proved here (the showcase loads it, and the scripts probe proves its first frames in a real browser): the
// script runs in a fresh context per test, against a page made of the few objects it reads.
const source = fs.readFileSync(path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot/omni-boot.js'), 'utf8');

class FakeElement {
    constructor(attributes = {}) {
        this.attributes = new Map(Object.entries(attributes));
        this.tokens = new Map();
        this.lang = '';
        this.style = {
            setProperty: (name, value) => this.tokens.set(name, value),
            removeProperty: name => this.tokens.delete(name)
        };
    }

    // The theme scopes nested in another one name it; the outermost has none.
    parentElement = null;

    closest(selector) {
        for (let element = this; element; element = element.parentElement) {
            if (selector === '.omni-theme-scope' && element.classes?.includes('omni-theme-scope')) {
                return element;
            }
        }

        return null;
    }

    getAttribute(name) { return this.attributes.get(name) ?? null; }
    setAttribute(name, value) { this.attributes.set(name, String(value)); }
    removeAttribute(name) { this.attributes.delete(name); }
}

// A page: the data attributes of the script tag, the stored values, the elements a selector finds, the
// colour scheme the system prefers. Returns what a test drives and reads.
const boot = ({ data = {}, stored = {}, elements = [], dark = false, storage = 'ok', language = 'fr-BE', observers = true } = {}) => {
    const root = new FakeElement();
    const page = { elements: [...elements], observers: [], schemeListeners: [] };
    const darkQuery = {
        matches: dark,
        addEventListener: (type, listener) => page.schemeListeners.push(listener),
        removeEventListener: (type, listener) => { page.schemeListeners = page.schemeListeners.filter(item => item !== listener); }
    };
    const select = selector => {
        if (selector === '[invalid') {
            throw new SyntaxError('not a selector');
        }

        const attribute = /^\[([\w-]+)\]$/.exec(selector)?.[1];
        const className = /^\.([\w-]+)$/.exec(selector)?.[1];
        return page.elements.filter(element => attribute ? element.attributes.has(attribute) : element.classes?.includes(className));
    };
    const window = {
        localStorage: {
            getItem: key => {
                if (storage === 'blocked') {
                    throw new Error('SecurityError');
                }

                return Object.hasOwn(stored, key) ? stored[key] : null;
            },
            get length() {
                if (storage === 'blocked') {
                    throw new Error('SecurityError');
                }

                return Object.keys(stored).length;
            },
            key: index => Object.keys(stored)[index] ?? null
        },
        matchMedia: () => darkQuery
    };
    if (observers) {
        window.MutationObserver = class {
            constructor(callback) { this.callback = callback; this.connected = false; page.observers.push(this); }
            observe() { this.connected = true; }
            disconnect() { this.connected = false; }
        };
    }

    // In a page, window is the global object: the script reads MutationObserver both ways.
    const context = Object.assign(window, {
        window,
        navigator: { language },
        document: {
            currentScript: { dataset: data },
            documentElement: root,
            querySelector: selector => select(selector)[0] ?? null,
            querySelectorAll: select
        }
    });
    vm.runInNewContext(source, context);
    return {
        root,
        page,
        api: window.OmniBoot,
        // Runs the callbacks of the observers still connected, as a mutation of the page would.
        mutate: () => page.observers.filter(observer => observer.connected).forEach(observer => observer.callback()),
        prefer: scheme => { darkQuery.matches = scheme === 'dark'; page.schemeListeners.slice().forEach(listener => listener()); }
    };
};

const snapshot = (overrides = {}) => JSON.stringify({
    version: 1,
    appearance: 'dark',
    density: 'compact',
    backdropMotion: false,
    textSize: 7,
    controlSize: 5,
    light: { '--omni-color-surface': '#ffffff' },
    dark: { '--omni-color-surface': '#1e1e1e', '--omni-color-text': '#f5f5f5' },
    ...overrides
});

test('the plain keys set the appearance, the preset, the levels and the language before the first paint', () => {
    const { root, api } = boot({
        data: { appearanceKey: 'a', themeKey: 't', textSizeKey: 's', controlSizeKey: 'c', languageKey: 'l' },
        stored: { a: 'dark', t: 'Givre', s: '8', c: '3', l: 'nl-BE' }
    });

    assert.equal(root.getAttribute('data-omni-theme'), 'dark');
    assert.equal(root.getAttribute('data-omni-theme-preset'), 'Givre');
    assert.equal(root.getAttribute('data-oe-text-size'), '8');
    assert.equal(root.getAttribute('data-oe-control-size'), '3');
    assert.equal(root.lang, 'nl-BE');
    assert.equal(api.culture, 'nl-BE');
    assert.equal(api.handOverTheme(), false, 'no snapshot key: nothing to hand over');
    assert.equal(api.releaseTheme(), false);
});

test('a value that is not well formed is ignored, and the browser language stands in for a bad saved one', () => {
    const { root, api } = boot({
        data: { appearanceKey: 'a', themeKey: 't', textSizeKey: 's', languageKey: 'l' },
        stored: { a: 'purple', t: 'bad name!', s: '11', l: '<script>' }
    });

    assert.equal(root.getAttribute('data-omni-theme'), null);
    assert.equal(root.getAttribute('data-omni-theme-preset'), null);
    assert.equal(root.getAttribute('data-oe-text-size'), null);
    assert.equal(api.culture, null);
    assert.equal(root.lang, 'fr-BE');
});

test('blocked site data leaves the page to its defaults, and no language key leaves the lang alone', () => {
    const { root, api } = boot({ data: { appearanceKey: 'a', themeSnapshotKey: 'snap' }, stored: { a: 'dark' }, storage: 'blocked' });

    assert.equal(root.getAttribute('data-omni-theme'), null);
    assert.equal(root.lang, '');
    assert.equal(api.culture, null);
});

test('a snapshot paints the root in the stored look: attributes, levels and the tokens of the half the mode resolves to', () => {
    const { root } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot() } });

    assert.equal(root.getAttribute('data-omni-theme'), 'dark');
    assert.equal(root.getAttribute('data-omni-theme-resolved'), 'dark');
    assert.equal(root.getAttribute('data-omni-density'), 'compact');
    assert.equal(root.getAttribute('data-omni-backdrop-motion'), 'off');
    assert.equal(root.getAttribute('data-omni-theme-boot'), '');
    assert.equal(root.getAttribute('data-oe-text-size'), '7');
    assert.equal(root.getAttribute('data-oe-control-size'), null, 'the default level 5 leaves the attribute out');
    assert.deepEqual([...root.tokens], [['--omni-color-surface', '#1e1e1e'], ['--omni-color-text', '#f5f5f5']]);
});

test('a corrupt or foreign snapshot paints nothing, and a half with one refused token is dropped whole', () => {
    const refused = [
        '{not json',
        snapshot({ version: 2 }),
        snapshot({ appearance: 'sepia' }),
        'x'.repeat(262145)
    ];
    for (const raw of refused) {
        const { root } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: raw } });
        assert.equal(root.getAttribute('data-omni-theme-boot'), null, raw.slice(0, 20));
    }

    const tooMany = Object.fromEntries(Array.from({ length: 257 }, (_, index) => [`--omni-t${index}`, '#000']));
    for (const dark of [{ '--omni-x': 'url(https://example.test/a.png)' }, { 'color': 'red' }, { '--omni-x': 3 }, [], tooMany, {}]) {
        const { root } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot({ dark, density: 'huge', backdropMotion: true }) } });
        assert.equal(root.getAttribute('data-omni-theme-boot'), '', 'the attributes still apply');
        assert.equal(root.getAttribute('data-omni-density'), null, 'an unknown density is left out');
        assert.equal(root.getAttribute('data-omni-backdrop-motion'), null);
        assert.equal(root.tokens.size, 0);
    }
});

test('a scope with the same snapshot key takes the copy only when it draws the same half', () => {
    const same = new FakeElement({ 'data-omni-theme-snapshot': 'snap', 'data-omni-theme': 'system' });
    const other = new FakeElement({ 'data-omni-theme-snapshot': 'snap', 'data-omni-theme': 'light' });
    const foreign = new FakeElement({ 'data-omni-theme-snapshot': 'another', 'data-omni-theme': 'dark' });
    const { page, mutate } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot() }, dark: true });

    page.elements.push(same, other, foreign);
    mutate();
    mutate();

    assert.equal(same.tokens.get('--omni-color-surface'), '#1e1e1e');
    assert.equal(same.getAttribute('data-omni-theme-resolved'), 'dark');
    assert.equal(other.tokens.size, 0);
    assert.equal(foreign.tokens.size, 0);
});

test('the hand-over releases the scopes at once and the root once the splash is gone, restoring what the root had', () => {
    const splash = Object.assign(new FakeElement(), { classes: ['omni-boot-splash'] });
    const scope = new FakeElement({ 'data-omni-theme-snapshot': 'snap', 'data-omni-theme': 'dark' });
    const { root, page, api, mutate } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot() }, elements: [splash, scope] });
    mutate();
    assert.equal(scope.tokens.size, 2);
    // Something else changes an attribute the copy set: that value stays after the release.
    root.setAttribute('data-omni-density', 'spacious');

    assert.equal(api.handOverTheme(), true);
    assert.equal(scope.tokens.size, 0);
    assert.equal(root.getAttribute('data-omni-theme-boot'), '', 'the splash still holds the root copy');
    assert.equal(api.handOverTheme(), false, 'handed over once');

    mutate();
    assert.equal(root.getAttribute('data-omni-theme-boot'), '', 'a mutation that leaves the splash changes nothing');
    page.elements.splice(page.elements.indexOf(splash), 1);
    mutate();

    assert.equal(root.getAttribute('data-omni-theme-boot'), null);
    assert.equal(root.getAttribute('data-omni-theme'), null);
    assert.equal(root.getAttribute('data-omni-density'), 'spacious');
    assert.equal(root.tokens.size, 0);
    assert.equal(api.releaseTheme(), false, 'released once');
});

test('a hold that matches nothing, an empty hold, a selector the browser cannot read or no observer release at once', () => {
    const cases = [
        { themeHold: '#nothing-here' },
        { themeHold: '' },
        { themeHold: '[invalid' }
    ];
    for (const extra of cases) {
        const { root, api } = boot({ data: { themeSnapshotKey: 'snap', ...extra }, stored: { snap: snapshot() } });
        assert.equal(api.handOverTheme(), true);
        assert.equal(root.getAttribute('data-omni-theme-boot'), null, JSON.stringify(extra));
    }

    const splash = Object.assign(new FakeElement(), { classes: ['omni-boot-splash'] });
    const { root, api } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot() }, elements: [splash], observers: false });
    assert.equal(api.handOverTheme(), true);
    assert.equal(root.getAttribute('data-omni-theme-boot'), null);
});

test('a root attribute the copy found already set gets its previous value back on release', () => {
    const { root, api } = boot({ data: { themeSnapshotKey: 'snap', appearanceKey: 'a' }, stored: { a: 'light', snap: snapshot() } });
    assert.equal(root.getAttribute('data-omni-theme'), 'dark');

    assert.equal(api.releaseTheme(), true);

    assert.equal(root.getAttribute('data-omni-theme'), 'light');
});

test('under the system mode the copy follows the colour scheme until it is released', () => {
    const { root, page, api, prefer } = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: snapshot({ appearance: 'system' }) } });
    assert.equal(root.getAttribute('data-omni-theme-resolved'), 'light');
    assert.equal(root.tokens.get('--omni-color-surface'), '#ffffff');

    prefer('dark');
    assert.equal(root.getAttribute('data-omni-theme-resolved'), 'dark');
    assert.equal(root.tokens.get('--omni-color-surface'), '#1e1e1e');

    api.releaseTheme();
    prefer('light');
    assert.equal(page.schemeListeners.length, 0, 'the listener leaves at the first change after the release');
    assert.equal(root.tokens.size, 0);
});

// ---- The package's own copy: the appearance entry of the OmniPreferences cache ----

const look = (overrides = {}) => ({
    version: 1, appearance: 'Dark', preset: 'Halo', palette: null, font: null, backdrop: null,
    density: 'Compact', backdropMotion: true, textSize: 7, controlSize: 3, fullWidth: false, ...overrides
});
// A cache entry as PreferenceCache writes it, with the boot copy omni-preferences.js keeps beside it.
const cached = ({ value = look(), boot = {}, updatedAt = '2026-11-18T10:00:00+00:00', withBoot = true } = {}) => JSON.stringify({
    Value: value === null ? null : JSON.stringify(value),
    UpdatedAt: updatedAt,
    Dirty: false,
    ...(withBoot ? {
        Boot: {
            version: 1, value: JSON.stringify(value ?? look()), appearance: 'dark', density: 'compact', backdropMotion: true,
            textSize: 7, controlSize: 3, drawnAt: Date.parse('2026-11-18T11:00:00Z'),
            light: { '--omni-color-surface': '#fafafa' }, dark: { '--omni-color-surface': '#101820', '--omni-color-text': '#eeeeee' },
            ...boot
        }
    } : {})
});
const scopeElement = (attributes, parent = null) => Object.assign(new FakeElement(attributes), { classes: ['omni-theme-scope'], parentElement: parent });
const appearanceKey = 'omni:pref:local:omni.appearance';

test('without any attribute, the appearance the cache holds is laid on the root before the first paint, tokens included', () => {
    const { root } = boot({ stored: { 'omni:pref:user-1:omni.appearance': cached(), 'omni:pref:user-1:omni.grid.x': '{"Value":"{}"}' } });

    assert.equal(root.getAttribute('data-omni-theme'), 'dark');
    assert.equal(root.getAttribute('data-omni-theme-resolved'), 'dark');
    assert.equal(root.getAttribute('data-omni-density'), 'compact');
    assert.equal(root.getAttribute('data-omni-backdrop-motion'), null);
    assert.equal(root.getAttribute('data-omni-theme-boot'), '');
    assert.equal(root.getAttribute('data-oe-text-size'), '7');
    assert.equal(root.getAttribute('data-oe-control-size'), '3');
    assert.deepEqual([...root.tokens], [['--omni-color-surface', '#101820'], ['--omni-color-text', '#eeeeee']]);
});

test('the copy paints the outermost theme scope only, when it draws the same half', () => {
    const outer = scopeElement({ 'data-omni-theme': 'dark' });
    const inner = scopeElement({ 'data-omni-theme': 'dark' }, outer);
    const { page, mutate } = boot({ stored: { [appearanceKey]: cached() } });

    page.elements.push(inner, outer);
    mutate();

    assert.equal(outer.tokens.get('--omni-color-surface'), '#101820');
    assert.equal(inner.tokens.size, 0, 'a nested scope draws its own look');

    const light = scopeElement({ 'data-omni-theme': 'light' });
    const other = boot({ stored: { [appearanceKey]: cached() } });
    other.page.elements.push(light);
    other.mutate();
    assert.equal(light.tokens.size, 0, 'a scope drawn in the other half keeps its own');
});

test('a copy drawn for another look, or no copy at all, leaves the tokens out and takes the attributes from the preference', () => {
    const changed = look({ appearance: 'light', density: 'spacious', backdropMotion: false, textSize: 9, controlSize: 5 });
    for (const raw of [cached({ value: changed, boot: { value: JSON.stringify(look()) } }), cached({ value: changed, withBoot: false })]) {
        const { root } = boot({ stored: { [appearanceKey]: raw } });
        assert.equal(root.getAttribute('data-omni-theme'), 'light');
        assert.equal(root.getAttribute('data-omni-theme-resolved'), 'light');
        assert.equal(root.getAttribute('data-omni-density'), 'spacious');
        assert.equal(root.getAttribute('data-omni-backdrop-motion'), 'off');
        assert.equal(root.getAttribute('data-oe-text-size'), '9');
        assert.equal(root.getAttribute('data-oe-control-size'), null);
        assert.equal(root.tokens.size, 0);
    }
});

test('names are compared without case, as the state reads them, and the content width does not matter', () => {
    const drawn = look({ appearance: 'dark', density: 'compact', fullWidth: true });
    const { root } = boot({ stored: { [appearanceKey]: cached({ value: look(), boot: { value: JSON.stringify(drawn) } }) } });
    assert.equal(root.tokens.get('--omni-color-surface'), '#101820');
});

test('a removed, malformed or foreign entry lays nothing, and an unknown setting keeps its default', () => {
    const refused = [
        cached({ value: null }),
        '{not json',
        JSON.stringify({ Value: '{not json', UpdatedAt: 'x' }),
        JSON.stringify({ Value: 42 }),
        JSON.stringify([1, 2]),
        'x'.repeat(262144 + 16385)
    ];
    for (const raw of refused) {
        const { root, api } = boot({ stored: { [appearanceKey]: raw } });
        assert.equal(root.getAttribute('data-omni-theme-boot'), null, raw.slice(0, 30));
        assert.equal(api.handOverTheme(true), false);
    }

    const odd = boot({ stored: { [appearanceKey]: cached({ value: look({ appearance: 'Sepia', density: 'Huge', textSize: 12, controlSize: '4' }), withBoot: false }) } });
    assert.equal(odd.root.getAttribute('data-omni-theme'), 'system');
    assert.equal(odd.root.getAttribute('data-omni-density'), null);
    assert.equal(odd.root.getAttribute('data-oe-text-size'), null);

    const corrupt = boot({ stored: { [appearanceKey]: cached({ boot: { dark: { '--omni-x': 'url(https://example.test/a.png)' } } }) } });
    assert.equal(corrupt.root.getAttribute('data-omni-theme'), 'dark', 'the attributes still apply');
    assert.equal(corrupt.root.tokens.size, 0, 'a half with a refused token is dropped whole');
});

test('with several users in the browser, the entry drawn or changed last is laid', () => {
    const { root } = boot({
        stored: {
            'omni:pref:old-user:omni.appearance': cached({ boot: { drawnAt: Date.parse('2026-11-01T00:00:00Z') }, updatedAt: '2026-11-01T00:00:00+00:00' }),
            'omni:pref:new-user:omni.appearance': cached({ value: look({ appearance: 'Light', density: 'Spacious' }), withBoot: false, updatedAt: '2026-11-18T12:00:00+00:00' })
        }
    });

    assert.equal(root.getAttribute('data-omni-theme'), 'light');
    assert.equal(root.getAttribute('data-omni-density'), 'spacious');
});

test('a valid host snapshot wins over the package copy, which the outermost scope then cannot hand over', () => {
    const { root, api } = boot({
        data: { themeSnapshotKey: 'snap' },
        stored: { snap: snapshot({ density: 'spacious' }), [appearanceKey]: cached() }
    });

    assert.equal(root.getAttribute('data-omni-density'), 'spacious');
    assert.equal(api.handOverTheme(true), false, 'the host snapshot belongs to its own scope');
    assert.equal(api.settleTheme(), false);
    assert.equal(api.handOverTheme(), true);

    const fallback = boot({ data: { themeSnapshotKey: 'snap' }, stored: { snap: '{broken', [appearanceKey]: cached() } });
    assert.equal(fallback.root.getAttribute('data-omni-density'), 'compact', 'an unusable host snapshot falls back to the package copy');
});

test('the outermost scope hands the package copy over: the scope at once, the root once the splash is gone', () => {
    const splash = Object.assign(new FakeElement(), { classes: ['omni-boot-splash'] });
    const scope = scopeElement({ 'data-omni-theme': 'dark' });
    const { root, page, api, mutate } = boot({ stored: { [appearanceKey]: cached() }, elements: [splash, scope] });
    mutate();
    assert.equal(scope.tokens.size, 2);

    assert.equal(api.handOverTheme(true), true);
    assert.equal(scope.tokens.size, 0);
    assert.equal(root.getAttribute('data-omni-theme-boot'), '');
    page.elements.splice(page.elements.indexOf(splash), 1);
    mutate();
    assert.equal(root.getAttribute('data-omni-theme-boot'), null);
    assert.equal(root.tokens.size, 0);
    assert.equal(root.getAttribute('data-oe-text-size'), '7', 'the levels stay for good, as the scope writes the same');
});

test('the leaving splash takes the package copy with it, scope included; without a splash it waits for the scope', () => {
    const splash = Object.assign(new FakeElement(), { classes: ['omni-boot-splash'] });
    const scope = scopeElement({ 'data-omni-theme': 'dark' });
    const { root, page, api, mutate } = boot({ stored: { [appearanceKey]: cached() }, elements: [splash, scope] });
    mutate();

    assert.equal(api.settleTheme(), true);
    assert.equal(scope.tokens.size, 2, 'held while the splash fades');
    page.elements.splice(page.elements.indexOf(splash), 1);
    mutate();
    assert.equal(scope.tokens.size, 0);
    assert.equal(root.getAttribute('data-omni-theme-boot'), null);
    assert.equal(api.settleTheme(), false, 'released once');

    const bare = boot({ stored: { [appearanceKey]: cached() } });
    assert.equal(bare.api.settleTheme(), false);
    assert.equal(bare.root.getAttribute('data-omni-theme-boot'), '');
});

test('a storage that throws on every access, the length included, leaves the page to its defaults without an error', () => {
    const { root, api } = boot({ stored: { [appearanceKey]: cached() }, storage: 'blocked' });

    assert.equal(root.getAttribute('data-omni-theme'), null);
    assert.equal(root.getAttribute('data-omni-theme-boot'), null);
    assert.equal(api.handOverTheme(true), false);
    assert.equal(api.settleTheme(), false);
});

test('nothing is ever written: the boot script only reads', () => {
    assert.equal(/setItem|removeItem|\.clear\(/.test(source), false);
});
