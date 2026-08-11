namespace OmniEurope.Blazor.Components;

public partial class OmniNumeric<TValue>
{
    [Parameter]
    public string? Minimum { get; set; }

    [Parameter]
    public string? Maximum { get; set; }

    [Parameter]
    public string? Step { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private void HandleChange(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (BindConverter.TryConvertTo<TValue>(value, System.Globalization.CultureInfo.CurrentCulture, out var parsedValue))
        {
            result = parsedValue;
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = string.IsNullOrWhiteSpace(DisplayName)
            ? Localize("NumericInvalid")
            : Localize("NumericInvalidNamed", DisplayName);
        return false;
    }
}
