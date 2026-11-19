using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Maps;

/// <summary>A route through Brussels in three stretches coloured by the rain forecast, with its markers, framed by the map.</summary>
public partial class MapExample
{
    private static readonly OmniGeoPoint Flagey = new(50.8274, 4.3720);
    private static readonly OmniGeoPoint PorteDeNamur = new(50.8385, 4.3622);
    private static readonly OmniGeoPoint GareCentrale = new(50.8452, 4.3571);
    private static readonly OmniGeoPoint GareDuNord = new(50.8603, 4.3610);

    private static readonly IReadOnlyList<OmniMapLine> Route =
    [
        new() { Points = [Flagey, new(50.8300, 4.3680), new(50.8330, 4.3650), PorteDeNamur], Tone = OmniTone.Success, Tooltip = "08:00 to 08:05, dry" },
        new() { Points = [PorteDeNamur, new(50.8410, 4.3640), new(50.8443, 4.3638), GareCentrale], Tone = OmniTone.Warning, Tooltip = "08:05 to 08:10, light rain" },
        new() { Points = [GareCentrale, new(50.8467, 4.3525), new(50.8484, 4.3497), new(50.8530, 4.3530), GareDuNord], Tone = OmniTone.Danger, Width = 7, Tooltip = "08:10 to 08:15, heavy rain" }
    ];

    private static readonly IReadOnlyList<OmniMapMarker> Stops =
    [
        new() { Point = Flagey, Label = "Start: Place Flagey", Kind = OmniMapMarkerKind.Start },
        new() { Point = GareCentrale, Label = "Stop: Gare Centrale", Kind = OmniMapMarkerKind.Waypoint },
        new() { Point = GareDuNord, Label = "End: Gare du Nord", Kind = OmniMapMarkerKind.End },
        new() { Point = new(50.8396, 4.3560), Label = "Bike rental", Kind = OmniMapMarkerKind.Point }
    ];

    /// <summary>Whether the map frames the whole route (true by default) or opens on its bound centre and zoom.</summary>
    [Parameter]
    public bool Fit { get; set; } = true;

    private OmniGeoPoint Center { get; set; } = new(50.8466, 4.3528);

    private int Zoom { get; set; } = 13;

    private OmniGeoPoint? Clicked { get; set; }

    private OmniMapMarker? Pressed { get; set; }

    private void ShowClick(OmniGeoPoint point) => Clicked = point;

    private void ShowMarker(OmniMapMarker marker) => Pressed = marker;

    private static string Describe(OmniGeoPoint point)
        => string.Create(CultureInfo.InvariantCulture, $"{point.Latitude:0.00000}, {point.Longitude:0.00000}");
}
