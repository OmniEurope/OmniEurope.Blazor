namespace OmniEurope.Blazor.Components;

public partial class OmniArcGaugeScaleValue
{
    /// <summary>The scale this value is drawn on, whose bounds it takes; outside a scale, 0 to 100.</summary>
    [CascadingParameter] private OmniArcGaugeScale? Scale { get; set; }

    [Parameter] public double Value { get; set; }

    [Parameter] public int ColorIndex { get; set; }
    [Parameter] public Func<double, string>? Formatter { get; set; }
    [Parameter] public bool ShowValue { get; set; } = true;

    private double EffectiveMinimum => Scale?.Minimum ?? 0;
    private double EffectiveMaximum => Scale?.Maximum ?? 100;
    private double ClampedValue => EffectiveMaximum <= EffectiveMinimum ? EffectiveMinimum : Math.Clamp(Value, EffectiveMinimum, EffectiveMaximum);
    private double Percentage => EffectiveMaximum <= EffectiveMinimum ? 0 : (ClampedValue - EffectiveMinimum) / (EffectiveMaximum - EffectiveMinimum) * 100;
    private string ValuePath => OmniChartGeometry.Gauge(Percentage);
    private string DisplayValue => Formatter?.Invoke(ClampedValue) ?? OmniChartGeometry.Number(ClampedValue);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
}
