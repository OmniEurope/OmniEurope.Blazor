namespace OmniEurope.Blazor.Components;

public partial class OmniSlider
{
    [Parameter]
    public double Minimum { get; set; }

    [Parameter]
    public double Maximum { get; set; } = 100;

    [Parameter]
    public double Step { get; set; } = 1;

    [Parameter]
    public bool Vertical { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ShowValue { get; set; } = true;

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public Func<double, string>? FormatValue { get; set; }

    private string OrientationText => Vertical ? "vertical" : "horizontal";
    private string ValueText => FormatValue?.Invoke(CurrentValue) ?? Format(CurrentValue);
    private static string Format(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

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
