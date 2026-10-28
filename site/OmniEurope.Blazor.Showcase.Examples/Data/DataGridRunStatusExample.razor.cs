namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A compact grid of recent runs with a single selected row, a tinted sorted column and status pills.</summary>
public partial class DataGridRunStatusExample
{
    private IReadOnlyList<RunRow> SelectedRuns { get; set; } = [.. RunData.Compact.Where(run => run.Run == "#2395")];
}
