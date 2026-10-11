using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>One cached preference: its JSON value (null for a tombstone), its date, and whether the store has yet to confirm it.</summary>
internal sealed record PreferenceCacheEntry(string? Value, DateTimeOffset UpdatedAt, bool Dirty);

/// <summary>
/// The browser cache of <see cref="Components.OmniPreferences"/>: one <c>localStorage</c> entry per
/// preference under <c>omni:pref:&lt;scope&gt;:&lt;key&gt;</c>, and the date of the last synchronization
/// under <c>omni:pref-sync:&lt;scope&gt;</c>, read and written through <c>omni-preferences.js</c>. The key
/// format and the entry encoding live here; the script only reads and writes strings, and falls back
/// to memory when the browser refuses its storage.
/// </summary>
internal sealed class PreferenceCache(IJSObjectReference module, string scope)
{
    internal const string KeyPrefix = "omni:pref:";
    internal const string SyncPrefix = "omni:pref-sync:";

    /// <summary>The prefix of every entry of this scope.</summary>
    internal string Prefix { get; } = KeyPrefix + scope + ":";

    private string SyncKey { get; } = SyncPrefix + scope;

    internal string StorageKey(string key) => Prefix + key;

    /// <summary>The preference key of a storage key of this scope; null for any other storage key.</summary>
    internal string? KeyOf(string storageKey) =>
        storageKey.Length > Prefix.Length && storageKey.StartsWith(Prefix, StringComparison.Ordinal)
            ? storageKey[Prefix.Length..]
            : null;

    internal async Task<PreferenceCacheEntry?> ReadAsync(string key, CancellationToken cancellationToken) =>
        Parse(await module.InvokeAsync<string?>("get", cancellationToken, StorageKey(key)));

    internal async Task WriteAsync(string key, PreferenceCacheEntry entry, CancellationToken cancellationToken) =>
        await module.InvokeVoidAsync("set", cancellationToken, StorageKey(key), JsonSerializer.Serialize(entry));

    /// <summary>Every readable entry of this scope; an entry that no longer parses is left out.</summary>
    internal async Task<Dictionary<string, PreferenceCacheEntry>> EntriesAsync(CancellationToken cancellationToken)
    {
        var pairs = await module.InvokeAsync<string[][]>("entries", cancellationToken, Prefix);
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
        var text = await module.InvokeAsync<string?>("get", cancellationToken, SyncKey);
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;
    }

    internal async Task WriteLastSyncAsync(DateTimeOffset value, CancellationToken cancellationToken) =>
        await module.InvokeVoidAsync("set", cancellationToken, SyncKey, value.ToString("O", CultureInfo.InvariantCulture));

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
