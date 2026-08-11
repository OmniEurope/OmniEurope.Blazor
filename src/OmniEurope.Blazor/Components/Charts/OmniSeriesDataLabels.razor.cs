namespace OmniEurope.Blazor.Components;

public partial class OmniSeriesDataLabels
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public Func<double, string>? Formatter { get; set; }
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Auxiliary, Data);
    private (double X, double Y) Projected(int index)
    {
        if (ChartContext is null) return OmniChartGeometry.ProjectedPoint(Data, index);
        return ChartContext.ProjectCoordinates(Data[index]);
    }
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
