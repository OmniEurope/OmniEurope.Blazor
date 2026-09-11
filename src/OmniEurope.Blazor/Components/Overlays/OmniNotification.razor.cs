using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniNotification
{
    /// <summary>Longest life the stylesheet has a class for, in whole seconds.</summary>
    private const int LongestLifeSeconds = 30;

    /// <summary>Beyond this length the details link is offered: the text is a report, not a message.</summary>
    private const int DetailsThreshold = 2000;

    private readonly string _messageId = $"omni-notification-message-{Guid.NewGuid():N}";
    private bool _expanded;
    private bool _pointerInside;
    private bool _focusInside;
    private bool _held;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public OmniNotificationSeverity Severity { get; set; }

    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>
    /// Tints the card in the colour of its severity and drains the tint over the time it has left.
    /// Needs <see cref="Duration"/>: a notification that stays until dismissed has nothing to count
    /// down. Off, the card stays the surface colour and still closes on time.
    /// </summary>
    [Parameter]
    public bool ShowCountdown { get; set; }

    /// <summary>How long the notification stays, when it goes on its own.</summary>
    [Parameter]
    public TimeSpan? Duration { get; set; }

    /// <summary>Where the full account of a very long message can be read.</summary>
    [Parameter]
    public string? DetailsHref { get; set; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Raised with true when the reader starts reading the notification (pointer over it or focus
    /// inside it) and with false when they stop. The host holds the countdown in between, as the
    /// stylesheet holds the tint.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnHeldChanged { get; set; }

    private string SeverityClass => $"omni-notification--{Severity.ToString().ToLowerInvariant()}";
    private string Role => Severity == OmniNotificationSeverity.Error ? "alert" : "status";
    private string LiveMode => Severity == OmniNotificationSeverity.Error ? "assertive" : "polite";
    private string MessageId => _messageId;

    private OmniIconName IconName => Severity switch
    {
        OmniNotificationSeverity.Success => OmniIconName.Check,
        OmniNotificationSeverity.Warning => OmniIconName.Warning,
        OmniNotificationSeverity.Error => OmniIconName.Close,
        _ => OmniIconName.Info
    };

    private bool IsLong => Message.Length > OmniNotificationStore.LongMessageThreshold;
    private bool Folded => IsLong && !_expanded;
    private bool ShowDetails => Message.Length > DetailsThreshold && !string.IsNullOrWhiteSpace(DetailsHref);

    /// <summary>
    /// Null when there is nothing to count down. Otherwise the tint plus the classes giving its life,
    /// rounded to the second: an inline style would be dropped by the content policy of a host that
    /// sets one, leaving a tint that never moves. The life is spelled as tens and units, which the
    /// stylesheet adds up, so twelve classes cover one to thirty seconds.
    /// </summary>
    private string? TintClass
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
                LongestLifeSeconds);
            var tens = seconds / 10;
            var units = seconds % 10;
            return CssClassBuilder.Combine([
                "omni-notification--tinted",
                tens > 0 ? $"omni-notification--t{tens.ToString(CultureInfo.InvariantCulture)}" : null,
                units > 0 ? $"omni-notification--u{units.ToString(CultureInfo.InvariantCulture)}" : null]);
        }
    }

    private void ToggleExpanded() => _expanded = !_expanded;

    private Task SetHeldAsync(bool? pointer = null, bool? focus = null)
    {
        _pointerInside = pointer ?? _pointerInside;
        _focusInside = focus ?? _focusInside;
        var held = _pointerInside || _focusInside;
        if (held == _held)
        {
            return Task.CompletedTask;
        }

        _held = held;
        return OnHeldChanged.InvokeAsync(held);
    }
}
