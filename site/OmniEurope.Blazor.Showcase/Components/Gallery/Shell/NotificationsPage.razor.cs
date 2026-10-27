using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Notifications page: the options of a notification stack, then the card itself placed by hand.</summary>
public partial class NotificationsPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private OmniNotificationPlacement Placement { get; set; } = OmniNotificationPlacement.TopEnd;

    private bool ShowCountdown { get; set; } = true;

    private bool Dismissible { get; set; } = true;

    private bool Group { get; set; }

    private IReadOnlyList<OmniOption<OmniNotificationPlacement>> PlacementOptions =>
    [
        new(OmniNotificationPlacement.TopStart, Text["NotificationsPlacementTopStart"]),
        new(OmniNotificationPlacement.TopCenter, Text["NotificationsPlacementTopCenter"]),
        new(OmniNotificationPlacement.TopEnd, Text["NotificationsPlacementTopEnd"]),
        new(OmniNotificationPlacement.BottomStart, Text["NotificationsPlacementBottomStart"]),
        new(OmniNotificationPlacement.BottomCenter, Text["NotificationsPlacementBottomCenter"]),
        new(OmniNotificationPlacement.BottomEnd, Text["NotificationsPlacementBottomEnd"])
    ];
}
