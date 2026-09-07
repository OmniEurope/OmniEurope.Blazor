namespace OmniEurope.Blazor.Components;

public partial class OmniCheckBox
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private void HandleChange(ChangeEventArgs args)
    {
        if (args.Value is bool value)
        {
            CurrentValue = value;
        }
    }

    protected override bool TryParseValueFromString(string? value, out bool result, out string validationErrorMessage)
    {
        if (bool.TryParse(value, out result))
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("CheckBoxInvalid");
        return false;
    }
}
