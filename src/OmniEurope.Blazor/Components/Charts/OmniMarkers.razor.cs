namespace OmniEurope.Blazor.Components;

public partial class OmniMarkers
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public double Radius { get; set; } = 1.5;
    [Parameter] public int ColorIndex { get; set; }
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Auxiliary, Data);
    private (double X, double Y) Projected(int index)
    {
        if (ChartContext is null) return OmniChartGeometry.ProjectedPoint(Data, index);
        return ChartContext.ProjectCoordinates(Data[index]);
    }
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
