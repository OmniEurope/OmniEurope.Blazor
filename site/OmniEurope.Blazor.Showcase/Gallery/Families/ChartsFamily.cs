using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Charts family of the gallery: its pages, in menu order.</summary>
internal static class ChartsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyCharts", OmniIconName.ChartBarHorizontal, typeof(ChartsStrings),
    [
    ]);
}
