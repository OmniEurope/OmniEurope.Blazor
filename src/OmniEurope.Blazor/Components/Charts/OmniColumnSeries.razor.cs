namespace OmniEurope.Blazor.Components;

public partial class OmniColumnSeries
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();
    [Parameter] public string? Title { get; set; }
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>
    /// Stacks the columns on the other stacked column series of the chart: all stacked series share one
    /// place in each category band, each value starting where the previous one ended (positive and
    /// negative values stack apart), and the value axis covers the stacked totals. Off by default: the
    /// series takes its own place in the band, side by side with the other column series.
    /// </summary>
    [Parameter] public bool Stacked { get; set; }

    protected override void OnParametersSet() =>
        ChartContext?.RegisterSeries(this, Stacked ? OmniChartSeriesKind.StackedColumn : OmniChartSeriesKind.Column, Data);

    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
    private string CssClass => Stacked ? $"omni-chart__columns omni-chart__columns--stacked {ColorClass}" : $"omni-chart__columns {ColorClass}";
    public void Dispose() { ChartContext?.UnregisterSeries(this); GC.SuppressFinalize(this); }
}
