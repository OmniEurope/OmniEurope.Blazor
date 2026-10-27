using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Foundations family of the gallery: its pages, in menu order.</summary>
internal static class FoundationsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyFoundations", OmniIconName.Layers, typeof(FoundationsStrings),
    [
    ]);
}
