using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>One cached preference: its JSON value (null for a tombstone), its date, and whether the store has yet to confirm it.</summary>
internal sealed record PreferenceCacheEntry(string? Value, DateTimeOffset UpdatedAt, bool Dirty);

/// <summary>
/// The browser cache of <see cref="Components.OmniPreferences"/>: one <c>localStorage</c> entry per
/// preference under <c>omni:pref:&lt;scope&gt;:&lt;key&gt;</c>, and the date of the last synchronization
/// under <c>omni:pref-sync:&lt;scope&gt;</c>, and the moment (this device's clock) the last synchronization
/// completed under <c>omni:pref-synced:&lt;scope&gt;</c>, read and written through <c>omni-preferences.js</c>.
/// The key format and the entry encoding live here; the script only reads and writes strings, and falls back
/// to memory when the browser refuses its storage.
/// </summary>
/// <remarks>
/// A cache of a signed-in user may have a <paramref name="mirror"/>, the cache of the signed-out visitor
/// (<see cref="Components.OmniSignedOutPreferences.Browser"/>): every entry written here is copied there, confirmed, so the browser
/// keeps the account's last values after sign-out. The visitor's own entries are written unconfirmed (dirty): they
/// have yet to be merged into an account.
/// </remarks>
internal sealed class PreferenceCache(IJSObjectReference module, string scope, bool isVisitor = false, PreferenceCache? mirror = null)
{
    /// <summary>The scope of the signed-out visitor's cache; a store may not use it for a user in <see cref="Components.OmniSignedOutPreferences.Browser"/> mode.</summary>
    internal const string VisitorScope = "visitor";

    internal const string KeyPrefix = "omni:pref:";
    internal const string SyncPrefix = "omni:pref-sync:";
    internal const string SyncedPrefix = "omni:pref-synced:";

    /// <summary>The scope (the user's partition) this cache belongs to.</summary>
    internal string Scope { get; } = scope;

    /// <summary>Whether this is the signed-out visitor's cache: it never talks to the store.</summary>
    internal bool IsVisitor { get; } = isVisitor;

    /// <summary>What the store answers as its scope while this cache is the one in use: the user's scope, or <see cref="Components.OmniPreferences.NoUserScope"/> for the visitor.</summary>
    internal string StoreScope { get; } = isVisitor ? Components.OmniPreferences.NoUserScope : scope;

    /// <summary>The visitor's cache that copies every write of this one; null when there is none.</summary>
    internal PreferenceCache? Mirror { get; } = mirror;

    /// <summary>The prefix of every entry of this scope.</summary>
    internal string Prefix { get; } = KeyPrefix + scope + ":";

    /// <summary>
    /// The keys this opening has read once from the store (guarded by the owner's lock): each opening, so each user,
    /// asks the store anew, and a reader that retries on a newer opening is never taken for one already refreshed.
    /// </summary>
    internal HashSet<string> Refreshed { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The first read of a key not cached yet, while the store answers (guarded by the owner's lock): a read of the same
    /// key made meanwhile on this opening waits for it instead of taking the empty cache for an absent preference.
    /// </summary>
    internal Dictionary<string, Task<string?>> FirstReads { get; } = new(StringComparer.Ordinal);

    private volatile bool _released;

    /// <summary>
    /// Whether the opening of this cache was released (a change of user, the disposal): its script module refuses
    /// every call, and a caller that meets <see cref="ObjectDisposedException"/> on it takes the current opening instead.
    /// </summary>
    internal bool Released => _released;

    internal void MarkReleased()
    {
        _released = true;
        Mirror?.MarkReleased();
    }

    private string SyncKey { get; } = SyncPrefix + scope;

    private string SyncedKey { get; } = SyncedPrefix + scope;

    internal string StorageKey(string key) => Prefix + key;

    /// <summary>The preference key of a storage key of this scope; null for any other storage key.</summary>
    internal string? KeyOf(string storageKey) =>
        storageKey.Length > Prefix.Length && storageKey.StartsWith(Prefix, StringComparison.Ordinal)
            ? storageKey[Prefix.Length..]
            : null;

    internal async Task<PreferenceCacheEntry?> ReadAsync(string key, CancellationToken cancellationToken) =>
        Parse(await module.InvokeTrackedAsync<string?>("get", cancellationToken, StorageKey(key)));

    internal async Task WriteAsync(string key, PreferenceCacheEntry entry, CancellationToken cancellationToken)
    {
        await module.InvokeVoidTrackedAsync("set", cancellationToken, StorageKey(key), JsonSerializer.Serialize(entry));
        if (Mirror is not null)
        {
            await Mirror.WriteAsync(key, entry with { Dirty = false }, cancellationToken);
        }
    }

    /// <summary>Every readable entry of this scope; an entry that no longer parses is left out.</summary>
    internal async Task<Dictionary<string, PreferenceCacheEntry>> EntriesAsync(CancellationToken cancellationToken)
    {
        var pairs = await module.InvokeTrackedAsync<string[][]>("entries", cancellationToken, Prefix);
        var entries = new Dictionary<string, PreferenceCacheEntry>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            if (pair is [var storageKey, var json] && KeyOf(storageKey) is { } key && Parse(json) is { } entry)
            {
                entries[key] = entry;
            }
        }

        return entries;
    }

    internal async Task<DateTimeOffset?> ReadLastSyncAsync(CancellationToken cancellationToken)
    {
        var text = await module.InvokeTrackedAsync<string?>("get", cancellationToken, SyncKey);
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;
    }

    internal async Task WriteLastSyncAsync(DateTimeOffset value, CancellationToken cancellationToken) =>
        await module.InvokeVoidTrackedAsync("set", cancellationToken, SyncKey, value.ToString("O", CultureInfo.InvariantCulture));

    /// <summary>When a synchronization of this scope last completed, by this device's clock; null when none did.</summary>
    internal async Task<DateTimeOffset?> ReadSyncedAtAsync(CancellationToken cancellationToken)
    {
        var text = await module.InvokeTrackedAsync<string?>("get", cancellationToken, SyncedKey);
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;
    }

    internal async Task WriteSyncedAtAsync(DateTimeOffset value, CancellationToken cancellationToken) =>
        await module.InvokeVoidTrackedAsync("set", cancellationToken, SyncedKey, value.ToString("O", CultureInfo.InvariantCulture));

    private static PreferenceCacheEntry? Parse(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PreferenceCacheEntry>(json);
        }
        catch (JsonException)
        {
            // An entry edited by hand or written by another version: treated as absent, so the store
            // value, or the caller's default, takes over.
            return null;
        }
    }
}
