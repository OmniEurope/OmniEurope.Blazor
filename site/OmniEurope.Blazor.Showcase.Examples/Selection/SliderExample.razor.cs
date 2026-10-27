using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A slider whose bounds, step, display format, orientation and state are parameters.</summary>
public partial class SliderExample
{
    private double _sliderValue = 40;

    /// <summary>The lowest value of the slider.</summary>
    [Parameter]
    public double Minimum { get; set; }

    /// <summary>The highest value of the slider.</summary>
    [Parameter]
    public double Maximum { get; set; } = 100;

    /// <summary>The gap between two positions.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>How the displayed value is written.</summary>
    [Parameter]
    public SliderFormat ValueFormat { get; set; } = SliderFormat.Raw;

    /// <summary>Whether the formatted value is written beside the slider.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Whether the slider stands upright.</summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>Whether the slider is greyed out and cannot be moved.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private double SliderValue
    {
        get => _sliderValue;
        set => _sliderValue = Math.Clamp(value, Minimum, Maximum);
    }

    /// <summary>The value of the last release, which is the only moment OnValueCommit is raised.</summary>
    private double? Committed { get; set; }

    private int CommitCount { get; set; }

    private string CommittedText => Committed is { } value
        ? $"{value.ToString(CultureInfo.InvariantCulture)}, commit no. {CommitCount}"
        : "none, release the slider";

    private string RawValue => SliderValue.ToString(CultureInfo.InvariantCulture);

    private string BoundsEcho => string.Format(
        CultureInfo.CurrentCulture,
        "Minimum {0}, maximum {1}, step {2}",
        Minimum.ToString(CultureInfo.CurrentCulture),
        Maximum.ToString(CultureInfo.CurrentCulture),
        Step.ToString(CultureInfo.CurrentCulture));

    private Func<double, string> Formatter => ValueFormat switch
    {
        SliderFormat.Percent => FormatShare,
        SliderFormat.Euro => FormatEuro,
        SliderFormat.Kilometres => FormatKilometres,
        _ => FormatRaw
    };

    /// <summary>A bound that moved brings the value back inside it before the next render.</summary>
    protected override void OnParametersSet() => _sliderValue = Math.Clamp(_sliderValue, Minimum, Maximum);

    private void OnCommitted(double value)
    {
        Committed = value;
        CommitCount++;
    }

    private static string FormatRaw(double value) => value.ToString(CultureInfo.CurrentCulture);

    private string FormatShare(double value)
    {
        var share = Maximum > Minimum ? (value - Minimum) / (Maximum - Minimum) : 0;
        return share.ToString("P0", CultureInfo.CurrentCulture);
    }

    private static string FormatEuro(double value) => $"{value.ToString("N2", CultureInfo.CurrentCulture)} €";

    private static string FormatKilometres(double value) => $"{value.ToString("0.#", CultureInfo.CurrentCulture)} km";
}
