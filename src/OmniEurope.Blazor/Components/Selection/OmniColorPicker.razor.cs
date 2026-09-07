namespace OmniEurope.Blazor.Components;

public partial class OmniColorPicker
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ShowValue { get; set; } = true;

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        if (value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit))
        {
            result = value.ToUpperInvariant();
            validationErrorMessage = null!;
            return true;
        }

        result = CurrentValue ?? "#000000";
        validationErrorMessage = Localize("ColorPickerInvalid");
        return false;
    }
}
