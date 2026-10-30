import assert from 'node:assert/strict';
import path from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

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
