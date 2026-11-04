using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A freehand drawing pad bound to its strokes, with a sample to load and an export as a transparent PNG.</summary>
public partial class InkCanvasExample
{
    private const int ExportWidth = 600;
    private const int ExportHeightWide = 200;
    private const int ExportHeightDefault = 300;

    private OmniInkCanvas? _canvas;

    /// <summary>Shows the toolbar above the drawing area: colour, width, Undo and Clear.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>The drawing can be seen but not changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The drawing area is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The drawing area is three times wider than high, like a signature pad; otherwise it is 12rem high.</summary>
    [Parameter]
    public bool Wide { get; set; }

    private IReadOnlyList<OmniInkStroke> Strokes { get; set; } = [];

    private string? Exported { get; set; }

    private int ExportHeight => Wide ? ExportHeightWide : ExportHeightDefault;

    private void LoadSample() => Strokes = InkSamples.Flourish;

    private async Task ExportAsync()
    {
        if (_canvas is not null)
        {
            Exported = await _canvas.ExportDataUrlAsync(ExportWidth, ExportHeight);
        }
    }
}
