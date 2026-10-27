using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Selection family of the gallery: its pages, in menu order.</summary>
internal static class SelectionFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilySelection", OmniIconName.CheckCircle, typeof(SelectionStrings),
    [
    ]);
}
