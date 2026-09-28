namespace OmniEurope.Blazor.Components;

public partial class OmniProgressBar
{
    [Parameter]
    public double Value { get; set; }

    [Parameter]
    public double Maximum { get; set; } = 100;

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string? ValueText { get; set; }

    [Parameter]
    public bool ShowValue { get; set; }

    [Parameter]
    public bool Indeterminate { get; set; }

    [Parameter]
    public OmniProgressVariant Variant { get; set; }

    [Parameter]
    public OmniProgressShape Shape { get; set; }

    private double NormalizedValue => Math.Clamp(Value, 0, Maximum);
    private double Percentage => Indeterminate ? 25 : NormalizedValue / Maximum * 100;
    private int PercentageBucket => Math.Clamp((int)(Math.Round(Percentage / 5, MidpointRounding.AwayFromZero) * 5), 0, 100);
    private string MaximumText => Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string? CurrentValueText => Indeterminate ? null : NormalizedValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("ProgressLabel") : Label;
    private string DisplayValue => ValueText ?? Localize("ProgressValue", Percentage);
    private string DashArray => Indeterminate ? "25 75" : $"{Percentage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} 100";
    private string LinearIndicatorClass => $"omni-progress__indicator omni-progress__indicator--{PercentageBucket}";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!double.IsFinite(Maximum) || !double.IsFinite(Value) || Maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), "Progress values must be finite and Maximum must be greater than zero.");
        }

    }
}
