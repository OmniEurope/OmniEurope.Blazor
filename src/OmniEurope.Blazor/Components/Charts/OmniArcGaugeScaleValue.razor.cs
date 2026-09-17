namespace OmniEurope.Blazor.Components;

public partial class OmniArcGaugeScaleValue
{
    private bool _hasMinimum;
    private bool _hasMaximum;

    /// <summary>The scale this value is drawn on, whose bounds it takes when it sets none itself.</summary>
    [CascadingParameter] private OmniArcGaugeScale? Scale { get; set; }

    [Parameter] public double Value { get; set; }

    /// <summary>Lower bound. Left unset inside an <see cref="OmniArcGaugeScale"/>, the scale's.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Upper bound. Left unset inside an <see cref="OmniArcGaugeScale"/>, the scale's.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    [Parameter] public int ColorIndex { get; set; }
    [Parameter] public Func<double, string>? Formatter { get; set; }
    [Parameter] public bool ShowValue { get; set; } = true;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Whether the markup set a bound, not whether it differs from the default: a value written
        // Minimum="0" on a scale from -20 keeps 0.
        _hasMinimum = parameters.TryGetValue<double>(nameof(Minimum), out _);
        _hasMaximum = parameters.TryGetValue<double>(nameof(Maximum), out _);
        return base.SetParametersAsync(parameters);
    }

    private double EffectiveMinimum => _hasMinimum || Scale is null ? Minimum : Scale.Minimum;
    private double EffectiveMaximum => _hasMaximum || Scale is null ? Maximum : Scale.Maximum;
    private double ClampedValue => EffectiveMaximum <= EffectiveMinimum ? EffectiveMinimum : Math.Clamp(Value, EffectiveMinimum, EffectiveMaximum);
    private double Percentage => EffectiveMaximum <= EffectiveMinimum ? 0 : (ClampedValue - EffectiveMinimum) / (EffectiveMaximum - EffectiveMinimum) * 100;
    private string ValuePath => OmniChartGeometry.Gauge(Percentage);
    private string DisplayValue => Formatter?.Invoke(ClampedValue) ?? OmniChartGeometry.Number(ClampedValue);
    private string ColorClass => $"omni-chart-color-{Math.Abs(ColorIndex) % 8}";
}
