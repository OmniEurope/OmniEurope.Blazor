import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';
import { dotnetRecorder, installDom } from './dom-stub.mjs';

// Storage doubles prove the precedence rules, not browser quota enforcement, which is verified
// separately in a native browser against the module extracted from the package.
class StorageDouble {
    values = new Map();
    refuseWrites = false;
    refuseReads = false;
    get length() { return this.values.size; }
    key(index) { return [...this.values.keys()][index] ?? null; }
    getItem(key) {
        if (this.refuseReads) throw new DOMException('Read refused', 'SecurityError');
        return this.values.get(key) ?? null;
    }
    setItem(key, value) {
        if (this.refuseWrites) throw new DOMException('Quota reached', 'QuotaExceededError');
        this.values.set(key, String(value));
    }
    removeItem(key) { this.values.delete(key); }
    clear() { this.values.clear(); }
}

let sequence = 0;
async function fixture() {
    const storage = new StorageDouble();
    Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: storage });
    const url = pathToFileURL(path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot/omni-preferences.js'));
    const module = await import(`${url.href}?test=${++sequence}`);
    module.set('pref:value', 'OLD');
    return { storage, module };
}

test('get returns the latest accepted value when persistence fails over an old entry', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    module.set('pref:value', 'NEW');
    assert.equal(storage.getItem('pref:value'), 'OLD');
    assert.equal(module.get('pref:value'), 'NEW');
});

test('entries gives the memory fallback precedence and keeps the prefix boundary', async () => {
    const { storage, module } = await fixture();
    module.set('other:value', 'OUTSIDE');
    storage.refuseWrites = true;
    module.set('pref:value', 'NEW');
    module.set('pref:second', 'SECOND');
    assert.deepEqual(new Map(module.entries('pref:')), new Map([['pref:value', 'NEW'], ['pref:second', 'SECOND']]));
});

test('successive failed writes expose the last value rather than either older copy', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    module.set('pref:value', 'FIRST');
    module.set('pref:value', 'SECOND');
    assert.equal(module.get('pref:value'), 'SECOND');
    assert.equal(new Map(module.entries('pref:')).get('pref:value'), 'SECOND');
});

test('a persisted retry releases the memory fallback and external writes remain visible', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    module.set('pref:value', 'PENDING');
    storage.refuseWrites = false;
    module.set('pref:value', 'SAVED');
    assert.equal(storage.getItem('pref:value'), 'SAVED');
    storage.setItem('pref:value', 'EXTERNAL');
    assert.equal(module.get('pref:value'), 'EXTERNAL');
    assert.equal(new Map(module.entries('pref:')).get('pref:value'), 'EXTERNAL');
});

test('native removal and clear after successful persistence do not resurrect a memory value', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    module.set('pref:value', 'PENDING');
    storage.refuseWrites = false;
    module.set('pref:value', 'SAVED');
    storage.removeItem('pref:value');
    assert.equal(module.get('pref:value'), null);
    assert.deepEqual(module.entries('pref:'), []);
    module.set('pref:value', 'AGAIN');
    storage.clear();
    assert.equal(module.get('pref:value'), null);
    assert.deepEqual(module.entries('pref:'), []);
});

test('a deletion tombstone accepted under quota hides the old persisted value', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    const tombstone = JSON.stringify({ Value: null, UpdatedAt: '2026-10-30T10:00:00+00:00', Dirty: true });
    module.set('pref:value', tombstone);
    assert.equal(module.get('pref:value'), tombstone);
    assert.equal(new Map(module.entries('pref:')).get('pref:value'), tombstone);
});

test('a storage read failure still exposes the fallback', async () => {
    const { storage, module } = await fixture();
    storage.refuseWrites = true;
    module.set('pref:value', 'NEW');
    storage.refuseReads = true;
    assert.equal(module.get('pref:value'), 'NEW');
    assert.deepEqual(module.entries('pref:'), [['pref:value', 'NEW']]);
});

// The boot copy (omni-boot.js): kept in the appearance entry, local to the browser, never handed to .NET.
const entry = (value, extra = {}) => JSON.stringify({ Value: value, UpdatedAt: '2026-11-18T10:00:00+00:00', Dirty: false, ...extra });
const bootCopy = JSON.stringify({ version: 1, value: '{"appearance":"Dark"}', appearance: 'dark', light: null, dark: { '--omni-color-surface': '#111111' } });

