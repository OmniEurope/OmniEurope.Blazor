using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>Two popovers whose placement, trigger variant and size are parameters.</summary>
public partial class PopoverExample
{
    /// <summary>Where the panel opens relative to its button.</summary>
    [Parameter]
    public OmniPopoverPlacement Placement { get; set; } = OmniPopoverPlacement.BottomEnd;

    /// <summary>The variant of the trigger button.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Ghost;

    /// <summary>The size of the trigger button.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Whether the triggers are inactive: the panels do not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? Last { get; set; }
}
