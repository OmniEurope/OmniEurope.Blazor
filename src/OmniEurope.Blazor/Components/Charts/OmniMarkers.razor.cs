using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>A dot on each point of a series, usually over the line of the same points.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniMarkers
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points to mark, the same as the series they decorate.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>Radius of each dot, in drawing units (the drawing is 100 high).</summary>
    [Parameter] public double Radius { get; set; } = 1.5;

    /// <summary>Rank in the palette of eight chart colours; a larger index wraps around.</summary>
    [Parameter] public int ColorIndex { get; set; }

    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Auxiliary, Data);

    private (double X, double Y) Projected(int index) =>
        ChartContext is null ? OmniChartGeometry.ProjectedPoint(Data, index) : ChartContext.ProjectCoordinates(Data[index]);

    private string ColorClass => ChartColor.Class(ColorIndex);

    /// <summary>Removes the markers from their chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }
}
