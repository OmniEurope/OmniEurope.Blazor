using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Data family of the gallery: its pages, in menu order.</summary>
internal static class DataFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyData", OmniIconName.Database, typeof(DataStrings),
    [
    ]);
}
