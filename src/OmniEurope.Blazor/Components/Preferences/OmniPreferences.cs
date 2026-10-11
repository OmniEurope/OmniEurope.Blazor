using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The user preferences (appearance, grid layouts and any value of the host), kept in the host's
/// <see cref="IOmniPreferenceStore"/> when it registers one and cached in the browser's
/// <c>localStorage</c>, which answers first. Without a store the preferences live in the browser only,
/// through the same API. Registered as scoped by <c>AddOmniEuropeBlazor</c>.
/// </summary>
/// <remarks>
/// <para>
/// Values are serialized as JSON (web defaults). A value that no longer reads as <c>T</c> (its shape
/// changed) reads as <c>default</c>, so the caller keeps its own defaults.
/// </para>
/// <para>
/// The store wins: a store value with a later date replaces the cached one and raises
/// <see cref="Changed"/>. The cache follows the store automatically: each key once per circuit when it
/// is first read, every <see cref="OmniPreferencesOptions.SyncInterval"/>, and whenever the tab becomes
/// visible again; another tab of the same browser is followed through the browser's <c>storage</c>
/// event. A write the store refuses is logged and kept in the browser, then written again at the next
/// synchronization.
/// </para>
/// <para>
/// While the page is prerendered, without JavaScript, reads and writes go to the store directly and
/// the browser cache is left alone; without a store they read nothing and keep nothing.
/// </para>
/// </remarks>
public sealed class OmniPreferences : IAsyncDisposable
{
    internal const string LocalScope = "local";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _javaScript;
    private readonly IOmniPreferenceStore? _store;
    private readonly ILogger<OmniPreferences> _logger;
    private readonly TimeProvider _time;
    private readonly TimeSpan _syncInterval;
    private readonly CancellationTokenSource _disposal = new();
    private readonly CancellationToken _stopping;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    // One store call at a time: the store is scoped (often a DbContext), and a synchronization runs off
    // the renderer's thread while the UI writes.
    private readonly SemaphoreSlim _storeGate = new(1, 1);
    // Each read-decide-write of a cached entry runs alone, so a write made meanwhile is never lost.
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private readonly Lock _gate = new();
    private readonly HashSet<string> _refreshed = new(StringComparer.Ordinal);
    private readonly List<Task> _background = [];
    private Task<PreferenceCache?>? _opening;
    private IJSObjectReference? _module;
    private DotNetObjectReference<PreferenceInteropBridge>? _bridge;
    private int _watcher;
    private ITimer? _timer;
    private bool _disposed;

