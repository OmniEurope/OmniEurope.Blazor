using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Shell family of the gallery: its pages, in menu order.</summary>
internal static class ShellFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyShell", OmniIconName.Sidebar, typeof(ShellStrings),
    [
    ]);
}
