using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>A value drawn on an <see cref="OmniArcGaugeScale"/>: an arc from the left end, and the value written in the middle.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its gauge, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// Its text is also part of the accessible name of the gauge.
/// </remarks>
public partial class OmniArcGaugeScaleValue : IDisposable
{
    /// <summary>The scale this value is drawn on, whose bounds it takes; outside a scale, 0 to 100.</summary>
    [CascadingParameter] private OmniArcGaugeScale? Scale { get; set; }

    /// <summary>The gauge this value names itself to.</summary>
    [CascadingParameter] private OmniArcGauge? Gauge { get; set; }

    /// <summary>The value, clamped to the bounds of the scale.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Rank in the palette of eight chart colours; a larger index wraps around.</summary>
    [Parameter] public int ColorIndex { get; set; }

    /// <summary>Writes the value as text; by default the number in the current culture, at most three decimals.</summary>
    [Parameter] public Func<double, string>? FormatValue { get; set; }

    /// <summary>Writes the value in the middle of the gauge; true by default. Hidden, it is still part of the accessible name.</summary>
    [Parameter] public bool ShowValue { get; set; } = true;

    private double EffectiveMinimum => Scale?.Minimum ?? 0;
    private double EffectiveMaximum => Scale?.Maximum ?? 100;
    private double ClampedValue => EffectiveMaximum <= EffectiveMinimum ? EffectiveMinimum : Math.Clamp(Value, EffectiveMinimum, EffectiveMaximum);
    private double Percentage => EffectiveMaximum <= EffectiveMinimum ? 0 : (ClampedValue - EffectiveMinimum) / (EffectiveMaximum - EffectiveMinimum) * 100;
    private string ValuePath => OmniChartGeometry.Gauge(Percentage);
    private string DisplayValue => FormatValue?.Invoke(ClampedValue) ?? OmniChartGeometry.Display(ClampedValue);
    private string ColorClass => ChartColor.Class(ColorIndex);

    /// <summary>Hands the displayed text of the value to its gauge, for the gauge's accessible name.</summary>
    protected override void OnParametersSet() => Gauge?.SetValue(this, DisplayValue);

    /// <summary>Removes the value from the accessible name of its gauge.</summary>
    public void Dispose()
    {
        Gauge?.RemoveValue(this);
        GC.SuppressFinalize(this);
    }
}
