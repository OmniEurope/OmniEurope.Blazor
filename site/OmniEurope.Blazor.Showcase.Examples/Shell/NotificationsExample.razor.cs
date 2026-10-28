using System.Text;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>Notifications of several lengths and roles sent to a host whose placement, countdown, dismissal and grouping are parameters.</summary>
public partial class NotificationsExample : IDisposable
{
    /// <summary>A demonstration notification stays long enough for its countdown to be read.</summary>
    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(7);

    /// <summary>The length of the trial report: enough to try the fold, the scrolling and the link to the log.</summary>
    private const int ReportLength = 4000;

    /// <summary>Where a report too long to read in a notification points: the full account lives in a log of the application.</summary>
    private const string ReportDetailsHref = "components/notifications";

    private const string RoleMessage = "A notification of this role, to compare the tints.";

    private const string VeryLongMessage =
        "The reference data synchronisation is complete. Four hundred and twelve entries were added, seventeen updated and two set aside for want of usable coordinates; the entries set aside remain in the import log and can be taken up by hand.";

    private static readonly (OmniSeverity Severity, string Title)[] Roles =
    [
        (OmniSeverity.Success, "Success"),
        (OmniSeverity.Warning, "Warning"),
        (OmniSeverity.Danger, "Error"),
        (OmniSeverity.Info, "Information")
    ];

    /// <summary>The corner or edge the stack grows from.</summary>
    [Parameter]
    public OmniNotificationPlacement Placement { get; set; } = OmniNotificationPlacement.TopEnd;

    /// <summary>Whether each card takes the tint of its role, which drains over the time it has left.</summary>
    [Parameter]
    public bool ShowCountdown { get; set; } = true;

    /// <summary>Whether each card carries a cross that sends it away without waiting.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>Whether several notifications stack behind a count and fan out on hover.</summary>
    [Parameter]
    public bool Group { get; set; }

    private OmniOverlayService Overlays { get; } = new();

    private OmniNotificationOptions Options => new(Placement, ShowCountdown, Dismissible, Group);

    private int ActionCount { get; set; }

    private void Notify(NotificationLength length) => Overlays.Notify(
        MessageFor(length),
        OmniSeverity.Info,
        title: "Test notification",
        duration: NotificationDuration,
        detailsHref: length == NotificationLength.Report ? ReportDetailsHref : null);

    /// <summary>The action is offered from the card: choosing it closes the notification, then runs here once.</summary>
    private void NotifyWithAction() => Overlays.Notify(
        "The file went to the archives.",
        OmniSeverity.Success,
        "Entry archived",
        NotificationDuration,
        "Undo",
        () => InvokeAsync(() =>
        {
            ActionCount++;
            StateHasChanged();
        }));

    private void NotifyRoles()
    {
        foreach (var (severity, title) in Roles)
        {
            Overlays.Notify(RoleMessage, severity, title, NotificationDuration);
        }
    }

    /// <summary>
    /// Several at once: grouping only shows from two, and the store keeps five, so four stay visible.
    /// </summary>
    private void NotifyBurst()
    {
        for (var index = 1; index <= 4; index++)
        {
            Overlays.Notify($"Notification {index} of four.", OmniSeverity.Info, title: "Test notification", duration: NotificationDuration);
        }
    }

    /// <summary>
    /// The paragraph repeated up to four thousand characters: a real report rather than filler, so the
    /// fold cuts in the middle of readable sentences.
    /// </summary>
    private static string ReportMessage()
    {
        var report = new StringBuilder(ReportLength + VeryLongMessage.Length);
        while (report.Length < ReportLength)
        {
            report.Append(VeryLongMessage).Append(' ');
        }

        return report.ToString(0, ReportLength);
    }

    private static string MessageFor(NotificationLength length) => length switch
    {
        NotificationLength.Short => "Saved.",
        NotificationLength.Long => "The reference data was synchronised and the missing entries were added to the local list.",
        NotificationLength.VeryLong => VeryLongMessage,
        _ => ReportMessage()
    };

    /// <inheritdoc />
    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }

    private enum NotificationLength
    {
        Short,
        Long,
        VeryLong,
        Report
    }
}
