using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object the log viewer script can call: it forwards a scroll or resize of the viewport,
/// with the script's verdict on following the tail. Kept apart from the component so that the callable
/// surface is internal rather than public API.
/// </summary>
internal sealed class LogViewerInteropBridge(Func<double, double, bool, bool, Task> onViewportChanged)
{
    /// <summary>The viewport scrolled or resized.</summary>
    [JSInvokable]
    public Task OnViewportChangedAsync(double scrollTop, double viewportHeight, bool atBottom, bool following) =>
        onViewportChanged(scrollTop, viewportHeight, atBottom, following);
}
