using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// One opening of the browser cache of <see cref="Components.OmniPreferences"/>: its <see cref="PreferenceCache"/>
/// and everything started for it (the script module, the watcher of the tab's visibility and of the other tabs'
/// writes, the periodic synchronization timer). It owns what it started and releases exactly that, so an
/// opening replaced by a change of user (<see cref="Components.OmniPreferences.ResetUserAsync"/>) never leaks
/// into, nor releases, the opening that follows it.
/// </summary>
internal sealed class PreferenceSession(IJSObjectReference module, string scope)
{
    private DotNetObjectReference<PreferenceInteropBridge>? _bridge;
    private int _watcher;
    private ITimer? _timer;

    /// <summary>The cache of the scope this opening was made for.</summary>
    internal PreferenceCache Cache { get; } = new(module, scope);

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
