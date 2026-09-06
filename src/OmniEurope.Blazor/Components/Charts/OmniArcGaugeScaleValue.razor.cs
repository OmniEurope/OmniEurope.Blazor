namespace OmniEurope.Blazor.Components;

public partial class OmniArcGaugeScaleValue
{
    [Parameter] public double Value { get; set; }
    [Parameter] public double Minimum { get; set; }
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public int ColorIndex { get; set; }
    [Parameter] public Func<double, string>? Formatter { get; set; }
    private double ClampedValue => Maximum <= Minimum ? Minimum : Math.Clamp(Value, Minimum, Maximum);
    private double Percentage => Maximum <= Minimum ? 0 : (ClampedValue - Minimum) / (Maximum - Minimum) * 100;
    private string ValuePath => OmniChartGeometry.Gauge(Percentage);
    private string DisplayValue => Formatter?.Invoke(ClampedValue) ?? OmniChartGeometry.Number(ClampedValue);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
}
