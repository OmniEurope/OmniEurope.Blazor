namespace OmniEurope.Blazor.Components;

public partial class OmniDatePicker
{
    [Parameter]
    public DateOnly? Minimum { get; set; }

    [Parameter]
    public DateOnly? Maximum { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    protected override string? FormatValueAsString(DateOnly? value) => FormatDate(value);

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private void HandleChange(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out DateOnly? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date)
            && (Minimum is null || date >= Minimum)
            && (Maximum is null || date <= Maximum))
        {
            result = date;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("DatePickerInvalid");
        return false;
    }
}
