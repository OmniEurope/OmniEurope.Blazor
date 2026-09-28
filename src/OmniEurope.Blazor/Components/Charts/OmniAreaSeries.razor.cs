namespace OmniEurope.Blazor.Components;

public partial class OmniAreaSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public string? Title { get; set; }
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>
    /// Stacks the area on the other stacked areas of the chart: each value starts where the previous
    /// stacked series of the same category ended (positive and negative values stack apart), and the
    /// value axis covers the stacked totals. Off by default: the area starts at zero.
    /// </summary>
    [Parameter] public bool Stacked { get; set; }

    protected override void OnParametersSet() =>
        ChartContext?.RegisterSeries(this, Stacked ? OmniChartSeriesKind.StackedArea : OmniChartSeriesKind.Area, Data);

    private string AreaPoints => ChartContext?.AreaPoints(this, Stacked) ?? OmniChartGeometry.AreaPoints(Data);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
    private string CssClass => Stacked ? $"omni-chart__area omni-chart__area--stacked {ColorClass}" : $"omni-chart__area {ColorClass}";
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
