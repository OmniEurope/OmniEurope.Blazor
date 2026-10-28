using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>A monthly line chart whose kind, markers, colours, legend, tooltip and range navigator are parameters.</summary>
public partial class LineChartExample
{
    /// <summary>Whether the series are lines, filled areas or stacked areas.</summary>
    [Parameter]
    public LineChartKind Kind { get; set; } = LineChartKind.Line;

    /// <summary>Whether one hover text names every series at the month under the pointer.</summary>
    [Parameter]
    public bool SharedTooltip { get; set; }

    /// <summary>Whether the series are drawn in dashes.</summary>
    [Parameter]
    public bool Dashed { get; set; }

    /// <summary>Whether the legend sits above the chart instead of where the chart finds room.</summary>
    [Parameter]
    public bool LegendAbove { get; set; }

    /// <summary>Whether a navigator under the chart picks the months drawn.</summary>
    [Parameter]
    public bool ShowRange { get; set; } = true;

    /// <summary>Whether a click on a legend entry hides its series.</summary>
    [Parameter]
    public bool AllowLegendToggle { get; set; } = true;

    /// <summary>Whether the chart keeps a readable width on a narrow screen and scrolls sideways in its card.</summary>
    [Parameter]
    public bool KeepMinWidth { get; set; } = true;

    /// <summary>Whether a dot marks each point.</summary>
    [Parameter]
    public bool ShowMarkers { get; set; } = true;

    /// <summary>The radius of the markers.</summary>
    [Parameter]
    public double MarkerRadius { get; set; } = 1.5;

    /// <summary>The rank in the chart palette of the first series; the others follow.</summary>
    [Parameter]
    public int FirstColor { get; set; }

    /// <summary>Whether the legend names each series.</summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>Whether guides are drawn behind the series.</summary>
    [Parameter]
    public bool ShowGridLines { get; set; } = true;

    /// <summary>Whether the values of the filings series are written above its points.</summary>
    [Parameter]
    public bool ShowDataLabels { get; set; }

    private int? RangeStart { get; set; }

    private int? RangeEnd { get; set; }

    /// <summary>Stacked, the three series add up: the axis must carry their sum.</summary>
    private double AxisMaximum => Kind == LineChartKind.StackedArea ? 200 : 80;

    private IReadOnlyList<LineChartSeries> Series => LineChartData.Monthly(FirstColor);

    private IEnumerable<LineChartSeries> MarkedSeries => Kind == LineChartKind.StackedArea ? Series.Take(1) : Series;
}
