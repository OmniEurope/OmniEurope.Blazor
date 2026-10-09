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

    /// <summary>
    /// Writes each label inside its bar, from the start of a horizontal bar or under the top of a column,
    /// outlined with the surface colour so it reads on any bar colour. A label longer than its bar runs on
    /// past its end. Off by default: above the point, or past the end of a horizontal bar.
    /// </summary>
    [Parameter] public bool Inside { get; set; }

    // Where the value axis starts (zero): a label inside a horizontal bar begins there.
    private (double X, double Y) Origin(int index) =>
        ChartContext is null ? (0, 0) : ChartContext.ProjectCoordinates(Data[index] with { Y = 0 });

    private string LabelsClass => Inside ? "omni-chart__labels omni-chart__labels--inside" : "omni-chart__labels";

    /// <summary>Registers the points with the chart so the plot covers them; data labels stay out of the legend and the data table.</summary>
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

    /// <summary>Decorates a series hidden through a legend entry: it draws nothing either.</summary>
    private bool Hidden => ChartContext?.IsDataHidden(Data) == true;
}
