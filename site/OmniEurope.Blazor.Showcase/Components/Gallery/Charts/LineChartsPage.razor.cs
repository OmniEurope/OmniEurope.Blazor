using OmniEurope.Blazor.Showcase.Examples.Charts;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Charts;

/// <summary>The Lines and areas page: the kind, markers, colours, legend, tooltip and range navigator of a monthly chart, then the kinds side by side and the mixed and ranged charts.</summary>
public partial class LineChartsPage
{
    [Inject]
    private IStringLocalizer<ChartsStrings> Text { get; set; } = default!;

    private bool ShowRange { get; set; } = true;

    private bool SharedTooltip { get; set; }

    private bool Dashed { get; set; }

    private bool LegendAbove { get; set; }

    private bool AllowLegendToggle { get; set; } = true;

    private bool KeepMinWidth { get; set; } = true;

    private LineChartKind Kind { get; set; } = LineChartKind.Line;

    private bool ShowMarkers { get; set; } = true;

    private double MarkerRadius { get; set; } = 1.5;

    private double FirstColorValue { get; set; }

    private int FirstColor => (int)FirstColorValue;

    private bool ShowLegend { get; set; } = true;

    private bool ShowGridLines { get; set; } = true;

    private bool ShowDataLabels { get; set; }

    private IReadOnlyList<OmniOption<LineChartKind>> KindOptions =>
    [
        new(LineChartKind.Line, Text["LineChartsKindLine"]),
        new(LineChartKind.Area, Text["LineChartsKindArea"]),
        new(LineChartKind.StackedArea, Text["LineChartsKindStacked"])
    ];
}
