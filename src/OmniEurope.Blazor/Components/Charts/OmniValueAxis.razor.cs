namespace OmniEurope.Blazor.Components;

public partial class OmniValueAxis
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public double Minimum { get; set; }
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public int TickCount { get; set; } = 5;
    [Parameter] public Func<double, string>? Formatter { get; set; }
    protected override void OnParametersSet()
    {
        if (Maximum <= Minimum) throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be greater than Minimum.");
        ChartContext?.RegisterValueAxis(this, Minimum, Maximum);
    }
    private string Format(double value) => Formatter?.Invoke(value) ?? OmniChartGeometry.Number(value);
    public void Dispose() { ChartContext?.UnregisterValueAxis(this); GC.SuppressFinalize(this); }
}
