using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Diagram family of the gallery: its pages, in menu order.</summary>
internal static class DiagramFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyDiagram", OmniIconName.Graph, typeof(DiagramStrings),
    [
    ]);
}
