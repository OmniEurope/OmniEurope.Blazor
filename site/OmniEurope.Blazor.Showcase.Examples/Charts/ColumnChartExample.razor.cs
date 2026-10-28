using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>A quarterly column chart whose kind, labels, legend, grid, axes and data table are parameters.</summary>
public partial class ColumnChartExample
{
    /// <summary>Whether the series are side-by-side columns, horizontal bars or stacked columns.</summary>
    [Parameter]
    public ColumnChartKind Kind { get; set; } = ColumnChartKind.Column;

    /// <summary>Whether each column of the 2026 series takes its own colour.</summary>
    [Parameter]
    public bool ColorByPoint { get; set; }

    /// <summary>Whether the value labels sit inside the columns instead of above them.</summary>
    [Parameter]
    public bool InsideLabels { get; set; }

    /// <summary>Whether the legend names each series.</summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>Whether the values of the 2026 series are written on the chart.</summary>
    [Parameter]
    public bool ShowDataLabels { get; set; }

    /// <summary>Whether guides are drawn behind the series.</summary>
    [Parameter]
    public bool ShowGridLines { get; set; } = true;

    /// <summary>The number of bands the grid lines draw.</summary>
    [Parameter]
    public int GridLineCount { get; set; } = 5;

    /// <summary>Whether both axes carry a title.</summary>
    [Parameter]
    public bool ShowAxisTitles { get; set; } = true;

    /// <summary>
    /// Whether the value axis takes its bounds from the series, rounded to a readable step, instead of
    /// <see cref="AxisMaximum"/>.
    /// </summary>
    [Parameter]
    public bool AutoAxis { get; set; }

    /// <summary>The top of the value axis; below the stacked total, the columns are clipped.</summary>
    [Parameter]
    public double AxisMaximum { get; set; } = 150;

    /// <summary>The number of intervals written on the value axis.</summary>
    [Parameter]
    public int TickCount { get; set; } = 5;

    /// <summary>Whether the figures are written in a table under the chart.</summary>
    [Parameter]
    public bool ShowDataTable { get; set; }
}
