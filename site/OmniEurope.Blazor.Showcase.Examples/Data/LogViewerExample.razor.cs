using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A processing log with a line of each level, the error line carrying its exception; search and line wrapping are parameters.</summary>
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
        new("Looking up the case", OmniLogLevel.Trace),
        new("Connection established", OmniLogLevel.Debug),
        new("Processing started", OmniLogLevel.Information),
        new("Missing document", OmniLogLevel.Warning),
        // A line as long as real ones get, with the exception that caused it: the log scrolls sideways
        // rather than wrapping it (unless Wrap is on).
        new("Validation rejected : System.IO.IOException: The process cannot access the file 'D:\\exports\\2026-10-23\\quarterly-report-final.xlsx' because it is being used by another process.", OmniLogLevel.Error),
        new("Processing aborted", OmniLogLevel.Critical)
    ];
}
