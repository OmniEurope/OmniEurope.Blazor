// Browser cache of OmniPreferences.
//
// The script only keeps strings: the key format and the entry encoding belong to .NET
// (Internal/PreferenceCache.cs). When the browser refuses localStorage (blocked site data, a private
// window that throws, a full quota) the entries live in memory for the life of the page, and nothing
// throws back into .NET.
//
// One member of an entry belongs to this script alone: `Boot`, the boot copy the outermost OmniThemeScope
// attaches to the appearance entry (attach) for omni-boot.js to replay before Blazor starts. It is local to
// this browser: never handed to .NET (get and entries leave it out), never sent to the store, and kept
// through every later write of the entry, so a synchronization or a confirmation does not drop it.
const memory = new Map();
const bootMember = 'Boot';
// The boot copies attached by this page, laid again on every write of their entry.
const boots = new Map();
const watchers = new Map();
let nextWatcher = 1;
let storage;

// The storage is probed once: reading the property itself throws when site data is blocked.
function local() {
    if (storage === undefined) {
        try {
            const candidate = globalThis.localStorage;
            const probe = 'omni:pref-probe';
            candidate.setItem(probe, probe);
            candidate.removeItem(probe);
            storage = candidate;
        } catch {
            storage = null;
        }
    }

    return storage;
}

// The entry as an object, or null when it is not one (a value written by another version, by hand).
function entryOf(value) {
    if (typeof value !== 'string') {
        return null;
    }

    try {
        const entry = JSON.parse(value);
        return entry && typeof entry === 'object' && !Array.isArray(entry) ? entry : null;
    } catch {
        return null;
    }
}

// The entry without its boot copy, as .NET reads it.
function withoutBoot(value) {
    if (typeof value !== 'string' || !value.includes(`"${bootMember}"`)) {
        return value;
    }

    const entry = entryOf(value);
    if (!entry || !Object.hasOwn(entry, bootMember)) {
        return value;
    }

    delete entry[bootMember];
    return JSON.stringify(entry);
}

function bootOf(value) {
    if (typeof value !== 'string' || !value.includes(`"${bootMember}"`)) {
        return undefined;
    }

    const boot = entryOf(value)?.[bootMember];
    return boot && typeof boot === 'object' ? boot : undefined;
}

function withBoot(value, boot) {
    const entry = entryOf(value);
    if (!entry) {
        return value;
    }

    entry[bootMember] = boot;
    return JSON.stringify(entry);
}

function stored(area, key) {
    try {
        return area.getItem(key);
    } catch {
        return null;
    }
}

export function get(key) {
    if (memory.has(key)) {
        return memory.get(key);
    }

    const area = local();
    if (area) {
        try {
            const value = area.getItem(key);
            if (value !== null) {
                return withoutBoot(value);
            }
        } catch {
            // Read refused: the key reads as absent.
        }
    }

    return null;
}

export function set(key, value) {
    const area = local();
    if (area) {
        // The boot copy of the entry stays: the one this page attached, or the one already stored.
        const boot = boots.get(key) ?? bootOf(stored(area, key));
        if (boot !== undefined) {
            try {
                area.setItem(key, withBoot(value, boot));
                memory.delete(key);
                return;
            } catch {
                // The copy made the entry too large for the quota: the entry is written without it below.
            }
        }

        try {
            area.setItem(key, value);
            memory.delete(key);
            return;
        } catch {
            // Quota reached or storage refused: the entry is kept in memory instead.
        }
    }

    memory.set(key, value);
}

// Attaches the boot copy of the look the outermost theme scope painted (a JSON object written by
// OmniThemeScope) to the stored entry of the appearance, dated by this device's clock, then lets the scope
// take over from the copy omni-boot.js laid. An entry not stored yet takes it at its first write. A storage
// that refuses, or a quota the copy would exceed, keeps the entry as it was: the next boot then shows the
// attributes the preference gives on its own.
export function attach(key, boot) {
    const copy = entryOf(boot);
    if (copy) {
        copy.drawnAt = Date.now();
        boots.set(key, copy);
        const area = local();
        const current = area ? stored(area, key) : null;
        if (current !== null) {
            try {
                area.setItem(key, withBoot(current, copy));
            } catch {
                // Quota reached: the entry stays without the copy.
            }
        }
    }

    const omniBoot = globalThis.OmniBoot;
    if (omniBoot && typeof omniBoot.handOverTheme === 'function') {
        omniBoot.handOverTheme(true);
    }
}

// Every entry whose key starts with the prefix, as [key, value] pairs; an unpersisted memory copy wins.
export function entries(prefix) {
    const found = new Map();
    for (const [key, value] of memory) {
        if (key.startsWith(prefix)) {
            found.set(key, value);
        }
    }

    const area = local();
    if (area) {
        try {
            for (let index = 0; index < area.length; index++) {
                const key = area.key(index);
                const value = key !== null && key.startsWith(prefix) ? area.getItem(key) : null;
                if (value !== null && !found.has(key)) {
                    found.set(key, withoutBoot(value));
                }
            }
        } catch {
            // Enumeration refused: the memory entries are all there is.
        }
    }

    return Array.from(found);
}

function notify(dotnet, method, ...args) {
    // Rejected only when the circuit is gone, which leaves nobody to tell.
    dotnet.invokeMethodAsync(method, ...args).catch(() => { });
}

// Reports the tab coming back into view, and another tab writing an entry of the prefix.
export function watch(dotnet, prefix) {
    const id = nextWatcher++;
    const onVisible = () => {
        if (document.visibilityState === 'visible') {
            notify(dotnet, 'OnVisible');
        }
    };
    const onStorage = event => {
        if (event.key && event.key.startsWith(prefix) && event.storageArea === local()) {
            notify(dotnet, 'OnStored', event.key);
        }
    };
    document.addEventListener('visibilitychange', onVisible);
    window.addEventListener('storage', onStorage);
    watchers.set(id, { onVisible, onStorage });
    return id;
}

export function unwatch(id) {
    const watcher = watchers.get(id);
    if (!watcher) {
        return;
    }

    document.removeEventListener('visibilitychange', watcher.onVisible);
    window.removeEventListener('storage', watcher.onStorage);
    watchers.delete(id);
}