    internal OmniPreferences(IJSRuntime javaScript, IOmniPreferenceStore? store, OmniPreferencesOptions options,
        ILogger<OmniPreferences> logger, TimeProvider time)
    {
        var interval = options.SyncInterval;
        if (interval != Timeout.InfiniteTimeSpan && interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), interval, "OmniPreferencesOptions.SyncInterval must be positive, or Timeout.InfiniteTimeSpan.");
        }

        _javaScript = javaScript;
        _store = store;
        _logger = logger;
        _time = time;
        _syncInterval = interval;
        _stopping = _disposal.Token;
    }

    /// <summary>
    /// Raised when a preference takes a new value: written here, read later from the store, or written
    /// by another tab of the same browser. It may be raised off the renderer's thread (a periodic
    /// synchronization): a component handler marshals its work with <c>InvokeAsync</c>. A handler that
    /// throws is logged and does not stop the others.
    /// </summary>
    public event Func<OmniPreferenceChangedEventArgs, Task>? Changed;

    /// <summary>
    /// Reads a preference: the cached value at once, or, when the browser holds none, the store's. The
    /// first read of a key in the circuit also asks the store in the background and raises
    /// <see cref="Changed"/> when the store holds a later value.
    /// </summary>
    /// <typeparam name="T">The type the value was written as.</typeparam>
    /// <param name="key">The key of the preference; must not be null or blank.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The value, or <c>default</c> when the preference is not set, was removed, or no longer reads as <typeparamref name="T"/>.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Deserialize<T>(await ReadAsync(key, cancellationToken));

    /// <summary>
    /// Writes a preference: in the browser first, then in the store, dated by the host's
    /// <see cref="TimeProvider"/>. A value equal to the current one writes nothing. A store failure is
    /// logged; the value stays in the browser and is written again at the next synchronization.
    /// </summary>
    /// <typeparam name="T">The type of the value, serialized as JSON.</typeparam>
    /// <param name="key">The key of the preference; must not be null or blank.</param>
    /// <param name="value">The value to keep.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the value is cached and the store has answered.</returns>
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) =>
        WriteAsync(key, JsonSerializer.Serialize(value, JsonOptions), cancellationToken);

    /// <summary>
    /// Removes a preference, which then reads as <c>default</c>. The removal is kept as a dated tombstone
    /// in the browser and in the store, so it reaches the user's other devices like any change.
    /// </summary>
    /// <param name="key">The key of the preference; must not be null or blank.</param>
    /// <param name="cancellationToken">Cancels the removal.</param>
    /// <returns>A task that completes once the removal is cached and the store has answered.</returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        WriteAsync(key, null, cancellationToken);

    /// <summary>
    /// Brings the browser cache and the store in step now, as the automatic synchronization does: the
    /// values the store has not confirmed are written to it unless it holds a later one, then every
    /// preference the store changed since the last synchronization replaces an older cached value. Does
    /// nothing without a store or while the page is prerendered. A store that cannot list its changes
    /// fails the task with its own exception; the unconfirmed values stay cached.
    /// </summary>
    /// <param name="cancellationToken">Cancels the synchronization.</param>
    /// <returns>A task that completes once the synchronization is over.</returns>
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        if (_store is null || await OpenAsync(cancellationToken) is not { } cache)
        {
            return;
        }

        var changes = new List<OmniPreferenceChangedEventArgs>();
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            await SyncCoreAsync(cache, changes, cancellationToken);
        }
        finally
        {
            _syncGate.Release();
            // Raised once the gate is free: a slow handler must not hold the next synchronization.
            await RaiseAllAsync(changes);
        }
    }

    /// <summary>Stops the automatic synchronization and releases the browser script.</summary>
    /// <returns>A task that completes once the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _disposal.CancelAsync();
        _timer?.Dispose();
        if (_module is not null)
        {
            try
            {
                if (_watcher != 0)
                {
                    await _module.InvokeVoidAsync("unwatch", _watcher);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the listeners with it.
            }
        }

        _bridge?.Dispose();
        _disposal.Dispose();
    }

    /// <summary>Whether the host registered a store: the preferences are then partitioned by its user scope.</summary>
    internal bool HasStore => _store is not null;

    /// <summary>Completes once the background work started so far (refreshes, synchronizations) is over.</summary>
    internal Task WhenIdleAsync()
    {
        lock (_gate)
        {
            return Task.WhenAll(_background);
        }
    }

    private async Task<string?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var cache = await OpenAsync(cancellationToken);
        if (cache is null)
        {
            return _store is null ? null : (await TryGetStoredAsync(key, cancellationToken))?.Value;
        }

        var cached = await cache.ReadAsync(key, cancellationToken);
        if (_store is null || !MarkRefreshed(key))
        {
            return cached?.Value;
        }

        if (cached is not null)
        {
            Track(RefreshAsync(cache, key));
            return cached.Value;
        }

        // Nothing cached (a new device, a cleared browser): the store is read now so the first display
        // is already right.
        var stored = await TryGetStoredAsync(key, cancellationToken);
        if (stored is null)
        {
            return null;
        }

        // A write made while the store answered stays, and is what the caller reads.
        var (before, applied) = await ApplyIfStoreWinsAsync(cache, key, stored, cancellationToken);
        return applied ? stored.Value : before?.Value;
    }

    private async Task WriteAsync(string key, string? value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var cache = await OpenAsync(cancellationToken);
        var preference = new OmniPreference(key, value, _time.GetUtcNow());
        var changed = true;
        if (cache is not null)
        {
            await _cacheGate.WaitAsync(cancellationToken);
            try
            {
                var current = await cache.ReadAsync(key, cancellationToken);
                if (current is not null && current.Value == value)
                {
                    return;
                }

                // A removal of a value never cached still reaches the store, which may hold it.
                changed = current is not null || value is not null;
                await cache.WriteAsync(key, new(value, preference.UpdatedAt, Dirty: _store is not null), cancellationToken);
            }
            finally
            {
                _cacheGate.Release();
            }
        }

        if (changed)
        {
            await RaiseAsync(new(key, value, OmniPreferenceChangeOrigin.Local));
        }

        if (_store is not null)
        {
            await PushAsync(cache, preference, cancellationToken);
        }
    }

    /// <summary>
    /// One synchronization; the changes it applied are added to <paramref name="changes"/>, for the caller
    /// to raise once the synchronization gate is free (even when this one fails half-way).
    /// </summary>
    private async Task SyncCoreAsync(PreferenceCache cache, List<OmniPreferenceChangedEventArgs> changes, CancellationToken cancellationToken)
    {
        var since = await cache.ReadLastSyncAsync(cancellationToken);
        var (stored, watermark) = Latest(await CallStoreAsync(store => store.GetChangedSinceAsync(since, cancellationToken), cancellationToken));
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
            var latest = stored.GetValueOrDefault(key) ?? await TryGetStoredAsync(key, cancellationToken);
            if (latest is not null && latest.UpdatedAt >= entry.UpdatedAt)
            {
                stored[key] = latest;
                continue;
            }

            stored.Remove(key);
            await PushAsync(cache, new OmniPreference(key, entry.Value, entry.UpdatedAt), cancellationToken);
        }
    }

    private async Task RefreshAsync(PreferenceCache cache, string key)
    {
        // The caller already has the cached value; the store is asked after it resumed.
        await Task.Yield();
        await RunInBackgroundAsync(async token =>
        {
            if (await TryGetStoredAsync(key, token) is { } stored && await ApplyStoredAsync(cache, key, stored, token) is { } change)
            {
                await RaiseAsync(change);
            }
        });
    }

    private async Task SyncInBackgroundAsync(PreferenceCache cache)
    {
        await Task.Yield();
        if (!await _syncGate.WaitAsync(0, CancellationToken.None))
        {
            // A synchronization is already running; it brings the same changes.
            return;
        }

        var changes = new List<OmniPreferenceChangedEventArgs>();
        try
        {
            await RunInBackgroundAsync(token => SyncCoreAsync(cache, changes, token));
        }
        finally
        {
            _syncGate.Release();
        }

        await RaiseAllAsync(changes);
    }

    private async Task RunInBackgroundAsync(Func<CancellationToken, Task> work)
    {
        try
        {
            await work(_stopping);
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            // The circuit ended while the work ran.
        }
        catch (JSDisconnectedException)
        {
            // Same: the browser is gone.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The preferences could not be synchronized with the store; the browser cache is kept and the next synchronization tries again.");
        }
    }

    /// <summary>A store value in place of the cached one when it wins; the change to raise, if any.</summary>
    private async Task<OmniPreferenceChangedEventArgs?> ApplyStoredAsync(PreferenceCache cache, string key, OmniPreference stored, CancellationToken cancellationToken)
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
    private async Task<(PreferenceCacheEntry? Current, bool Applied)> ApplyIfStoreWinsAsync(PreferenceCache cache, string key,
        OmniPreference stored, CancellationToken cancellationToken)
    {
        await _cacheGate.WaitAsync(cancellationToken);
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
            _cacheGate.Release();
        }
    }

    private async Task PushAsync(PreferenceCache? cache, OmniPreference preference, CancellationToken cancellationToken)
    {
        await _storeGate.WaitAsync(cancellationToken);
        try
        {
            // Read under the store gate: a write that replaced this entry is saved after it, by its own push,
            // so an older value is never saved over a newer one. An entry gone from the cache (cleared, or a
            // browser that keeps nothing) is still saved.
            if ((cache is not null && await cache.ReadAsync(preference.Key, cancellationToken) is { } cached && !IsCached(cached, preference))
                || !await TrySaveAsync(preference, cancellationToken))
            {
                return;
            }
        }
        finally
        {
            _storeGate.Release();
        }

        if (cache is null)
        {
            return;
        }

        // Confirmed: unless a later write replaced it meanwhile, the cached entry is no longer pending.
        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (await cache.ReadAsync(preference.Key, cancellationToken) is { } entry && IsCached(entry, preference))
            {
                await cache.WriteAsync(preference.Key, entry with { Dirty = false }, cancellationToken);
            }
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    /// <summary>Saves to the store, the caller holding the store gate; a failure is logged and gives false.</summary>
    private async Task<bool> TrySaveAsync(OmniPreference preference, CancellationToken cancellationToken)
    {
        try
        {
            await _store!.SaveAsync(preference, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The preference {Key} could not be written to the store; it stays in the browser and the next synchronization writes it again.", preference.Key);
            return false;
        }
    }

    private static bool IsCached(PreferenceCacheEntry? entry, OmniPreference preference) =>
        entry is not null && entry.UpdatedAt == preference.UpdatedAt && entry.Value == preference.Value;

    private async Task<OmniPreference?> TryGetStoredAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await CallStoreAsync(store => store.GetAsync(key, cancellationToken), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The preference {Key} could not be read from the store; the browser cache answers instead.", key);
            return null;
        }
    }

    /// <summary>One call to the store, never at the same time as another (see <see cref="IOmniPreferenceStore"/>).</summary>
    private async Task<T> CallStoreAsync<T>(Func<IOmniPreferenceStore, Task<T>> call, CancellationToken cancellationToken)
    {
        await _storeGate.WaitAsync(cancellationToken);
        try
        {
            return await call(_store!);
        }
        finally
        {
            _storeGate.Release();
        }
    }

    private async Task RaiseAllAsync(List<OmniPreferenceChangedEventArgs> changes)
    {
        foreach (var change in changes)
        {
            await RaiseAsync(change);
        }
    }

    private async Task RaiseAsync(OmniPreferenceChangedEventArgs change)
    {
        if (Changed is not { } handlers)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<OmniPreferenceChangedEventArgs, Task>>())
        {
            try
            {
                await handler(change);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A handler of OmniPreferences.Changed failed for the preference {Key}.", change.Key);
            }
        }
    }

    private async Task<PreferenceCache?> OpenAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Task<Task<PreferenceCache?>>? start = null;
        Task<PreferenceCache?> opening;
        lock (_gate)
        {
            if (_opening is null)
            {
                start = new(OpenCoreAsync);
                _opening = start.Unwrap();
            }

            opening = _opening;
        }

        // Started outside the lock, on the caller's thread and context.
        start?.RunSynchronously(TaskScheduler.Default);
        PreferenceCache? cache;
        try
        {
            cache = await opening.WaitAsync(cancellationToken);
        }
        catch when (opening.IsFaulted || opening.IsCanceled)
        {
            // A failed opening is not kept for the whole circuit: the next call tries again.
            ForgetOpening(opening);
            throw;
        }

        if (cache is null)
        {
            // Prerendering, or a store that could not name its scope: the next call tries again.
            ForgetOpening(opening);
        }

        return cache;
    }

    private void ForgetOpening(Task<PreferenceCache?> opening)
    {
        lock (_gate)
        {
            if (_opening == opening)
            {
                _opening = null;
            }
        }
    }

    private async Task<PreferenceCache?> OpenCoreAsync()
    {
        var scope = LocalScope;
        if (_store is not null)
        {
            try
            {
                scope = await CallStoreAsync(store => store.GetScopeAsync(_stopping).AsTask(), _stopping);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !_stopping.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "The preference store could not give the scope of the browser cache; the store is used alone until it does.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(scope) || scope.Contains(':', StringComparison.Ordinal))
            {
                throw new InvalidOperationException("IOmniPreferenceStore.GetScopeAsync must return a non-blank scope without a colon.");
            }
        }

        IJSObjectReference module;
        try
        {
            module = await _javaScript.InvokeAsync<IJSObjectReference>("import", _stopping, OmniModules.Preferences);
        }
        catch (InvalidOperationException)
        {
            // Prerendering: JavaScript cannot be called yet.
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }

        var cache = new PreferenceCache(module, scope);
        try
        {
            await WatchAsync(module, cache);
        }
        catch
        {
            await ReleaseOpeningAsync(module);
            throw;
        }

        _module = module;
        return cache;
    }

    /// <summary>Releases what a failed opening set up, so the next opening starts clean.</summary>
    private async Task ReleaseOpeningAsync(IJSObjectReference module)
    {
        _timer?.Dispose();
        _timer = null;
        _bridge?.Dispose();
        _bridge = null;
        _watcher = 0;
        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone, and the module with it.
        }
    }

    private async Task WatchAsync(IJSObjectReference module, PreferenceCache cache)
    {
        var hasStore = _store is not null;
        _bridge = DotNetObjectReference.Create(new PreferenceInteropBridge(
            onVisible: () => hasStore ? SyncInBackgroundAsync(cache) : Task.CompletedTask,
            onStored: storageKey => OnStoredAsync(cache, storageKey)));
        _watcher = await module.InvokeAsync<int>("watch", _stopping, _bridge, cache.Prefix);
        if (hasStore && _syncInterval != Timeout.InfiniteTimeSpan)
        {
            _timer = _time.CreateTimer(_ => Track(SyncInBackgroundAsync(cache)), null, _syncInterval, _syncInterval);
        }
    }

    private async Task OnStoredAsync(PreferenceCache cache, string storageKey)
    {
        if (cache.KeyOf(storageKey) is not { } key)
        {
            return;
        }

        await RunInBackgroundAsync(async token =>
        {
            var entry = await cache.ReadAsync(key, token);
            await RaiseAsync(new(key, entry?.Value, OmniPreferenceChangeOrigin.OtherTab));
        });
    }

    private bool MarkRefreshed(string key)
    {
        lock (_gate)
        {
            return _refreshed.Add(key);
        }
    }

    private void Track(Task work)
    {
        lock (_gate)
        {
            _background.RemoveAll(task => task.IsCompleted);
            _background.Add(work);
        }
    }

    private static T? Deserialize<T>(string? json)
    {
        if (json is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
