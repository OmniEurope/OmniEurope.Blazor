using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The drawing pad page: its toolbar, its shape, and whether it can be drawn on.</summary>
public partial class InkCanvasPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private bool ShowToolbar { get; set; } = true;

    private bool Wide { get; set; }

    private bool ReadOnly { get; set; }

    private bool Disabled { get; set; }
}
