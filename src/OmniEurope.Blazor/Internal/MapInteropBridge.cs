namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-map.js</c> can call: the view the reader moved to (after a drag, a wheel turn, a double
/// click, a key or a zoom button), and a click on the map that was not a drag. Kept apart from the component so that the
/// callable surface is internal rather than public API.
/// </summary>
internal sealed class MapInteropBridge(Func<double, double, int, Task> onViewChanged, Func<double, double, Task> onMapClick)
{
    [JSInvokable]
    public Task OnViewChanged(double latitude, double longitude, int zoom) => onViewChanged(latitude, longitude, zoom);

    [JSInvokable]
    public Task OnMapClick(double latitude, double longitude) => onMapClick(latitude, longitude);
}
