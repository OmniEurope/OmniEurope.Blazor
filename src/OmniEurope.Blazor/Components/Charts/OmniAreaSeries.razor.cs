using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>A series drawn as a filled area between its line and zero, or on the stacked areas below it.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniAreaSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points: <see cref="OmniChartPoint.X"/> along the categories, <see cref="OmniChartPoint.Y"/> the value.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>The name of the series, which the legend and the data table of the chart show.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Rank in the palette of eight chart colours; a larger index wraps around.</summary>
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>
    /// Stacks the area on the other stacked areas of the chart: each value starts where the previous
    /// stacked series of the same category ended (positive and negative values stack apart), and the
    /// value axis covers the stacked totals. Off by default: the area starts at zero.
    /// </summary>
    [Parameter] public bool Stacked { get; set; }

    /// <summary>
    /// Draws the outline of the area in dashes (<c>omni-chart__area--dashed</c>), the fill unchanged,
    /// for a target, a forecast or a series to set apart from the others; the dashes keep their screen
    /// size like the stroke. Off by default: a solid outline.
    /// </summary>
    [Parameter] public bool Dashed { get; set; }

    /// <summary>Registers the series, plain or stacked, with its chart so the shared plot takes its points into account.</summary>
    protected override void OnParametersSet() =>
        ChartContext?.RegisterSeries(this, Stacked ? OmniChartSeriesKind.StackedArea : OmniChartSeriesKind.Area, Data, Title, ColorIndex);

    private string AreaPoints => ChartContext?.AreaPoints(this, Stacked) ?? OmniChartGeometry.AreaPoints(Data);

    private string ColorClass => ChartColor.Class(ColorIndex);

    private string CssClass =>
        $"omni-chart__area{(Stacked ? " omni-chart__area--stacked" : null)}{(Dashed ? " omni-chart__area--dashed" : null)} {ColorClass}";

    /// <summary>Removes the series from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }
}
