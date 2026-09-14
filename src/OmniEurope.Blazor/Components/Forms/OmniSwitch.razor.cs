namespace OmniEurope.Blazor.Components;

public partial class OmniSwitch
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
