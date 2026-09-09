namespace OmniEurope.Blazor.Components;

public partial class OmniPanelMenu
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Narrow the items down to their icons. The text stays in the markup so a screen reader still
    /// announces the destination, and a title attribute is not a substitute for it.
    /// </summary>
    [Parameter]
    public OmniPanelMenuDisplayStyle DisplayStyle { get; set; } = OmniPanelMenuDisplayStyle.IconAndText;

    /// <summary>
    /// The sidebar this menu is nested in, when it is nested in one. A sidebar that keeps a rail of
    /// icons once closed decides the display style on its own: leaving that to the consumer means
    /// two places holding the same state, and one of them going stale.
    /// </summary>
    [CascadingParameter]
    private OmniSidebarState? Sidebar { get; set; }

    private OmniPanelMenuDisplayStyle EffectiveDisplayStyle => Sidebar?.IconsOnly == true
        ? OmniPanelMenuDisplayStyle.Icon
        : DisplayStyle;

    private OmniPanelMenuContext OwnContext => new(EffectiveDisplayStyle);

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PanelMenuLabel")
        : Label;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
