using OmniEurope.Blazor.Showcase.Examples.Charts;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Charts;

/// <summary>The Columns and bars page: the kind, labels, legend, grid, axes and data table of a quarterly chart, then the three kinds side by side and two horizontal charts.</summary>
public partial class ColumnChartsPage
{
    [Inject]
    private IStringLocalizer<ChartsStrings> Text { get; set; } = default!;

    private ColumnChartKind Kind { get; set; } = ColumnChartKind.Column;

    private bool ColorByPoint { get; set; }

    private bool InsideLabels { get; set; }

    private bool ShowLegend { get; set; } = true;

    private bool ShowDataLabels { get; set; }

    private bool ShowGridLines { get; set; } = true;

    private double GridLineCountValue { get; set; } = 5;

    private int GridLineCount => (int)GridLineCountValue;

    private bool ShowAxisTitles { get; set; } = true;

    /// <summary>Below the stacked total, the columns are clipped: that is what this setting shows.</summary>
    private double AxisMaximum { get; set; } = 150;

    /// <summary>The axis then takes its bounds from the series, rounded to a readable step.</summary>
    private bool AutoAxis { get; set; }

    private double TickCountValue { get; set; } = 5;

    private int TickCount => (int)TickCountValue;

    private bool ShowDataTable { get; set; }

    private IReadOnlyList<OmniOption<ColumnChartKind>> KindOptions =>
    [
        new(ColumnChartKind.Column, Text["ColumnChartsKindColumn"]),
        new(ColumnChartKind.Bar, Text["ColumnChartsKindBar"]),
        new(ColumnChartKind.Stacked, Text["ColumnChartsKindStacked"])
    ];
}
