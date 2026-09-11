namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where the map is looked at from: the offset of its origin on the canvas, in pixels, and the zoom
/// factor, between 0.1 and 5.
/// </summary>
/// <param name="PanX">Horizontal offset of the map origin, in canvas pixels.</param>
/// <param name="PanY">Vertical offset of the map origin, in canvas pixels.</param>
/// <param name="Zoom">Scale factor applied to the map.</param>
public sealed record OmniMindMapViewState(double PanX, double PanY, double Zoom);
