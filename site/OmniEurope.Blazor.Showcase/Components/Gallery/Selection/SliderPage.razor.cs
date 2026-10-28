using System.Globalization;
using OmniEurope.Blazor.Showcase.Examples.Selection;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Slider page: the bounds, step, display format, orientation and state of a slider, then more sliders and a selectable minimum.</summary>
public partial class SliderPage
{
    private static readonly double[] Steps = [0.5, 1, 5, 10];

    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    /// <summary>The ranges of the two bound settings do not overlap, so the minimum never passes the maximum.</summary>
    private double Minimum { get; set; }

    private double Maximum { get; set; } = 100;

    private double Step { get; set; } = 1;

    private SliderFormat ValueFormat { get; set; } = SliderFormat.Raw;

    private bool ShowValue { get; set; } = true;

    private bool Vertical { get; set; }

    private bool SliderDisabled { get; set; }

    private static IReadOnlyList<OmniOption<double>> StepOptions =>
    [
        .. Steps.Select(step => new OmniOption<double>(step, step.ToString(CultureInfo.CurrentCulture)))
    ];

    private IReadOnlyList<OmniOption<SliderFormat>> FormatOptions =>
    [
        new(SliderFormat.Raw, Text["SliderFormatRaw"]),
        new(SliderFormat.Percent, Text["SliderFormatPercent"]),
        new(SliderFormat.Euro, Text["SliderFormatEuro"]),
        new(SliderFormat.Kilometres, Text["SliderFormatKilometres"])
    ];
}
