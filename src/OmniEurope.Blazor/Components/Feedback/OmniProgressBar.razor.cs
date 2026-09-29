namespace OmniEurope.Blazor.Components;

/// <summary>
/// How far a task has gone (<c>role="progressbar"</c>), as a line or a ring; indeterminate while the end
/// is not known.
/// </summary>
public partial class OmniProgressBar
{
    // The share of the track the moving indicator of an indeterminate bar covers: drawing only, never
    // a value, so it is neither announced nor written.
    private const double IndeterminateShare = 25;

    /// <summary>The progress, from 0 to <see cref="Maximum"/>; a value outside is drawn at the nearest end.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The value of a finished task; 100 by default. Must be finite and greater than zero.</summary>
    [Parameter]
    public double Maximum { get; set; } = 100;

    /// <summary>Accessible name of the bar; the localized "Progress" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The progress in words (<c>aria-valuetext</c>, and the text <see cref="ShowValue"/> writes), for a
    /// value a percentage does not say: "3 of 12 files". Null, the default, reads the percentage.
    /// </summary>
    [Parameter]
    public string? ValueText { get; set; }

    /// <summary>
    /// Writes the progress beside the bar: <see cref="ValueText"/>, else the percentage, else, for an
    /// indeterminate bar, the localized "In progress" (an indeterminate bar has no number to show).
    /// </summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>The end is not known: the indicator moves along the track and no value is announced.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>The colour intention of the indicator; <see cref="OmniTone.Accent"/> by default.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>A line (the default) or a ring.</summary>
    [Parameter]
    public OmniProgressShape Shape { get; set; }

    private double NormalizedValue => Math.Clamp(Value, 0, Maximum);
    private double Percentage => NormalizedValue / Maximum * 100;
    private double DrawnPercentage => Indeterminate ? IndeterminateShare : Percentage;
    private int PercentageBucket => Math.Clamp((int)(Math.Round(DrawnPercentage / 5, MidpointRounding.AwayFromZero) * 5), 0, 100);
    private string MaximumText => Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string? CurrentValueText => Indeterminate ? null : NormalizedValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string EffectiveLabel => LocalizeOr(Label, "ProgressLabel");
    private string DisplayValue => ValueText ?? (Indeterminate ? Localize("ProgressIndeterminate") : Localize("ProgressValue", Percentage));
    private string DashArray => Indeterminate ? "25 75" : $"{Percentage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} 100";
    private string LinearIndicatorClass => $"omni-progress__indicator omni-progress__indicator--{PercentageBucket}";

    /// <summary>Refuses a <see cref="Maximum"/> that is not positive and values that are not finite.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!double.IsFinite(Maximum) || !double.IsFinite(Value) || Maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), "Progress values must be finite and Maximum must be greater than zero.");
        }
    }
}
