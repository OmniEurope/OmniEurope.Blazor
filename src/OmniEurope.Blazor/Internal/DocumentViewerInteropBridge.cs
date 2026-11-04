namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object the document viewer script can call: it reports the zoom the script computed to fit the
/// width and the page the reader is on. Kept apart from the component so that the callable surface is internal
/// rather than public API.
/// </summary>
internal sealed class DocumentViewerInteropBridge(Func<double, int, Task> onViewChanged)
{
    /// <summary>The zoom applied or the page in view changed.</summary>
    /// <param name="zoom">The zoom the script applied.</param>
    /// <param name="page">The position, from 1, of the page that fills most of the viewport; 0 without pages.</param>
    /// <returns>A task that completes once the component has taken the report.</returns>
    [JSInvokable]
    public Task OnViewChangedAsync(double zoom, int page) => onViewChanged(zoom, page);
}
