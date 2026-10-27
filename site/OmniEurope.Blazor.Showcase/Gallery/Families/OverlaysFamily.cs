using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Overlays family of the gallery: its pages, in menu order.</summary>
internal static class OverlaysFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyOverlays", OmniIconName.PictureInPicture, typeof(OverlaysStrings),
    [
    ]);
}
