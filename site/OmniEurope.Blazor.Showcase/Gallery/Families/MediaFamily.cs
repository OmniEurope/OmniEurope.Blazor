using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Media family of the gallery: its pages, in menu order.</summary>
internal static class MediaFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyMedia", OmniIconName.Monitor, typeof(MediaStrings),
    [
        // One main item and a strip of thumbnails, from the width of the stage: the call scene.
        new("media-stage", "MediaStageTitle", "MediaStageDescription", OmniIconName.Users,
            ["OmniMediaStage"],
            [
                "Media/MediaStageExample.razor", "Media/MediaStageExample.razor.cs"
            ])
    ]);
}
