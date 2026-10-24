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
/// The first-read and tab-visible synchronizations are limited: none runs when the cache (shared by the
/// page loads and the tabs of the same browser and user) synchronized less than
/// <see cref="OmniPreferencesOptions.MinSyncGap"/> ago (thirty seconds by default), and concurrent
/// triggers share the one synchronization under way. The periodic synchronization and
/// <see cref="SyncAsync"/> are never limited: a host that learns of a change by its own signal (a SignalR
/// hub, for instance) calls <see cref="SyncAsync"/> at once, and may set
/// <see cref="OmniPreferencesOptions.SyncInterval"/> to <see cref="Timeout.InfiniteTimeSpan"/>.
/// </para>
/// <para>
/// While the page is prerendered, without JavaScript, reads and writes go to the store directly and
/// the browser cache is left alone; without a store they read nothing and keep nothing.
/// </para>
/// </remarks>
public sealed class OmniPreferences : IAsyncDisposable
{
    internal const string LocalScope = "local";

    /// <summary>
    /// The scope an <see cref="IOmniPreferenceStore"/> answers while no user is signed in (an empty string): the
    /// preferences then read as <c>default</c>, so every component keeps its own defaults, and a value written
    /// meanwhile is kept in the circuit's memory only, never in the browser cache nor in the store. Nothing is
    /// logged. The scope is asked again at the next read or write; <see cref="ResetUserAsync"/> applies at once
    /// the preferences of the user who signs in.
    /// </summary>
    public const string NoUserScope = "";

    private readonly IJSRuntime _javaScript;
    private readonly IOmniPreferenceStore? _store;
    private readonly ILogger<OmniPreferences> _logger;
    private readonly TimeProvider _time;
    private readonly TimeSpan _syncInterval;
    private readonly TimeSpan _minSyncGap;
    private readonly CancellationTokenSource _disposal = new();
    private readonly CancellationToken _stopping;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    // The store's calls, one at a time (null without a store).
    private readonly PreferenceStoreAccess? _storeAccess;
    // Reconciles the browser cache with the store (null without a store).
    private readonly PreferenceSynchronizer? _sync;
    // Each read-decide-write of a cached entry runs alone, so a write made meanwhile is never lost.
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private readonly Lock _gate = new();
    private readonly HashSet<string> _refreshed = new(StringComparer.Ordinal);
    // The first read of a key not cached yet, while the store answers: a read of the same key made
    // meanwhile waits for it instead of taking the empty cache for an absent preference.
    private readonly Dictionary<string, Task<string?>> _firstReads = new(StringComparer.Ordinal);
    private readonly List<Task> _background = [];
    // Every key read or written in this circuit: announced again when the signed-in user changes.
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private readonly SignedOutPreferences _signedOut = new();
    private Task<PreferenceSession?>? _opening;
    private bool _disposed;

