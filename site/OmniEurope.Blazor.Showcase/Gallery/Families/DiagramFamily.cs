using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Diagram family of the gallery: its pages, in menu order.</summary>
internal static class DiagramFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyDiagram", OmniIconName.Graph, typeof(DiagramStrings),
    [
        new("mind-map", "MindMapTitle", "MindMapDescription", OmniIconName.Graph,
            ["OmniMindMap", "OmniMindMapToolbar", "OmniMindMapNodeProperties", "OmniGitGraph"],
            [
                "Diagram/MindMapExample.razor", "Diagram/MindMapExample.razor.cs",
                "Diagram/MindMapJsonExample.razor", "Diagram/MindMapJsonExample.razor.cs",
                "Diagram/GitGraphExample.razor", "Diagram/GitGraphExample.razor.cs",
                "Diagram/MindMapSample.cs"
            ])
    ]);
}
