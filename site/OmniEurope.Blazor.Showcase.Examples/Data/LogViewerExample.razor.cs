using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A log viewer on two lines whose search field and line wrapping are parameters.</summary>
public partial class LogViewerExample
{
    /// <summary>Whether the search field is shown above the lines.</summary>
    [Parameter]
    public bool ShowSearch { get; set; } = true;

    /// <summary>Whether a long line folds instead of scrolling sideways.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    private IReadOnlyList<OmniLogLine> Lines { get; } =
    [
        new("Processing started", OmniLogLevel.Information),
        new("Check required", OmniLogLevel.Warning)
    ];
}
