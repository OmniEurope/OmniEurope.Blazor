using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object the list script can call: it forwards a scroll or resize of the scrolling area to
/// the list. Kept apart from the component so that the callable surface is internal rather than public API.
/// </summary>
internal sealed class ListInteropBridge(Func<double, double, Task> onViewportChanged)
{
    /// <summary>The scrolling ancestor moved or resized.</summary>
    [JSInvokable]
    public Task OnViewportChangedAsync(double scrollTop, double viewportHeight) => onViewportChanged(scrollTop, viewportHeight);
}
