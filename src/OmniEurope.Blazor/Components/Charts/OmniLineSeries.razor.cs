namespace OmniEurope.Blazor.Components;

public partial class OmniLineSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public string? Title { get; set; }
    [Parameter] public int ColorIndex { get; set; }
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Line, Data);
    private string PointText => ChartContext?.Points(this) ?? OmniChartGeometry.Points(Data);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
