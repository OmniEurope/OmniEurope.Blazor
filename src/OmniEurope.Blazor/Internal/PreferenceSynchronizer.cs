using Microsoft.Extensions.Logging;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The reconciliation of the browser cache of <see cref="OmniPreferences"/> with the host's store: the
/// synchronization (what the store changed, what the cache holds unconfirmed), the arbitration of a store
/// value against a cached one by date, the confirmed write, and the check that the store still answers for
/// the user the cache belongs to. Each read-decide-write of a cached entry runs under <c>cacheGate</c>,
/// shared with the writes of <see cref="OmniPreferences"/>, so a write made meanwhile is never lost.
/// </summary>
internal sealed class PreferenceSynchronizer(IOmniPreferenceStore store, PreferenceStoreAccess access, SemaphoreSlim cacheGate, TimeProvider time, ILogger logger)
{
    /// <summary>
    /// One synchronization; the changes it applied are added to <paramref name="changes"/>, for the caller
    /// to raise once the synchronization gate is free (even when this one fails half-way). False, with
    /// nothing written to the cache or to the store, when the store now answers for another user than the
    /// cache belongs to.
    /// </summary>
    internal async Task<bool> SyncAsync(PreferenceCache cache, List<OmniPreferenceChangedEventArgs> changes, CancellationToken cancellationToken)
    {
        var since = await cache.ReadLastSyncAsync(cancellationToken);
        var (stored, watermark) = Latest(await access.CallAsync(current => current.GetChangedSinceAsync(since, cancellationToken), cancellationToken));
        // Checked once the store has answered, before anything is written to it or to the cache: its answer
        // is of the user it was given for, which must be the one the cache belongs to.
        if (await UserChangedAsync(cache, wait: true, cancellationToken) != false)
        {
            return false;
        }

        var entries = await cache.EntriesAsync(cancellationToken);
        await PushDirtyAsync(cache, entries, stored, cancellationToken);
        foreach (var (key, preference) in stored)
        {
            // The snapshot taken above may be stale by now: the entry is read again before deciding.
            if (await ApplyStoredAsync(cache, key, preference, cancellationToken) is { } change)
            {
                changes.Add(change);
            }
        }

        // The mark is a date of the store's own preferences, never this device's clock, so a change
        // dated by another device is not skipped because the two clocks differ.
        if (watermark is { } next && (since is not { } previous || next > previous))
        {
            await cache.WriteLastSyncAsync(next, cancellationToken);
        }

        // Only a synchronization that went through counts: a failed one is tried again by the next trigger.
        await cache.WriteSyncedAtAsync(time.GetUtcNow(), cancellationToken);
        return true;
    }

    /// <summary>
    /// Whether the cache was synchronized less than <paramref name="gap"/> ago (this device's clock, kept in the
    /// browser cache so a new page load or another tab of the same user sees it); never when the gap is zero.
    /// </summary>
    internal async Task<bool> SyncedWithinAsync(PreferenceCache cache, TimeSpan gap, CancellationToken cancellationToken)
    {
        if (gap <= TimeSpan.Zero || await cache.ReadSyncedAtAsync(cancellationToken) is not { } synced)
        {
            return false;
        }

        var elapsed = time.GetUtcNow() - synced;
        // A clock set back is not "recent": the next trigger synchronizes and the date is written again.
        return elapsed >= TimeSpan.Zero && elapsed < gap;
    }

    /// <summary>The latest store value of each key listed, and the latest date of the list.</summary>
    private static (Dictionary<string, OmniPreference> Stored, DateTimeOffset? Watermark) Latest(IReadOnlyList<OmniPreference> changes)
    {
        var stored = new Dictionary<string, OmniPreference>(StringComparer.Ordinal);
        DateTimeOffset? watermark = null;
        foreach (var preference in changes)
        {
            if (string.IsNullOrWhiteSpace(preference?.Key))
            {
                continue;
            }

            if (!stored.TryGetValue(preference.Key, out var known) || preference.UpdatedAt > known.UpdatedAt)
            {
                stored[preference.Key] = preference;
            }

            watermark = watermark is { } mark && mark >= preference.UpdatedAt ? mark : preference.UpdatedAt;
        }

        return (stored, watermark);
    }

