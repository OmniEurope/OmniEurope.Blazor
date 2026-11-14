using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// How an <see cref="OmniPreferences"/> reads a key against the browser cache and the store: the cached
/// value at once, what a read owes the store (a refresh, a first read nothing caches, the wait for one
/// already under way), and the read with no cache open. The service keeps the opening of the cache, the
/// writes and the synchronization; this collaborator calls back into it to track and raise.
/// </summary>
internal sealed class PreferenceReads(
    OmniPreferences owner,
    PreferenceStoreAccess? storeAccess,
    PreferenceSynchronizer? sync,
    SignedOutPreferences signedOut,
    CancellationToken stopping)
{
    // Guards the Refreshed and FirstReads bookkeeping of every cache this reader serves.
    private readonly Lock _gate = new();

    internal async Task<string?> ReadFromAsync(PreferenceCache? cache, string key, CancellationToken cancellationToken)
    {
        if (cache is null)
        {
            return await ReadUncachedAsync(key, cancellationToken);
        }

        var cached = await cache.ReadAsync(key, cancellationToken);
        if (!owner.HasStore || cache.IsVisitor)
        {
            return cached?.Value;
        }

        var (firstRead, started, refresh) = ClaimRead(cache, key, cached is not null);
        if (refresh)
        {
            owner.Track(RefreshAsync(cache, key));
            return cached!.Value;
        }

        if (started is not null)
        {
            owner.Track(ReadFirstAsync(cache, key, started));
        }

        // A key read before, whose first read is over: the cache holds what the store said.
        return firstRead is null ? cached?.Value : await firstRead.WaitAsync(cancellationToken);
    }

    /// <summary>A read with no cache open: the signed-out values of this circuit, else the store itself, else nothing.</summary>
    private async Task<string?> ReadUncachedAsync(string key, CancellationToken cancellationToken)
    {
        if (signedOut.Active)
        {
            return signedOut.Read(key);
        }

        return owner.HasStore ? (await storeAccess!.TryGetAsync(key, cancellationToken))?.Value : null;
    }

    /// <summary>
    /// Decides, under the gate, what a read of <paramref name="key"/> owes the store: a refresh behind a cached value, the
    /// first read of a key nothing caches (<c>Started</c>, the caller then runs it), or the wait for a first read already
    /// under way (<c>FirstRead</c>).
    /// </summary>
    private (Task<string?>? FirstRead, TaskCompletionSource<string?>? Started, bool Refresh) ClaimRead(PreferenceCache cache, string key, bool isCached)
    {
        lock (_gate)
        {
            if (!cache.Refreshed.Add(key))
            {
                return (cache.FirstReads.GetValueOrDefault(key), null, false);
            }

            if (isCached)
            {
                return (null, null, true);
            }

            // Registered before it runs: a read that comes while the store answers finds it.
            var started = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            cache.FirstReads[key] = started.Task;
            return (started.Task, started, false);
        }
    }

    /// <summary>
    /// The first read of a key nothing caches (a new device, a cleared browser): the store is read now so
    /// that the first display is already right, under the service's lifetime rather than one caller's
    /// token, since every concurrent read of the key waits for it.
    /// </summary>
    private async Task ReadFirstAsync(PreferenceCache cache, string key, TaskCompletionSource<string?> result)
    {
        try
        {
            var stored = await storeAccess!.TryGetAsync(key, stopping);
            if (stored is null)
            {
                // The store has none: what the cache holds now (a write made meanwhile, or nothing).
                result.TrySetResult((await cache.ReadAsync(key, stopping))?.Value);
                return;
            }

            // A write made while the store answered stays, and is what the caller reads (the store value
            // is only refused over an entry cached meanwhile, so there is one when it is not applied).
            var (before, applied) = await sync!.ApplyIfStoreWinsAsync(cache, key, stored, stopping);
            result.TrySetResult(applied ? stored.Value : before!.Value);
        }
        catch (Exception exception)
        {
            // Every waiting read sees the failure; none is lost unobserved here.
            result.TrySetException(exception);
        }
        finally
        {
            lock (_gate)
            {
                // Only its own entry: a reset of the user may have registered a newer read of the key meanwhile.
                if (cache.FirstReads.TryGetValue(key, out var registered) && registered == result.Task)
                {
                    cache.FirstReads.Remove(key);
                }
            }
        }
    }

    private async Task RefreshAsync(PreferenceCache cache, string key)
    {
        // The caller already has the cached value; the store is asked after it resumed.
        await Task.Yield();
        await owner.RunInBackgroundAsync(async token =>
        {
            // Never limited: a page load re-reads each cached key it reads once, so a reload shows the store's value.
            if (await storeAccess!.TryGetAsync(key, token) is { } stored && await sync!.ApplyStoredAsync(cache, key, stored, token) is { } change)
            {
                await owner.RaiseAsync(change);
            }
        });
    }
}
