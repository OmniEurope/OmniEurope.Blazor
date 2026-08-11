namespace OmniEurope.Blazor.Components;

public partial class OmniSwitch
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private void Toggle()
    {
        if (!Disabled)
        {
            CurrentValue = !CurrentValue;
        }
    }

    protected override bool TryParseValueFromString(string? value, out bool result, out string validationErrorMessage)
    {
        if (bool.TryParse(value, out result))
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("SwitchInvalid");
        return false;
    }
}