    /// <summary>
    /// Writes each unconfirmed entry to the store, unless the store holds a value at least as late,
    /// which is then left in <paramref name="stored"/> to replace it.
    /// </summary>
    private async Task PushDirtyAsync(PreferenceCache cache, Dictionary<string, PreferenceCacheEntry> entries,
        Dictionary<string, OmniPreference> stored, CancellationToken cancellationToken)
    {
        foreach (var (key, entry) in entries.Where(pair => pair.Value.Dirty))
        {
            // The store's value of a key it did not report as changed may still be later than the
            // unconfirmed one (written on another device before the last synchronization).
            var latest = stored.GetValueOrDefault(key) ?? await access.TryGetAsync(key, cancellationToken);
            if (latest is not null && latest.UpdatedAt >= entry.UpdatedAt)
            {
                stored[key] = latest;
                continue;
            }

            stored.Remove(key);
            await PushAsync(cache, new OmniPreference(key, entry.Value, entry.UpdatedAt), cancellationToken);
        }
    }


    /// <summary>A store value in place of the cached one when it wins; the change to raise, if any.</summary>
    internal async Task<OmniPreferenceChangedEventArgs?> ApplyStoredAsync(PreferenceCache cache, string key, OmniPreference stored, CancellationToken cancellationToken)
    {
        var (current, applied) = await ApplyIfStoreWinsAsync(cache, key, stored, cancellationToken);
        return applied && current?.Value != stored.Value && (current is not null || stored.Value is not null)
            ? new(key, stored.Value, OmniPreferenceChangeOrigin.Store)
            : null;
    }

    /// <summary>
    /// Writes the store value to the cache when it wins over the entry cached at this moment: nothing
    /// cached, a later date, or an unconfirmed entry the store holds a value at least as late as. A
    /// local write made while the store answered is later, and stays.
    /// </summary>
    internal async Task<(PreferenceCacheEntry? Current, bool Applied)> ApplyIfStoreWinsAsync(PreferenceCache cache, string key,
        OmniPreference stored, CancellationToken cancellationToken)
    {
        await cacheGate.WaitAsync(cancellationToken);
        try
        {
            var current = await cache.ReadAsync(key, cancellationToken);
            if ((current is not null && stored.UpdatedAt < current.UpdatedAt) || (current is { Dirty: false } && stored.UpdatedAt == current.UpdatedAt))
            {
                return (current, false);
            }

            await cache.WriteAsync(key, new(stored.Value, stored.UpdatedAt, Dirty: false), cancellationToken);
            return (current, true);
        }
        finally
        {
            cacheGate.Release();
        }
    }

    /// <summary>
    /// Writes a preference to the store while the cached entry is still the one to write, then marks the
    /// cached entry confirmed. Nothing is saved when the store now answers for another user than the cache
    /// belongs to: a value is never saved for a user other than the one it was written for.
    /// </summary>
    internal async Task PushAsync(PreferenceCache? cache, OmniPreference preference, CancellationToken cancellationToken)
    {
        // Checked under the store gate: an entry another write replaced is not saved over (its own push saves
        // it). An entry gone from the cache (cleared, or a browser that keeps nothing) is still saved.
        if (!await access.TrySaveAsync(preference, async () =>
                (cache is null || await store.GetScopeAsync(cancellationToken) == cache.Scope)
                && (cache is null || await cache.ReadAsync(preference.Key, cancellationToken) is not { } cached || IsCached(cached, preference)),
                cancellationToken))
        {
            return;
        }

        if (cache is null)
        {
            return;
        }

        // Confirmed: unless a later write replaced it meanwhile, the cached entry is no longer pending.
        await cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (await cache.ReadAsync(preference.Key, cancellationToken) is { } entry && IsCached(entry, preference))
            {
                await cache.WriteAsync(preference.Key, entry with { Dirty = false }, cancellationToken);
            }
        }
        finally
        {
            cacheGate.Release();
        }
    }

    /// <summary>Whether the cached entry is exactly the preference being written.</summary>
    private static bool IsCached(PreferenceCacheEntry? entry, OmniPreference preference) =>
        entry is not null && entry.UpdatedAt == preference.UpdatedAt && entry.Value == preference.Value;

    /// <summary>
    /// Whether the store now names another scope than the cache's; null when it cannot say (logged), in
    /// which case the cache is not to be trusted for this call either. With <paramref name="wait"/> false,
    /// a store busy with another call is not asked and the user is taken as unchanged.
    /// </summary>
    internal async Task<bool?> UserChangedAsync(PreferenceCache cache, bool wait, CancellationToken cancellationToken)
    {
        try
        {
            Func<IOmniPreferenceStore, Task<string?>> ask = async current => await current.GetScopeAsync(cancellationToken);
            var scope = wait ? await access.CallAsync(ask, cancellationToken) : await access.CallIfIdleAsync(ask, cancellationToken);
            return scope is not null && scope != cache.Scope;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The preference store could not give the scope of the browser cache; the store is used alone until it does.");
            return null;
        }
    }
}
