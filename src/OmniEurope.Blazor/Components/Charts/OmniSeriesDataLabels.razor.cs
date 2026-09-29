namespace OmniEurope.Blazor.Components;

/// <summary>The value of each point written above it, or past the end of a horizontal bar.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniSeriesDataLabels
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points to label, the same as the series they decorate.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>
    /// Writes a value as text; by default the number in the current culture, at most three decimals. A
    /// point with its own <see cref="OmniChartPoint.Label"/> shows that label instead.
    /// </summary>
    [Parameter] public Func<double, string>? FormatValue { get; set; }

    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Auxiliary, Data);

    private string Text(OmniChartPoint point) => point.Label ?? FormatValue?.Invoke(point.Y) ?? OmniChartGeometry.Display(point.Y);

    private (double X, double Y) Projected(int index) =>
        ChartContext is null ? OmniChartGeometry.ProjectedPoint(Data, index) : ChartContext.ProjectCoordinates(Data[index]);

    /// <summary>Removes the labels from their chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }
}
