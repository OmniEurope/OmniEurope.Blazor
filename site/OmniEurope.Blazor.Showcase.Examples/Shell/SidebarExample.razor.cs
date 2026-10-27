using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>A miniature application shell whose sidebar placement, rendering, opening, collapse and motion are parameters.</summary>
public partial class SidebarExample
{
    /// <summary>The side of the shell the menu sits on.</summary>
    [Parameter]
    public OmniSidebarPlacement Placement { get; set; } = OmniSidebarPlacement.Start;

    /// <summary>Whether the menu shows its icons with their text, or the icons alone.</summary>
    [Parameter]
    public OmniPanelMenuDisplay Display { get; set; } = OmniPanelMenuDisplay.IconAndText;

    /// <summary>Whether opening pushes the content aside or floats over it.</summary>
    [Parameter]
    public OmniSidebarReveal Reveal { get; set; } = OmniSidebarReveal.Push;

    /// <summary>How a pushing menu opens and closes: at once, or sliding.</summary>
    [Parameter]
    public OmniSidebarTransition Transition { get; set; } = OmniSidebarTransition.Smooth;

    /// <summary>Whether a floating menu dims the content behind it; a click on the veil closes the menu.</summary>
    [Parameter]
    public bool Backdrop { get; set; } = true;

    /// <summary>Whether a closed menu leaves a rail of icons or disappears.</summary>
    [Parameter]
    public OmniSidebarCollapse Collapse { get; set; } = OmniSidebarCollapse.Icons;

    private bool MenuOpen { get; set; } = true;
}
