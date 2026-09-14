namespace OmniEurope.Blazor.Components;

public partial class OmniNullableSwitch
{
    private readonly string _generatedId = $"omni-switch-{Guid.NewGuid():N}";

    /// <summary>The settings tile around the control, whose name labels it and whose whole surface toggles it.</summary>
    [CascadingParameter]
    private OmniSettingsTileContext? SettingsTile { get; set; }

    // Only a control inside a settings tile needs an id of its own: the label of the tile points at it.
    private string? EffectiveId => Id ?? (SettingsTile is null ? null : _generatedId);

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SettingsTile?.Join(this, EffectiveId!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SettingsTile?.Leave(this);
        }

        base.Dispose(disposing);
    }
}
