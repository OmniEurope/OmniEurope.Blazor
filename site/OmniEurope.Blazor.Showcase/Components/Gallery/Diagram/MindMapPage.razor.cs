using System.Globalization;
using OmniEurope.Blazor.Showcase.Examples.Diagram;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Diagram;

/// <summary>The Mind map page: an editable map with its toolbar and properties panel, the stored format it reads and writes, and a commit graph.</summary>
public partial class MindMapPage
{
    [Inject]
    private IStringLocalizer<DiagramStrings> Text { get; set; } = default!;

    private OmniMindMapDocument Map { get; set; } = MindMapSample.Create();

    private OmniMindMapViewState? View { get; set; }

    private bool ReadOnly { get; set; }

    private bool Directed { get; set; }

    private bool ShowToolbar { get; set; } = true;

    private bool ShowPanel { get; set; } = true;

    private bool UseCustomLabels { get; set; }

    private bool ShowJson { get; set; }

    private string? SelectedLabel { get; set; }

    private string ZoomText => View is null
        ? Text["MindMapZoomFitted"]
        : View.Zoom.ToString("P0", CultureInfo.CurrentCulture);

    private void OnNodeSelected(OmniMindMapNode? node) => SelectedLabel = node?.Label;

    // A null view makes the map fit the canvas again, as at the first render.
    private void ResetView() => View = null;

    private void ResetDocument()
    {
        Map = MindMapSample.Create();
        View = null;
        SelectedLabel = null;
    }
}
