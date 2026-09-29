using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A series of rectangles, one per point, rising from zero to its value: vertical columns by default,
/// horizontal bars with <see cref="Horizontal"/>. Several series stand side by side in each category
/// band, or stack with <see cref="Stacked"/>.
/// </summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// Its point titles are hover texts; the chart's data table is what a screen reader reads.
/// </remarks>
public partial class OmniColumnSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points: <see cref="OmniChartPoint.X"/> places a point among the categories, <see cref="OmniChartPoint.Y"/> is its value.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>The name of the series, which the legend and the chart's data table show.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Rank in the palette of eight chart colours; a larger index wraps around.</summary>
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>
    /// Stacks the rectangles on the other stacked series of the same direction: all stacked series
    /// share one place in each category band, each value starting where the previous one ended
    /// (positive and negative values stack apart), and the value axis covers the stacked totals. Off
    /// by default: the series takes its own place in the band, beside the other series.
    /// </summary>
    [Parameter] public bool Stacked { get; set; }

    /// <summary>
    /// Draws horizontal bars instead of columns, which turns the whole chart: the values run along the
    /// bottom and the categories down the left. Off by default.
    /// </summary>
    [Parameter] public bool Horizontal { get; set; }

    private OmniChartSeriesKind Kind => (Horizontal, Stacked) switch
    {
        (true, true) => OmniChartSeriesKind.StackedBar,
        (true, false) => OmniChartSeriesKind.Bar,
        (false, true) => OmniChartSeriesKind.StackedColumn,
        _ => OmniChartSeriesKind.Column
    };

    /// <summary>Registers the series with its chart, as columns or bars, stacked or not, so the shared plot takes its points into account.</summary>
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, Kind, Data, Title, ColorIndex);

    private (double X, double Y, double Width, double Height) Rectangle(int index) => Horizontal
        ? ChartContext?.BarRect(this, index, Stacked) ?? OmniChartGeometry.BarRect(Data, index)
        : ChartContext?.ColumnRect(this, index, Stacked) ?? OmniChartGeometry.ColumnRect(Data, index);

    private string CssClass
    {
        get
        {
            var shape = Horizontal ? "omni-chart__bars" : "omni-chart__columns";
            return Stacked ? $"{shape} {shape}--stacked {ChartColor.Class(ColorIndex)}" : $"{shape} {ChartColor.Class(ColorIndex)}";
        }
    }

    /// <summary>Removes the series from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }
}
