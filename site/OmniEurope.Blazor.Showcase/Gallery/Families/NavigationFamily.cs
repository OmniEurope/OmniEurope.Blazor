using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Navigation family of the gallery: its pages, in menu order.</summary>
internal static class NavigationFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyNavigation", OmniIconName.Path, typeof(NavigationStrings),
    [
    ]);
}
