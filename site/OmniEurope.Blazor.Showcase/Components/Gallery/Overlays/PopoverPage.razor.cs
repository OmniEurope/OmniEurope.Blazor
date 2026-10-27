using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Overlays;

/// <summary>The Popover page: two anchored panels, their placement, trigger variant and size as settings.</summary>
public partial class PopoverPage
{
    [Inject]
    private IStringLocalizer<OverlaysStrings> Text { get; set; } = default!;

    private OmniPopoverPlacement Placement { get; set; } = OmniPopoverPlacement.BottomEnd;

    private OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Ghost;

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private bool Disabled { get; set; }

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
    [
        new(OmniControlSize.Small, Text["PopoverSizeSmall"]),
        new(OmniControlSize.Medium, Text["PopoverSizeMedium"]),
        new(OmniControlSize.Large, Text["PopoverSizeLarge"])
    ];

    private IReadOnlyList<OmniOption<OmniPopoverPlacement>> PlacementOptions =>
    [
        new(OmniPopoverPlacement.BottomEnd, Text["PopoverPlacementEnd"]),
        new(OmniPopoverPlacement.BottomStart, Text["PopoverPlacementStart"])
    ];

    private IReadOnlyList<OmniOption<OmniButtonVariant>> VariantOptions =>
    [
        new(OmniButtonVariant.Ghost, Text["PopoverVariantGhost"]),
        new(OmniButtonVariant.Secondary, Text["PopoverVariantSecondary"]),
        new(OmniButtonVariant.Primary, Text["PopoverVariantPrimary"])
    ];
}
