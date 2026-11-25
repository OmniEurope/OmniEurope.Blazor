namespace OmniEurope.Blazor.Components;

/// <summary>
/// The only .NET object <c>omni-media-tile.js</c> can call: the full-screen state of the tile, told at every change
/// whoever caused it (the button, Escape, the browser). Kept apart from the component so that the callable surface is
/// internal rather than public API.
/// </summary>
internal sealed class MediaTileInteropBridge(Func<bool, Task> onFullscreenChanged)
{
    [JSInvokable]
    public Task OnFullscreenChanged(bool fullscreen) => onFullscreenChanged(fullscreen);
}