    internal OmniPreferences(IJSRuntime javaScript, IOmniPreferenceStore? store, OmniPreferencesOptions options,
        ILogger<OmniPreferences> logger, TimeProvider time)
    {
        var interval = options.SyncInterval;
        if (interval != Timeout.InfiniteTimeSpan && interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), interval, "OmniPreferencesOptions.SyncInterval must be positive, or Timeout.InfiniteTimeSpan.");
        }

        var gap = options.MinSyncGap;
        if (gap < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), gap, "OmniPreferencesOptions.MinSyncGap must be positive, or TimeSpan.Zero.");
        }

        _javaScript = javaScript;
        _store = store;
        _storeAccess = store is null ? null : new PreferenceStoreAccess(store, logger);
        _sync = store is null ? null : new PreferenceSynchronizer(store, _storeAccess!, _cacheGate, time, logger);
        _logger = logger;
        _time = time;
        _syncInterval = interval;
        _minSyncGap = gap;
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
        PreferenceJson.Deserialize<T>(await ReadAsync(key, cancellationToken));

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
        WriteAsync(key, PreferenceJson.Serialize(value), cancellationToken);

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
    /// fails the task with its own exception; the unconfirmed values stay cached. Never limited by
    /// <see cref="OmniPreferencesOptions.MinSyncGap"/>: a call always reaches the store, which makes it the way
    /// for a host to follow a change signal it receives itself.
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
        var ran = false;
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            ran = await _sync!.SyncAsync(cache, changes, cancellationToken);
        }
        finally
        {
            _syncGate.Release();
            // Raised once the gate is free: a slow handler must not hold the next synchronization.
            await RaiseAllAsync(changes);
        }

        if (!ran)
        {
            // Another user signed in meanwhile: take them now, with the gate free.
            await OpenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Takes the signed-in user anew at once: call it after a sign-in, a sign-out or a change of account.
    /// The package also notices a change of user by itself, since it asks the store for the scope at every
    /// read, write and synchronization, but only this call raises the new user's preferences straight away
    /// instead of at the next use. The browser cache of the former user is left (it is partitioned by user),
    /// the store is asked for the new user's scope, the values written while no user was signed in are
    /// dropped, then every preference read or written in this circuit is read again for the new user and
    /// raised through <see cref="Changed"/> with <see cref="OmniPreferenceChangeOrigin.UserChanged"/>, so
    /// <see cref="OmniAppearanceState"/>, the grids with a <c>StateKey</c> and any host listener show the new
    /// user's preferences (or their defaults when the user has none). A synchronization or a write under way
    /// completes first, for the former user.
    /// </summary>
    /// <param name="cancellationToken">Cancels the change.</param>
    /// <returns>A task that completes once every preference read again has been raised.</returns>
    public async Task ResetUserAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string[] keys;
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            await _cacheGate.WaitAsync(cancellationToken);
            try
            {
                // An opening still under way ends first, so that what it starts is released with it and
                // cannot be published for the former user after the reset.
                Task<PreferenceSession?>? former;
                lock (_gate)
                {
                    former = _opening;
                }

                await EndedAsync(former, cancellationToken);
                lock (_gate)
                {
                    _opening = null;
                    _refreshed.Clear();
                    // A first read still under way answers its own callers, for the former user; a read made
                    // from now on asks the new user's cache.
                    _firstReads.Clear();
                    keys = [.. _known];
                }

                _signedOut.Clear();
                await ReleaseAsync(former);
            }
            finally
            {
                _cacheGate.Release();
            }
        }
        finally
        {
            _syncGate.Release();
        }

        foreach (var key in keys)
        {
            await RaiseAsync(new(key, await ReadAsync(key, cancellationToken), OmniPreferenceChangeOrigin.UserChanged));
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
        Task<PreferenceSession?>? opening;
        lock (_gate)
        {
            opening = _opening;
            _opening = null;
        }

        await ReleaseAsync(opening);
        _disposal.Dispose();
    }

    /// <summary>Waits for an opening to end, however it ends; a caller's cancellation is the only exception raised.</summary>
    private static async Task EndedAsync(Task<PreferenceSession?>? opening, CancellationToken cancellationToken)
    {
        if (opening is null)
        {
            return;
        }

        try
        {
            await opening.WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A failed opening released what it had started, and published nothing.
        }
    }

    /// <summary>Stops the synchronization of an opening and releases its script and its listeners.</summary>
    private static async Task ReleaseAsync(Task<PreferenceSession?>? opening)
    {
        if (opening is null)
        {
            return;
        }

        PreferenceSession? session;
        try
        {
            session = await opening;
        }
        catch (Exception)
        {
            // A failed or cancelled opening released what it had started, and published nothing.
            return;
        }

        if (session is not null)
        {
            await session.ReleaseAsync();
        }
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
        Know(key);
        var cache = await OpenAsync(cancellationToken);
        if (cache is null && _signedOut.Active)
        {
            return _signedOut.Read(key);
        }

        if (cache is null)
        {
            return _store is null ? null : (await _storeAccess!.TryGetAsync(key, cancellationToken))?.Value;
        }

        var cached = await cache.ReadAsync(key, cancellationToken);
        if (_store is null)
        {
            return cached?.Value;
        }

        Task<string?>? firstRead = null;
        TaskCompletionSource<string?>? started = null;
        var refresh = false;
        lock (_gate)
        {
            if (_refreshed.Add(key))
            {
                if (cached is not null)
                {
                    refresh = true;
                }
                else
                {
                    // Registered before it runs: a read that comes while the store answers finds it.
                    started = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    firstRead = started.Task;
                    _firstReads[key] = firstRead;
                }
            }
            else if (_firstReads.TryGetValue(key, out var pending))
            {
                firstRead = pending;
            }
        }

        if (refresh)
        {
            Track(RefreshAsync(cache, key));
            return cached!.Value;
        }

        if (started is not null)
        {
            Track(ReadFirstAsync(cache, key, started));
        }

        // A key read before, whose first read is over: the cache holds what the store said.
        return firstRead is null ? cached?.Value : await firstRead.WaitAsync(cancellationToken);
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
            var stored = await _storeAccess!.TryGetAsync(key, _stopping);
            if (stored is null)
            {
                // The store has none: what the cache holds now (a write made meanwhile, or nothing).
                result.TrySetResult((await cache.ReadAsync(key, _stopping))?.Value);
                return;
            }

            // A write made while the store answered stays, and is what the caller reads (the store value
            // is only refused over an entry cached meanwhile, so there is one when it is not applied).
            var (before, applied) = await _sync!.ApplyIfStoreWinsAsync(cache, key, stored, _stopping);
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
                if (_firstReads.TryGetValue(key, out var registered) && registered == result.Task)
                {
                    _firstReads.Remove(key);
                }
            }
        }
    }

    private async Task WriteAsync(string key, string? value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Know(key);
        var cache = await OpenAsync(cancellationToken);
        if (cache is null && _signedOut.Active)
        {
            // No user signed in: the value lives in this circuit only, neither cached nor stored.
            if (_signedOut.Write(key, value))
            {
                await RaiseAsync(new(key, value, OmniPreferenceChangeOrigin.Local));
            }

            return;
        }

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
            await _sync!.PushAsync(cache, preference, cancellationToken);
        }
    }

    private async Task RefreshAsync(PreferenceCache cache, string key)
    {
        // The caller already has the cached value; the store is asked after it resumed.
        await Task.Yield();
        await RunInBackgroundAsync(async token =>
        {
            // A synchronization that just completed brought this key too: the store is not asked again.
            if (await _sync!.SyncedWithinAsync(cache, _minSyncGap, token))
            {
                return;
            }

            if (await _storeAccess!.TryGetAsync(key, token) is { } stored && await _sync!.ApplyStoredAsync(cache, key, stored, token) is { } change)
            {
                await RaiseAsync(change);
            }
        });
    }

    /// <summary>
    /// An automatic synchronization. The periodic one is never limited; one started by a tab becoming visible
    /// (<paramref name="limited"/>) is skipped when the cache synchronized less than <see cref="OmniPreferencesOptions.MinSyncGap"/> ago.
    /// </summary>
    private async Task SyncInBackgroundAsync(PreferenceCache cache, bool limited)
    {
        await Task.Yield();
        if (!await _syncGate.WaitAsync(0, CancellationToken.None))
        {
            // A synchronization is already running; it brings the same changes.
            return;
        }

        var changes = new List<OmniPreferenceChangedEventArgs>();
        var ran = true;
        try
        {
            await RunInBackgroundAsync(async token =>
            {
                // Checked under the gate: a trigger that came while another synchronization ran finds it done.
                if (!limited || !await _sync!.SyncedWithinAsync(cache, _minSyncGap, token))
                {
                    ran = await _sync!.SyncAsync(cache, changes, token);
                }
            });
        }
        finally
        {
            _syncGate.Release();
        }

        await RaiseAllAsync(changes);
        if (!ran)
        {
            // Another user signed in meanwhile: take them now, with the gate free.
            await RunInBackgroundAsync(token => OpenAsync(token));
        }
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
        Task<Task<PreferenceSession?>>? start = null;
        Task<PreferenceSession?> opening;
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
        PreferenceSession? session;
        try
        {
            session = await opening.WaitAsync(cancellationToken);
        }
        catch when (opening.IsFaulted || opening.IsCanceled)
        {
            // A failed opening is not kept for the whole circuit: the next call tries again.
            ForgetOpening(opening);
            throw;
        }

        if (session is null)
        {
            // Prerendering, or a store that could not name its scope: the next call tries again.
            ForgetOpening(opening);
            return null;
        }

        // The scope was asked once, when the cache opened: the user may have changed since, and the cache
        // is the former user's. It is asked again at every use, so a host that never calls ResetUserAsync
        // still never reads or caches a value under the wrong user. A store busy with another call is not
        // waited for (a write is cached at once whatever the store is doing): the synchronization and the
        // write to the store check it again.
        switch (_sync is null ? false : await _sync.UserChangedAsync(session.Cache, wait: false, cancellationToken))
        {
            case false:
                return session.Cache;
            case true:
                await ResetUserAsync(cancellationToken);
                return await OpenAsync(cancellationToken);
            default:
                // The store cannot name its user now: the store alone answers until it can.
                return null;
        }
    }

    private void ForgetOpening(Task<PreferenceSession?> opening)
    {
        lock (_gate)
        {
            if (_opening == opening)
            {
                _opening = null;
            }
        }
    }

    private async Task<PreferenceSession?> OpenCoreAsync()
    {
        var scope = LocalScope;
        if (_store is not null)
        {
            try
            {
                scope = await _storeAccess!.CallAsync(store => store.GetScopeAsync(_stopping).AsTask(), _stopping);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !_stopping.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "The preference store could not give the scope of the browser cache; the store is used alone until it does.");
                return null;
            }

            // No user signed in yet: the defaults, quietly, until the store names one.
            _signedOut.Active = scope == NoUserScope;
            if (_signedOut.Active)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(scope) || scope.Contains(':', StringComparison.Ordinal))
            {
                throw new InvalidOperationException("IOmniPreferenceStore.GetScopeAsync must return a non-blank scope without a colon, or NoUserScope while no user is signed in.");
            }
        }

        IJSObjectReference module;
        try
        {
            module = await OmniScriptModule.ImportAsync(_javaScript, OmniModules.Preferences, _stopping);
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

        var session = new PreferenceSession(module, scope);
        try
        {
            await WatchAsync(session);
        }
        catch
        {
            // Released with the opening, so the next opening starts clean.
            await session.ReleaseAsync();
            throw;
        }

        return session;
    }

    private async Task WatchAsync(PreferenceSession session)
    {
        var cache = session.Cache;
        var hasStore = _store is not null;
        await session.WatchAsync(
            new PreferenceInteropBridge(
                onVisible: () => hasStore ? SyncInBackgroundAsync(cache, limited: true) : Task.CompletedTask,
                onStored: storageKey => OnStoredAsync(cache, storageKey)),
            _stopping);
        if (hasStore && _syncInterval != Timeout.InfiniteTimeSpan)
        {
            session.Schedule(_time.CreateTimer(_ => Track(SyncInBackgroundAsync(cache, limited: false)), null, _syncInterval, _syncInterval));
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

    private void Know(string key)
    {
        lock (_gate)
        {
            _known.Add(key);
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
}
