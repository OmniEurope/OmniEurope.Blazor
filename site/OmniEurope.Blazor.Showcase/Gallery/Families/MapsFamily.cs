using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Maps family of the gallery: its pages, in menu order.</summary>
internal static class MapsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyMaps", OmniIconName.MapPin, typeof(MapsStrings),
    [
        // A route in stretches coloured by the weather, its markers, framed by the map; pan, zoom, clicks.
        new("map", "MapTitle", "MapDescription", OmniIconName.Path,
            ["OmniMap"],
            [
                "Maps/MapExample.razor", "Maps/MapExample.razor.cs"
            ])
    ]);
}
