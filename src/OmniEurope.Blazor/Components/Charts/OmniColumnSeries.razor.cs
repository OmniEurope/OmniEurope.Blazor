namespace OmniEurope.Blazor.Components;

public partial class OmniColumnSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public string? Title { get; set; }
    [Parameter] public int ColorIndex { get; set; }
    protected override void OnParametersSet() => ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Column, Data);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
