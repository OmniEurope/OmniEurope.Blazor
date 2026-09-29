using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>A series drawn as a line through its points.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniLineSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points: <see cref="OmniChartPoint.X"/> along the categories, <see cref="OmniChartPoint.Y"/> the value.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>The name of the series, which the legend and the data table of the chart show.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Rank in the palette of eight chart colours; a larger index wraps around.</summary>
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>Registers the series with its chart so the shared plot takes its points into account.</summary>
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Line, Data, Title, ColorIndex);

    private string PointText => ChartContext?.Points(this) ?? OmniChartGeometry.Points(Data);

    private string ColorClass => ChartColor.Class(ColorIndex);

    /// <summary>Removes the series from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }
}
