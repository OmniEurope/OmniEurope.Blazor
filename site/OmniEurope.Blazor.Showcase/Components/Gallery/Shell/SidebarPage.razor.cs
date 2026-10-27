using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Side menu page: a miniature shell whose sidebar settings change only the preview.</summary>
public partial class SidebarPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private OmniSidebarPlacement Placement { get; set; } = OmniSidebarPlacement.Start;

    private OmniPanelMenuDisplay Display { get; set; } = OmniPanelMenuDisplay.IconAndText;

    private OmniSidebarReveal Reveal { get; set; } = OmniSidebarReveal.Push;

    private OmniSidebarTransition Transition { get; set; } = OmniSidebarTransition.Smooth;

    /// <summary>The veil behind a floating menu, kept inside the preview by the containment of its stage.</summary>
    private bool Backdrop { get; set; } = true;

    private OmniSidebarCollapse Collapse { get; set; } = OmniSidebarCollapse.Icons;

    private IReadOnlyList<OmniOption<OmniSidebarPlacement>> PlacementOptions =>
    [
        new(OmniSidebarPlacement.Start, Text["SidebarPlacementStart"]),
        new(OmniSidebarPlacement.End, Text["SidebarPlacementEnd"])
    ];

    private IReadOnlyList<OmniOption<OmniPanelMenuDisplay>> DisplayOptions =>
    [
        new(OmniPanelMenuDisplay.IconAndText, Text["SidebarDisplayIconAndText"]),
        new(OmniPanelMenuDisplay.Icon, Text["SidebarDisplayIconOnly"])
    ];

    private IReadOnlyList<OmniOption<OmniSidebarReveal>> RevealOptions =>
    [
        new(OmniSidebarReveal.Push, Text["SidebarRevealPush"]),
        new(OmniSidebarReveal.Overlay, Text["SidebarRevealOverlay"])
    ];

    private IReadOnlyList<OmniOption<OmniSidebarTransition>> TransitionOptions =>
    [
        new(OmniSidebarTransition.Instant, Text["SidebarTransitionInstant"]),
        new(OmniSidebarTransition.Smooth, Text["SidebarTransitionSmooth"])
    ];

    private IReadOnlyList<OmniOption<OmniSidebarCollapse>> CollapseOptions =>
    [
        new(OmniSidebarCollapse.Hidden, Text["SidebarCollapseHidden"]),
        new(OmniSidebarCollapse.Icons, Text["SidebarCollapseIcons"])
    ];
}