test('attach lays the boot copy on the stored entry, dated, and hands the boot copy over to the outermost scope', async () => {
    const { storage, module } = await fixture();
    const handOvers = [];
    globalThis.OmniBoot = { handOverTheme: fromPreferences => handOvers.push(fromPreferences) };
    try {
        module.set('omni:pref:local:omni.appearance', entry('{"appearance":"Dark"}'));
        module.attach('omni:pref:local:omni.appearance', bootCopy);
    }
    finally {
        delete globalThis.OmniBoot;
    }

    const stored = JSON.parse(storage.getItem('omni:pref:local:omni.appearance'));
    assert.equal(stored.Value, '{"appearance":"Dark"}');
    assert.equal(stored.Boot.dark['--omni-color-surface'], '#111111');
    assert.equal(typeof stored.Boot.drawnAt, 'number');
    assert.deepEqual(handOvers, [true]);
});

test('get and entries never hand the boot copy to .NET', async () => {
    const { module } = await fixture();
    module.set('omni:pref:local:omni.appearance', entry('{"appearance":"Dark"}'));
    module.attach('omni:pref:local:omni.appearance', bootCopy);

    const read = JSON.parse(module.get('omni:pref:local:omni.appearance'));
    assert.equal(Object.hasOwn(read, 'Boot'), false);
    assert.equal(read.Value, '{"appearance":"Dark"}');
    const listed = JSON.parse(new Map(module.entries('omni:pref:local:')).get('omni:pref:local:omni.appearance'));
    assert.equal(Object.hasOwn(listed, 'Boot'), false);
});

test('a later write of the entry keeps the boot copy: the one attached, or the one a previous page stored', async () => {
    const { storage, module } = await fixture();
    const key = 'omni:pref:local:omni.appearance';
    module.attach(key, bootCopy);
    // Not stored yet: the copy waits for the first write of the entry.
    assert.equal(storage.getItem(key), null);
    module.set(key, entry('{"appearance":"Dark"}', { Dirty: true }));
    assert.equal(JSON.parse(storage.getItem(key)).Boot.appearance, 'dark');
    module.set(key, entry('{"appearance":"Dark"}'));
    assert.equal(JSON.parse(storage.getItem(key)).Dirty, false);
    assert.equal(JSON.parse(storage.getItem(key)).Boot.appearance, 'dark');

    // A new page (a fresh module) that never attached keeps what the previous one stored.
    const url = pathToFileURL(path.resolve(import.meta.dirname, '../../src/OmniEurope.Blazor/wwwroot/omni-preferences.js'));
    const next = await import(`${url.href}?test=${++sequence}`);
    next.set(key, entry('{"appearance":"Light"}'));
    assert.equal(JSON.parse(storage.getItem(key)).Value, '{"appearance":"Light"}');
    assert.equal(JSON.parse(storage.getItem(key)).Boot.appearance, 'dark', 'kept: the boot script compares the look it was drawn for');
});

test('a boot copy the quota refuses leaves the entry written without it, and a malformed copy is ignored', async () => {
    const { storage, module } = await fixture();
    const key = 'omni:pref:local:omni.appearance';
    module.attach(key, '{not json');
    module.set(key, entry('{"appearance":"Dark"}'));
    assert.equal(Object.hasOwn(JSON.parse(storage.getItem(key)), 'Boot'), false);

    module.attach(key, bootCopy);
    const original = storage.setItem.bind(storage);
    storage.setItem = (name, value) => {
        if (String(value).includes('"Boot"')) throw new DOMException('Quota reached', 'QuotaExceededError');
        original(name, value);
    };
    module.set(key, entry('{"appearance":"Light"}'));
    const stored = JSON.parse(storage.getItem(key));
    assert.equal(stored.Value, '{"appearance":"Light"}');
    assert.equal(Object.hasOwn(stored, 'Boot'), false);
    assert.equal(module.get(key), entry('{"appearance":"Light"}'), 'persisted, not kept in memory');
});

