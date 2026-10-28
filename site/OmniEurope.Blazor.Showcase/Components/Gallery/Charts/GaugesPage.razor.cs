using OmniEurope.Blazor.Showcase.Examples.Charts;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Charts;

/// <summary>The Gauges page: the value, scale, value format and colour of an arc gauge, then the palette and the limits of a scale.</summary>
public partial class GaugesPage
{
    private GaugeScale _scale = GaugeScale.Percent;

    [Inject]
    private IStringLocalizer<ChartsStrings> Text { get; set; } = default!;

    private double Value { get; set; } = 72;

    /// <summary>Changing the scale puts the value back in the middle of the new one, or it would stay clamped to the old bounds.</summary>
    private GaugeScale Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Value = Math.Round((Minimum + Maximum) / 2);
        }
    }

    private GaugeFormat Format { get; set; } = GaugeFormat.Unit;

    private double ColorIndexValue { get; set; } = 1;

    private int ColorIndex => (int)ColorIndexValue;

    private double Minimum => GaugeScales.Minimum(Scale);

    private double Maximum => GaugeScales.Maximum(Scale);

    private double Step => GaugeScales.Step(Scale);

    private IReadOnlyList<OmniOption<GaugeScale>> ScaleOptions =>
    [
        new(GaugeScale.Percent, Text["GaugesScalePercent"]),
        new(GaugeScale.Files, Text["GaugesScaleFiles"]),
        new(GaugeScale.Temperature, Text["GaugesScaleTemperature"])
    ];

    private IReadOnlyList<OmniOption<GaugeFormat>> FormatOptions =>
    [
        new(GaugeFormat.Raw, Text["GaugesFormatRaw"]),
        new(GaugeFormat.Share, Text["GaugesFormatShare"]),
        new(GaugeFormat.Unit, Text["GaugesFormatUnit"])
    ];
}
