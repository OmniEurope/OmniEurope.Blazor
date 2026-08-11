namespace OmniEurope.Blazor.Components;

public partial class OmniNullableSwitch
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool AllowIndeterminate { get; set; } = true;

    [Parameter]
    public string IndeterminateDescription { get; set; } = string.Empty;

    private string EffectiveIndeterminateDescription => string.IsNullOrWhiteSpace(IndeterminateDescription)
        ? Localize("NullableSwitchIndeterminate")
        : IndeterminateDescription;

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private void Cycle()
    {
        if (Disabled)
        {
            return;
        }

        CurrentValue = CurrentValue switch
        {
            null => true,
            true => false,
            _ when AllowIndeterminate => null,
            _ => true
        };
    }

    protected override bool TryParseValueFromString(string? value, out bool? result, out string validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        if (bool.TryParse(value, out var parsed))
        {
            result = parsed;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("NullableSwitchInvalid");
        return false;
    }
}
