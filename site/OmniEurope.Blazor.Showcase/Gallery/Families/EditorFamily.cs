using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Editor family of the gallery: its pages, in menu order.</summary>
internal static class EditorFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyEditor", OmniIconName.BracketsCurly, typeof(EditorStrings),
    [
    ]);
}
