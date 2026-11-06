using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// One opening of the browser cache of <see cref="Components.OmniPreferences"/>: its <see cref="PreferenceCache"/>
/// and everything started for it (the script module, the watcher of the tab's visibility and of the other tabs'
/// writes, the periodic synchronization timer). It owns what it started and releases exactly that, so an
/// opening replaced by a change of user (<see cref="Components.OmniPreferences.ResetUserAsync"/>) never leaks
/// into, nor releases, the opening that follows it.
/// </summary>
internal sealed class PreferenceSession(IJSObjectReference module, string scope, bool visitor = false, bool mirrorToVisitor = false)
{
    private DotNetObjectReference<PreferenceInteropBridge>? _bridge;
    private int _watcher;
    private ITimer? _timer;
    private readonly Lock _mergeGate = new();
    private Task? _merged;

    /// <summary>The cache of the scope this opening was made for.</summary>
    internal PreferenceCache Cache { get; } = visitor
        ? new(module, PreferenceCache.VisitorScope, isVisitor: true)
        : new(module, scope, mirror: mirrorToVisitor ? new PreferenceCache(module, PreferenceCache.VisitorScope, isVisitor: true) : null);

    /// <summary>
    /// Runs <paramref name="merge"/> (the merge of the visitor's preferences into this opening's account,
    /// <see cref="Components.OmniSignedOutPreferences.Browser"/>) for the first caller, and makes every other wait for it. A merge
    /// that reports failure (false) is forgotten, so the next caller runs it again.
    /// </summary>
    internal async Task MergedAsync(Func<Task<bool>> merge, CancellationToken cancellationToken)
    {
        TaskCompletionSource? mine = null;
        Task merged;
        lock (_mergeGate)
        {
            if (_merged is null)
            {
                mine = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _merged = mine.Task;
            }

            merged = _merged;
        }

        if (mine is null)
        {
            await merged.WaitAsync(cancellationToken);
            return;
        }

        var done = false;
        try
        {
            done = await merge();
        }
        finally
        {
            if (!done)
            {
                lock (_mergeGate)
                {
                    _merged = null;
                }
            }

            mine.SetResult();
        }
    }

    /// <summary>Starts the script's watch of the scope; the bridge is released again when the watch fails.</summary>
    internal async Task WatchAsync(PreferenceInteropBridge bridge, CancellationToken cancellationToken)
    {
        var reference = DotNetObjectReference.Create(bridge);
        try
        {
            _watcher = await module.InvokeAsync<int>("watch", cancellationToken, reference, Cache.Prefix);
        }
        catch
        {
            reference.Dispose();
            throw;
        }

        _bridge = reference;
    }

    /// <summary>Keeps the periodic synchronization timer, stopped with the session.</summary>
    internal void Schedule(ITimer timer) => _timer = timer;

    /// <summary>Stops the timer, the watch and the listeners, and releases the script module.</summary>
    internal async Task ReleaseAsync()
    {
        _timer?.Dispose();
        _timer = null;
        var watcher = _watcher;
        _watcher = 0;
        try
        {
            if (watcher != 0)
            {
                await module.InvokeVoidAsync("unwatch", watcher);
            }

            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone, and the listeners with it.
        }

        _bridge?.Dispose();
        _bridge = null;
    }
}
