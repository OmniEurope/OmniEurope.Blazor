namespace OmniEurope.Blazor.Components;

/// <summary>A number picked on a range: the browser's range input, with the value written beside it.</summary>
public partial class OmniSlider
{
    /// <summary>The lowest value; 0 by default. Must be finite.</summary>
    [Parameter]
    public double Minimum { get; set; }

    /// <summary>The highest value; 100 by default. Must be finite and not below <see cref="Minimum"/>.</summary>
    [Parameter]
    public double Maximum { get; set; } = 100;

    /// <summary>The increment between two values; 1 by default. Must be finite and greater than zero.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>Draws the slider upright and announces it as vertical. Off by default.</summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>Whether the slider is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

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

    private string OrientationText => Vertical ? "vertical" : "horizontal";
    private string ValueText => FormatValue?.Invoke(CurrentValue) ?? CurrentValue.ToString(CultureInfo.CurrentCulture);
    private static string Format(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    // No change handler at all without OnValueCommit: a slider that does not ask for it renders as before.
    private EventCallback<ChangeEventArgs> ChangeCallback => OnValueCommit.HasDelegate
        ? EventCallback.Factory.Create<ChangeEventArgs>(this, HandleChangeAsync)
        : default;

    private Task HandleChangeAsync(ChangeEventArgs args) =>
        double.TryParse(args.Value?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? OnValueCommit.InvokeAsync(value)
            : Task.CompletedTask;

    /// <summary>Checks the bounds, the step and the value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A bound is not finite or <see cref="Maximum"/> is below <see cref="Minimum"/>; <see cref="Step"/> is not
    /// finite and positive; or the value is not finite or lies outside the bounds.
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
        if (!double.IsFinite(Value) || Value < Minimum || Value > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(Value), "Value must be finite and within the slider bounds.");
        }
    }

    /// <summary>Accepts a number written in the invariant culture that lies within the bounds.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The number read, or 0 when the text is not a number.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized "invalid value" message.</param>
    /// <returns>True when the text is a number within <see cref="Minimum"/> and <see cref="Maximum"/>.</returns>
    protected override bool TryParseValueFromString(string? value, out double result, out string validationErrorMessage)
    {
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result)
            && result >= Minimum && result <= Maximum)
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("SliderInvalid");
        return false;
    }
}
