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
/// Only the tab-visible synchronization is limited: it does not run when the cache (shared by the
/// page loads and the tabs of the same browser and user) synchronized less than
/// <see cref="OmniPreferencesOptions.MinSyncGap"/> ago (thirty seconds by default), and concurrent
/// triggers share the one synchronization under way. A page load still re-reads from the store each
/// cached key it reads, once, so a reload shows the store's value. The periodic synchronization and
/// <see cref="SyncAsync"/> are never limited: a host that learns of a change by its own signal (a SignalR
/// hub, for instance) calls <see cref="SyncAsync"/> at once, and may set
/// <see cref="OmniPreferencesOptions.SyncInterval"/> to <see cref="Timeout.InfiniteTimeSpan"/>.
/// </para>
/// <para>
/// While the page is prerendered, without JavaScript, reads and writes go to the store directly and
/// the browser cache is left alone; without a store they read nothing and keep nothing.
/// </para>
/// <para>
/// Signed out (<see cref="NoUserScope"/>), the preferences read as their defaults by default. With
/// <see cref="OmniPreferencesOptions.SignedOut"/> set to <see cref="OmniSignedOutPreferences.Browser"/>, the visitor's
/// preferences are read and written in a browser cache of their own and the store is not called for them. At
/// sign-in, each preference the visitor changed goes to the account unless the account holds one, which
/// wins and replaces the visitor's value visible meanwhile (<see cref="OmniPreferenceChangeOrigin.UserChanged"/>);
/// the account's values are also copied into the visitor's cache, so after sign-out this browser keeps them
/// and later changes stay local.
/// </para>
/// </remarks>
public sealed class OmniPreferences : IAsyncDisposable
{
    internal const string LocalScope = "local";

    /// <summary>
    /// The scope an <see cref="IOmniPreferenceStore"/> answers while no user is signed in (an empty string): the
    /// preferences then read as <c>default</c>, so every component keeps its own defaults, and a value written
    /// meanwhile is kept in the circuit's memory only, never in the browser cache nor in the store (unless
    /// <see cref="OmniPreferencesOptions.SignedOut"/> is <see cref="OmniSignedOutPreferences.Browser"/>: the visitor's
    /// preferences are then kept in the browser, never in the store). Nothing is logged. The scope is asked again
    /// at the next read or write; <see cref="ResetUserAsync"/> applies at once the preferences of the user who signs in.
    /// </summary>
    public const string NoUserScope = "";

    private readonly IJSRuntime _javaScript;
    private readonly IOmniPreferenceStore? _store;
    private readonly ILogger<OmniPreferences> _logger;
    private readonly TimeProvider _time;
    private readonly TimeSpan _syncInterval;
    private readonly TimeSpan _minSyncGap;
    private readonly OmniSignedOutPreferences _signedOutMode;
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
    private readonly List<Task> _background = [];
    // Every key read or written in this circuit: announced again when the signed-in user changes.
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private readonly SignedOutPreferences _signedOut = new();
    private readonly PreferenceReads _reads;
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

