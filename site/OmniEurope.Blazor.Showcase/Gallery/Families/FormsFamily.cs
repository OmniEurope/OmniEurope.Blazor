using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Forms family of the gallery: its pages, in menu order.</summary>
internal static class FormsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyForms", OmniIconName.Edit, typeof(FormsStrings),
    [
    ]);
}
