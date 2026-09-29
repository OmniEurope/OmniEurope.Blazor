namespace OmniEurope.Blazor.Components;

/// <summary>
/// A vertical navigation menu of <see cref="OmniPanelMenuItem"/>, groups folding their entries, the
/// entry of the current route marked; down to its icons on the rail of an <see cref="OmniSidebar"/>.
/// </summary>
public partial class OmniPanelMenu
{
    /// <summary>Accessible name of the navigation landmark. Null, the default, is the localized "Navigation".</summary>
    [Parameter]
    public string? Label { get; set; }

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

    private OmniPanelMenuContext OwnContext => new(EffectiveDisplayStyle) { ExpandSidebar = Sidebar?.Expand, CanNavigate = CanNavigate };

    private string EffectiveLabel => LocalizeOr(Label, "PanelMenuLabel");

    /// <summary>
    /// Asked, with the address, before any entry of the menu navigates: the entry navigates only when
    /// it answers true, which lets the host keep a page with unsaved changes or handle the choice
    /// itself. Null, the default, lets the links navigate as plain links.
    /// </summary>
    [Parameter]
    public Func<string, Task<bool>>? CanNavigate { get; set; }

    /// <summary>The entries of the menu, <see cref="OmniPanelMenuItem"/> in their order, groups nesting their own.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
