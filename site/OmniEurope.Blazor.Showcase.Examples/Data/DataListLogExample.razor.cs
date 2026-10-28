using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A processing log with one line of each level, the error line carrying the exception that caused it.</summary>
public partial class DataListLogExample
{
    private IReadOnlyList<OmniLogLine> LogLines { get; } =
    [
        new("Looking up the case", OmniLogLevel.Trace),
        new("Connection established", OmniLogLevel.Debug),
        new("Processing started", OmniLogLevel.Information),
        new("Missing document", OmniLogLevel.Warning),
        // A line as long as real ones get, with the exception that caused it: the log scrolls sideways
        // rather than wrapping it.
        new("Validation rejected : System.IO.IOException: The process cannot access the file 'D:\\exports\\2026-10-23\\quarterly-report-final.xlsx' because it is being used by another process.", OmniLogLevel.Error),
        new("Processing aborted", OmniLogLevel.Critical)
    ];
}
