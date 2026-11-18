using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Media;

/// <summary>The media tile page: the stream, how the picture fills the tile, the sound, the status icons and the drawing layer.</summary>
public partial class MediaTilePage
{
    [Inject]
    private IStringLocalizer<MediaStrings> Text { get; set; } = default!;

    private bool HasStream { get; set; } = true;

    private bool CoverChosen { get; set; }

    private bool Muted { get; set; } = true;

    private bool ShowFullscreen { get; set; } = true;

    private bool MicrophoneMuted { get; set; }

    private bool ScreenSharing { get; set; }

    private bool CameraActive { get; set; }

    private bool Drawing { get; set; } = true;

    private bool ShowDrawingToolbar { get; set; } = true;

    private OmniInkTool Tool { get; set; }

    private bool EraserChosen
    {
        get => Tool == OmniInkTool.Eraser;
        set => Tool = value ? OmniInkTool.Eraser : OmniInkTool.Pen;
    }
}
