namespace OmniEurope.Blazor.Components;

public partial class OmniAppearanceToggle
{
    [Parameter]
    public OmniAppearance Appearance { get; set; }

    [Parameter]
    public EventCallback<OmniAppearance> AppearanceChanged { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? Localize("AppearanceAriaLabel")
        : AriaLabel;

    private string DisplayLabel => Appearance switch
    {
        OmniAppearance.Light => Localize("AppearanceLight"),
        OmniAppearance.Dark => Localize("AppearanceDark"),
        _ => Localize("AppearanceSystem")
    };

    private Task CycleAsync() => AppearanceChanged.InvokeAsync(Appearance switch
    {
        OmniAppearance.System => OmniAppearance.Light,
        OmniAppearance.Light => OmniAppearance.Dark,
        _ => OmniAppearance.System
    });
}
