using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Overlays;

/// <summary>The Overflow menu page: the three-dot menu of each row of a list, its trigger and entries as settings.</summary>
public partial class OverflowMenuPage
{
    [Inject]
    private IStringLocalizer<OverlaysStrings> Text { get; set; } = default!;

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private bool Disabled { get; set; }

    private bool ShowDelete { get; set; } = true;

    private bool DisableRename { get; set; }

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
    [
        new(OmniControlSize.Small, Text["OverflowMenuSizeSmall"]),
        new(OmniControlSize.Medium, Text["OverflowMenuSizeMedium"]),
        new(OmniControlSize.Large, Text["OverflowMenuSizeLarge"])
    ];
}
