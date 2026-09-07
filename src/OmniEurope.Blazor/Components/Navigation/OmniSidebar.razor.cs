namespace OmniEurope.Blazor.Components;

public partial class OmniSidebar
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public OmniSidebarPosition Position { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? Localize("SidebarLabel")
        : AriaLabel;
}
