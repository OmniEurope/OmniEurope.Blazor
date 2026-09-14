namespace OmniEurope.Blazor.Components;

public partial class OmniValueAxis
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public double Minimum { get; set; }
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public int TickCount { get; set; } = 5;
    [Parameter] public Func<double, string>? Formatter { get; set; }

    /// <summary>The chart's layout, or one of its own when the axis is drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    private IEnumerable<double> Ticks => Enumerable.Range(0, TickCount + 1)
        .Select(index => Minimum + ((Maximum - Minimum) * index / Math.Max(1, TickCount)));

    protected override void OnParametersSet()
    {
        if (Maximum <= Minimum) throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be greater than Minimum.");
        Context.RegisterValueAxis(this, Minimum, Maximum);
    }
    private string Format(double value) => Formatter?.Invoke(value) ?? OmniChartGeometry.Number(value);
    private static string N(double value) => OmniChartGeometry.Number(value);
    public void Dispose() { ChartContext?.UnregisterValueAxis(this); GC.SuppressFinalize(this); }
}
