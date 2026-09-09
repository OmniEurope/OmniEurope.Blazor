using System.Globalization;

namespace OmniEurope.Blazor.Components;

public partial class OmniNotification
{
    /// <summary>Longest countdown the stylesheet knows, in whole seconds.</summary>
    private const int LongestCountdownSeconds = 20;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public OmniNotificationSeverity Severity { get; set; }

    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>
    /// Drains a bar along the notification for as long as it has left. Needs <see cref="Duration"/>:
    /// a notification that stays until dismissed has nothing to count down.
    /// </summary>
    [Parameter]
    public bool ShowCountdown { get; set; }

    /// <summary>How long the notification stays, when it goes on its own.</summary>
    [Parameter]
    public TimeSpan? Duration { get; set; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private string SeverityClass => $"omni-notification--{Severity.ToString().ToLowerInvariant()}";
    private string Role => Severity == OmniNotificationSeverity.Error ? "alert" : "status";
    private string LiveMode => Severity == OmniNotificationSeverity.Error ? "assertive" : "polite";

    /// <summary>
    /// Null when there is nothing to count down. Otherwise the class carrying the animation, whose
    /// length is rounded to the second: the alternative is an inline style, which the content policy
    /// of a host that sets one would drop, leaving a bar that never moves.
    /// </summary>
    private string? CountdownClass
    {
        get
        {
            if (!ShowCountdown || Duration is not { } duration || duration <= TimeSpan.Zero)
            {
                return null;
            }

            var seconds = Math.Clamp(
                (int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero),
                1,
                LongestCountdownSeconds);
            return $"omni-notification__countdown omni-notification__countdown--{seconds.ToString(CultureInfo.InvariantCulture)}s";
        }
    }
}
