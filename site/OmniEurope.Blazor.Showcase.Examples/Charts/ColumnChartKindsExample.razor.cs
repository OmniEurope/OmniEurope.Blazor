using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>Columns, bars and stacked columns on the same data, side by side.</summary>
public partial class ColumnChartKindsExample
{
    /// <summary>Whether each column of the 2026 series takes its own colour.</summary>
    [Parameter]
    public bool ColorByPoint { get; set; }
}
