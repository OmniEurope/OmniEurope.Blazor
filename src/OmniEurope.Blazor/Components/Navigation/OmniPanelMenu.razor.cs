namespace OmniEurope.Blazor.Components;

public partial class OmniPanelMenu
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PanelMenuLabel")
        : Label;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