// The reconnection display of Blazor Server, as the host's own element (classes documented by ASP.NET
// Core) or the default display (an open shadow root holding the overlay), and a MutationObserver double
// that a test fires by hand. The send failure itself happens inside blazor.web.js and cannot be caught
// here; what is proved is that no call is made while the display shows the connection as lost.
async function watchFixture({ defaultDisplay = false } = {}) {
    const dom = installDom();
    const observers = [];
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; this.target = null; observers.push(this); }
        observe(target) { this.target = target; }
        disconnect() { this.target = null; }
    };
    const host = dom.el('div', { id: 'components-reconnect-modal' });
    let indicator = host;
    if (defaultDisplay) {
        indicator = dom.el('div', { class: 'components-reconnect-overlay' });
        host.shadowRoot = dom.el('div', {}, indicator);
    }
    dom.body.append(host);
    document.visibilityState = 'visible';
    const { storage, module } = await fixture();
    const dotnet = dotnetRecorder();
    const id = module.watch(dotnet, 'pref:');
    const fireObservers = () => observers.filter(observer => observer.target).forEach(observer => observer.callback([]));
    return { dom, storage, module, dotnet, id, indicator, observers, fireObservers };
}

test('a tab shown while the connection is lost sends nothing, then reports once the connection is back', async () => {
    const { dom, dotnet, indicator, fireObservers } = await watchFixture();
    indicator.classList.add('components-reconnect-show');
    dom.dispatch('visibilitychange');
    dom.dispatch('visibilitychange');
    assert.deepEqual(dotnet.calls, []);

    indicator.classList.remove('components-reconnect-show');
    indicator.classList.add('components-reconnect-retrying');
    fireObservers();
    assert.deepEqual(dotnet.calls, []);

    indicator.classList.remove('components-reconnect-retrying');
    indicator.classList.add('components-reconnect-hide');
    fireObservers();
    assert.deepEqual(dotnet.calls, [['OnVisible']]);
});

test('entries written by another tab during the outage are reported once each after reconnection', async () => {
    const { dom, storage, dotnet, indicator, fireObservers } = await watchFixture();
    indicator.classList.add('components-reconnect-failed');
    for (const key of ['pref:a', 'pref:b', 'pref:a', 'other:c']) {
        dom.dispatch('storage', { key, storageArea: storage });
    }
    assert.deepEqual(dotnet.calls, []);

    indicator.classList.remove('components-reconnect-failed');
    indicator.classList.add('components-reconnect-hide');
    fireObservers();
    assert.deepEqual(dotnet.calls, [['OnStored', 'pref:a'], ['OnStored', 'pref:b']]);
});

test('the default reconnection display is read through its shadow overlay', async () => {
    const { dom, dotnet, indicator, fireObservers } = await watchFixture({ defaultDisplay: true });
    indicator.classList.add('components-reconnect-visible');
    dom.dispatch('visibilitychange');
    assert.deepEqual(dotnet.calls, []);

    indicator.classList.remove('components-reconnect-visible');
    fireObservers();
    assert.deepEqual(dotnet.calls, [['OnVisible']]);
});

test('a report made once the connection is up also flushes what waited, and stops the waiting', async () => {
    const { dom, dotnet, indicator, observers, fireObservers } = await watchFixture();
    indicator.classList.add('components-reconnect-show');
    dom.dispatch('visibilitychange');
    indicator.classList.remove('components-reconnect-show');
    dom.dispatch('visibilitychange');
    assert.deepEqual(dotnet.calls, [['OnVisible']]);
    assert.ok(observers.every(observer => observer.target === null));
    fireObservers();
    assert.deepEqual(dotnet.calls, [['OnVisible']]);
});

test('unwatch during the outage drops the waiting reports', async () => {
    const { dom, module, dotnet, id, indicator, fireObservers } = await watchFixture();
    indicator.classList.add('components-reconnect-show');
    dom.dispatch('visibilitychange');
    module.unwatch(id);
    indicator.classList.remove('components-reconnect-show');
    fireObservers();
    assert.deepEqual(dotnet.calls, []);
});

test('without a reconnection display (WebAssembly, Hybrid) every report goes out at once', async () => {
    const dom = installDom();
    document.visibilityState = 'visible';
    const { module } = await fixture();
    const dotnet = dotnetRecorder();
    module.watch(dotnet, 'pref:');
    dom.dispatch('visibilitychange');
    assert.deepEqual(dotnet.calls, [['OnVisible']]);
});
