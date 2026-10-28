using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A grid that fills a fixed-height frame, with its pager and its lines chosen above it.</summary>
public partial class DataGridFillBoxExample
{
    // « none » scrolls the grid virtually, a position pages it with the pager there.
    private readonly IReadOnlyList<OmniOption<string>> _pagerOptions =
    [
        new("none", "None"),
        new("bottom", "Bottom"),
        new("top", "Top"),
        new("both", "Top and bottom")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridLines>> _lineOptions =
    [
        new(OmniDataGridLines.None, "None"),
        new(OmniDataGridLines.Horizontal, "Horizontal"),
        new(OmniDataGridLines.Vertical, "Vertical"),
        new(OmniDataGridLines.Both, "Both")
    ];

    private string _pager = "none";

    private OmniDataGridLines _lines = OmniDataGridLines.Horizontal;

    private bool Paged => _pager != "none";

    private OmniDataGridPlacement PagerPlacement => _pager switch
    {
        "top" => OmniDataGridPlacement.Top,
        "both" => OmniDataGridPlacement.TopAndBottom,
        _ => OmniDataGridPlacement.Bottom
    };
}
