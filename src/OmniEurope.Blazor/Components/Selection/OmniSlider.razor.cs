namespace OmniEurope.Blazor.Components;

/// <summary>A number picked on a range: the browser's range input, with the value written beside it.</summary>
public partial class OmniSlider
{
    /// <summary>The lowest value; 0 by default. Must be finite.</summary>
    [Parameter]
    public double Minimum { get; set; }

    /// <summary>
    /// The lowest value the reader can pick, when it lies above <see cref="Minimum"/>; null (the default) lets the
    /// whole scale be picked. The track still starts at <see cref="Minimum"/>, so the thumb sits at the true
    /// proportion of the scale (5 of a 0 to 10 scale is in the middle), but a drag or a key press below this value
    /// is brought back to it. Must be finite and within <see cref="Minimum"/> and <see cref="Maximum"/>.
    /// </summary>
    [Parameter]
    public double? SelectableMinimum { get; set; }

    /// <summary>The highest value; 100 by default. Must be finite and not below <see cref="Minimum"/>.</summary>
    [Parameter]
    public double Maximum { get; set; } = 100;

    /// <summary>The increment between two values; 1 by default. Must be finite and greater than zero.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>The direction of the slider, drawn and announced as such (<c>aria-orientation</c>): <see cref="OmniOrientation.Horizontal"/> by default.</summary>
    [Parameter]
    public OmniOrientation Orientation { get; set; }

    /// <summary>Whether the slider is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the value can be read and the slider focused but not changed: a drag or a key press leaves
    /// the value as it is (<c>aria-readonly</c>) and the thumb is drawn back on it. Not dimmed. Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether the value is written beside the slider, as an <c>output</c>; true by default.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Identifiers of the elements that describe the slider, written as <c>aria-describedby</c>; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// Writes the value as text, for the value shown and <c>aria-valuetext</c>; null (the default) writes
    /// the number in the current culture (<c>0,5</c> in French). The input's own numbers
    /// (<c>value</c>, <c>min</c>, <c>max</c>, <c>aria-valuenow</c>) stay in the invariant culture.
    /// </summary>
    [Parameter]
    public Func<double, string>? FormatValue { get; set; }

    /// <summary>
    /// Raised once when the reader lets go of the thumb (or commits a key press), with the final value.
    /// <c>ValueChanged</c> still follows every step of the drag; this one suits an action that must run
    /// once, such as seeking a media position. Unset, nothing more happens on release.
    /// </summary>
    [Parameter]
    public EventCallback<double> OnValueCommit { get; set; }

    private double LowestSelectable => SelectableMinimum ?? Minimum;
    private string OrientationText => Orientation == OmniOrientation.Vertical ? "vertical" : "horizontal";
    // A value outside the selectable bounds is drawn and announced at the nearest one, as the progress bar does;
    // the bound model keeps its value until the reader moves the thumb. A value that is not a number sits at the bottom.
    private double DisplayValue => double.IsNaN(Value) ? LowestSelectable : Math.Clamp(Value, LowestSelectable, Maximum);
    private string DisplayValueAsString => Format(DisplayValue);
    private string ValueText => FormatValue?.Invoke(DisplayValue) ?? DisplayValue.ToString(CultureInfo.CurrentCulture);
    private static string Format(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private void HandleInput(string? text)
    {
        if (!ReadOnly)
        {
            CurrentValueAsString = Clamped(text);
        }
    }

    /// <summary>Writes the value as the input does, in the invariant culture, whatever the culture of the page.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The number as the range input reads it.</returns>
    protected override string? FormatValueAsString(double value) => Format(value);

    // A value under the selectable minimum is written as the minimum. The render that follows the event
    // puts the thumb back there, whatever the browser drew.
    private string? Clamped(string? text) =>
        SelectableMinimum is { } lowest
        && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)
        && number < lowest
            ? Format(lowest)
            : text;

    // No change handler at all without OnValueCommit: a slider that does not ask for it renders as before.
    private EventCallback<ChangeEventArgs> ChangeCallback => OnValueCommit.HasDelegate
        ? EventCallback.Factory.Create<ChangeEventArgs>(this, HandleChangeAsync)
        : default;

    private Task HandleChangeAsync(ChangeEventArgs args) => ReadOnly ? Task.CompletedTask :
        double.TryParse(Clamped(args.Value?.ToString()), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? OnValueCommit.InvokeAsync(value)
            : Task.CompletedTask;

    /// <summary>Checks the bounds and the step. A value outside them is drawn at the nearest bound, not refused.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A bound is not finite or <see cref="Maximum"/> is below <see cref="Minimum"/>; <see cref="Step"/> is not
    /// finite and positive; or <see cref="SelectableMinimum"/> is not finite or lies outside the bounds.
    /// </exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!double.IsFinite(Minimum) || !double.IsFinite(Maximum) || Maximum < Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be finite and greater than or equal to Minimum.");
        }
        if (!double.IsFinite(Step) || Step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Step), "Step must be finite and greater than zero.");
        }
        if (SelectableMinimum is { } lowest && (!double.IsFinite(lowest) || lowest < Minimum || lowest > Maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(SelectableMinimum), "SelectableMinimum must be finite and within Minimum and Maximum.");
        }
    }

    /// <summary>Accepts a number written in the invariant culture that lies within the selectable bounds.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The number read, or 0 when the text is not a number.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized "invalid value" message.</param>
    /// <returns>True when the text is a number from <see cref="SelectableMinimum"/> (else <see cref="Minimum"/>) to <see cref="Maximum"/>.</returns>
    protected override bool TryParseValueFromString(string? value, out double result, out string validationErrorMessage)
    {
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result)
            && result >= LowestSelectable && result <= Maximum)
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("SliderInvalid");
        return false;
    }
}
