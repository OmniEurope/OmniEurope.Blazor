// Browser cache of OmniPreferences.
//
// The script only keeps strings: the key format and the entry encoding belong to .NET
// (Internal/PreferenceCache.cs). When the browser refuses localStorage (blocked site data, a private
// window that throws, a full quota) the entries live in memory for the life of the page, and nothing
// throws back into .NET.
const memory = new Map();
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

export function get(key) {
    const area = local();
    if (area) {
        try {
            const value = area.getItem(key);
            if (value !== null) {
                return value;
            }
        } catch {
            // Read refused: the memory copy answers.
        }
    }

    return memory.has(key) ? memory.get(key) : null;
}

export function set(key, value) {
    const area = local();
    if (area) {
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

// Every entry whose key starts with the prefix, as [key, value] pairs; the stored copy wins over memory.
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
                if (value !== null) {
                    found.set(key, value);
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