        if (!Enum.IsDefined(options.SignedOut))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.SignedOut, "OmniPreferencesOptions.SignedOut must be a defined OmniSignedOutPreferences value.");
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
        _signedOutMode = options.SignedOut;
        _stopping = _disposal.Token;
        _reads = new PreferenceReads(this, _storeAccess, _sync, _signedOut, _stopping);
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
        // The signed-out visitor's browser cache (Browser mode) has nothing to synchronize with.
        if (_store is null || await OpenAsync(cancellationToken) is not { IsVisitor: false } cache)
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
        catch (ObjectDisposedException) when (Superseded(cache))
        {
            // A change of user released this opening meanwhile: take the user signed in now, below.
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
    /// dropped (in <see cref="OmniSignedOutPreferences.Browser"/> mode they are merged into the account instead, and
    /// the account's values stay in the visitor's browser cache), then every preference read or written in this circuit is read again for the new user and
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

                await PreferenceSession.EndedAsync(former, cancellationToken);
                lock (_gate)
                {
                    _opening = null;
                    // Every opening reads its keys anew from the store (PreferenceCache.Refreshed): a first read still
                    // under way answers its own callers, for the former user.
                    keys = [.. _known];
                }

                _signedOut.Clear();
                await PreferenceSession.ReleaseAsync(former);
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

        await PreferenceSession.ReleaseAsync(opening);
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
        Know(key);
        while (true)
        {
            var cache = await OpenAsync(cancellationToken);
            try
            {
                return await _reads.ReadFromAsync(cache, key, cancellationToken);
            }
            catch (ObjectDisposedException) when (Superseded(cache))
            {
                // A change of user released this opening while it was read: read again for the user signed in now.
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="cache"/> was released by a change of user while the service lives on: a call that failed on
    /// it is made again on the current opening.
    /// </summary>
    private bool Superseded(PreferenceCache? cache) => cache is { Released: true } && !_disposed;

    private async Task WriteAsync(string key, string? value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Know(key);
        PreferenceCache? cache;
        OmniPreference preference;
        bool? changed;
        while (true)
        {
            cache = await OpenAsync(cancellationToken);
            if (cache is null && _signedOut.Active)
            {
                // No user signed in: the value lives in this circuit only, neither cached nor stored.
                if (_signedOut.Write(key, value))
                {
                    await RaiseAsync(new(key, value, OmniPreferenceChangeOrigin.Local));
                }

                return;
            }

            preference = new OmniPreference(key, value, _time.GetUtcNow());
            try
            {
                changed = cache is null ? true : await CacheWriteAsync(cache, preference, cancellationToken);
                break;
            }
            catch (ObjectDisposedException) when (Superseded(cache))
            {
                // A change of user released this opening before the value was cached: write it for the user signed in now.
            }
        }

        if (changed is null)
        {
            return;
        }

        if (changed.Value)
        {
            await RaiseAsync(new(key, value, OmniPreferenceChangeOrigin.Local));
        }

        try
        {
            if (_store is not null && cache is not { IsVisitor: true } && await _sync!.PushAsync(cache, preference, cancellationToken) is { } kept)
            {
                await RaiseAsync(kept);
            }
        }
        catch (ObjectDisposedException) when (Superseded(cache))
        {
            // A change of user released this opening once the value was cached: it stays unconfirmed in the former user's
            // cache, which sends it at that user's next synchronization.
        }
    }

    /// <summary>
    /// Caches a written value; null when it equals the cached one (nothing to write), otherwise whether the preference
    /// changes (a removal of a value never cached does not, yet still reaches the store, which may hold it).
    /// </summary>
    private async Task<bool?> CacheWriteAsync(PreferenceCache cache, OmniPreference preference, CancellationToken cancellationToken)
    {
        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            var current = await cache.ReadAsync(preference.Key, cancellationToken);
            if (current is not null && current.Value == preference.Value)
            {
                return null;
            }

            await cache.WriteAsync(preference.Key, new(preference.Value, preference.UpdatedAt, Dirty: _store is not null), cancellationToken);
            return current is not null || preference.Value is not null;
        }
        finally
        {
            _cacheGate.Release();
        }
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

    internal async Task RunInBackgroundAsync(Func<CancellationToken, Task> work)
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

    internal async Task RaiseAsync(OmniPreferenceChangedEventArgs change)
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
                await MergeVisitorOnceAsync(session, cancellationToken);
                return session.Cache;
            case true:
                await ResetUserAsync(cancellationToken);
                return await OpenAsync(cancellationToken);
            default:
                // The store cannot name its user now: the store alone answers until it can.
                return null;
        }
    }

    /// <summary>
    /// Browser mode, an account's opening: merges once the visitor's unconfirmed preferences into the account
    /// before the first read, write or synchronization of this opening, which wait for it. Outside the opening
    /// task, so that a change of user (which waits for an opening to end) never waits on a merge. A merge
    /// that failed is logged, kept for the next call to try again, and never fails the caller.
    /// </summary>
    private async Task MergeVisitorOnceAsync(PreferenceSession session, CancellationToken cancellationToken)
    {
        if (session.Cache.Mirror is null)
        {
            return;
        }

        var changes = new List<OmniPreferenceChangedEventArgs>();
        await session.MergedAsync(async () =>
        {
            var done = false;
            await RunInBackgroundAsync(async token =>
            {
                await _sync!.MergeVisitorAsync(session.Cache, changes, token);
                done = true;
            });
            return done;
        }, cancellationToken);
        await RaiseAllAsync(changes);
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
        var visitor = _signedOut.Active && _signedOutMode == OmniSignedOutPreferences.Browser;
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

            // No user signed in yet: the defaults, quietly, until the store names one (or, in Browser mode,
            // the visitor's own browser cache, opened below).
            _signedOut.Active = scope == NoUserScope;
            visitor = _signedOut.Active && _signedOutMode == OmniSignedOutPreferences.Browser;
            if (_signedOut.Active && !visitor)
            {
                return null;
            }

            if (!visitor && (string.IsNullOrWhiteSpace(scope) || scope.Contains(':', StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("IOmniPreferenceStore.GetScopeAsync must return a non-blank scope without a colon, or NoUserScope while no user is signed in.");
            }

            if (_signedOutMode == OmniSignedOutPreferences.Browser && scope == PreferenceCache.VisitorScope)
            {
                throw new InvalidOperationException($"IOmniPreferenceStore.GetScopeAsync must not return \"{PreferenceCache.VisitorScope}\" when OmniPreferencesOptions.SignedOut is Browser: that scope is the signed-out visitor's browser cache.");
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

        var session = new PreferenceSession(module, scope, visitor, mirrorToVisitor: _store is not null && _signedOutMode == OmniSignedOutPreferences.Browser);
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
        // The visitor's cache never talks to the store: no synchronization, no unconfirmed entries to send.
        var hasStore = _store is not null && !cache.IsVisitor;
        await session.WatchAsync(
            new PreferenceInteropBridge(
                onVisible: () => hasStore ? SyncInBackgroundAsync(cache, limited: true) : Task.CompletedTask,
                onStored: storageKey => OnStoredAsync(cache, storageKey)),
            _stopping);
        if (hasStore && _syncInterval != Timeout.InfiniteTimeSpan)
        {
            session.Schedule(_time.CreateTimer(_ => Track(SyncInBackgroundAsync(cache, limited: false)), null, _syncInterval, _syncInterval));
        }

        if (hasStore && await PendingAtOpeningAsync(cache) is { Count: > 0 } pending)
        {
            Track(SendPendingAsync(cache, pending));
        }
    }

    /// <summary>
    /// The entries an earlier page left unconfirmed (a reload that interrupted a write): the first read of
    /// such a key keeps it, being later than the store's value, without sending it, so until the periodic
    /// synchronization another device read the old value (client application request). Read during the
    /// opening, before any write of this page reaches the cache, so a write made now is never taken for one of
    /// them; a cache that cannot be read sends nothing now and leaves them to the next synchronization.
    /// </summary>
    private async Task<Dictionary<string, PreferenceCacheEntry>?> PendingAtOpeningAsync(PreferenceCache cache)
    {
        try
        {
            return (await cache.EntriesAsync(_stopping)).Where(pair => pair.Value.Dirty).ToDictionary(StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !_stopping.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "The unconfirmed preferences of the browser cache could not be read at the opening; the next synchronization sends them.");
            return null;
        }
    }

    /// <summary>Sends, in the background, the entries left unconfirmed by an earlier page, without waiting for the synchronization interval.</summary>
    private async Task SendPendingAsync(PreferenceCache cache, Dictionary<string, PreferenceCacheEntry> pending)
    {
        await Task.Yield();
        var changes = new List<OmniPreferenceChangedEventArgs>();
        // Under the synchronization gate: a synchronization pushes the same entries, and the one that comes
        // second finds them confirmed instead of writing them twice.
        await RunInBackgroundAsync(async token =>
        {
            await _syncGate.WaitAsync(token);
            try
            {
                await _sync!.SendPendingAsync(cache, pending, changes, token);
            }
            finally
            {
                _syncGate.Release();
            }
        });
        await RaiseAllAsync(changes);
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

    internal void Track(Task work)
    {
        lock (_gate)
        {
            _background.RemoveAll(task => task.IsCompleted);
            _background.Add(work);
        }
    }
}
