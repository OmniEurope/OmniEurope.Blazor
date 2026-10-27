using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Layout family of the gallery: its pages, in menu order.</summary>
internal static class LayoutFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyLayout", OmniIconName.Rows, typeof(LayoutStrings),
    [
    ]);
}
