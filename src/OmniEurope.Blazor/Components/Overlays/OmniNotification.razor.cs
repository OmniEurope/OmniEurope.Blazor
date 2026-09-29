using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// One notification card: a severity mark, an optional title, the message (folded past a length, with
/// a control to unfold it), an optional action, a link to the details of a very long message and a
/// close button. Announced politely, assertively for <see cref="OmniSeverity.Danger"/>. Drawn by
/// <see cref="OmniComponentsHost"/> for <see cref="OmniOverlayService.Notify(string, OmniSeverity, string?, TimeSpan?)"/>, or directly.
/// </summary>
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

    /// <summary>The text of the notification.</summary>
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>A short title above the message. Null, the default, draws none.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The severity: the mark, the colour and how the card is announced. <see cref="OmniSeverity.Info"/> by default.</summary>
    [Parameter]
    public OmniSeverity Severity { get; set; }

    /// <summary>Draws the close button, which raises <see cref="OnDismiss"/>. On by default.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>Accessible name and tooltip of the close button. Null, the default, is the localized "Dismiss notification".</summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    /// <summary>
    /// Tints the card in the colour of its severity and drains the tint over the time it has left.
    /// Needs <see cref="Duration"/>: a notification that stays until dismissed has nothing to count
    /// down. On by default; off, the card stays the surface colour and still closes on time.
    /// </summary>
    [Parameter]
    public bool ShowCountdown { get; set; } = true;

    /// <summary>How long the notification stays, when it goes on its own.</summary>
    [Parameter]
    public TimeSpan? Duration { get; set; }

    /// <summary>Where the full account of a very long message can be read.</summary>
    [Parameter]
    public string? DetailsHref { get; set; }

    /// <summary>The label of the action offered as a button; with <see cref="OnActionClick"/>, the button shows.</summary>
    [Parameter]
    public string? ActionText { get; set; }

    /// <summary>Raised when the reader chooses the action.</summary>
    [Parameter]
    public EventCallback OnActionClick { get; set; }

    /// <summary>Raised when the reader presses the close button.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Raised with true when the reader starts reading the notification (pointer over it or focus
    /// inside it) and with false when they stop. The host holds the countdown in between, as the
    /// stylesheet holds the tint.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnHeldChange { get; set; }

    // The classes keep the names they had before the severities were merged with those of the alert.
    private string SeverityClass => Severity switch
    {
        OmniSeverity.Success => "omni-notification--success",
        OmniSeverity.Warning => "omni-notification--warning",
        OmniSeverity.Danger => "omni-notification--danger",
        _ => "omni-notification--info"
    };
    private string Role => Severity == OmniSeverity.Danger ? "alert" : "status";
    private string LiveMode => Severity == OmniSeverity.Danger ? "assertive" : "polite";
    private string MessageId => _messageId;

    private string EffectiveCloseLabel => LocalizeOr(CloseLabel, "NotificationDismiss");

    // The mark is the alert disc: the same glyph per severity, on the same bright or deep fill.
    private string GlyphPath => OmniSeverityGlyph.For(Severity);

    private bool IsLong => Message.Length > OmniNotificationStore.LongMessageThreshold;
    private bool Folded => IsLong && !_expanded;
    private bool ShowDetails => Message.Length > DetailsThreshold && !string.IsNullOrWhiteSpace(DetailsHref);

    // The same policy as OmniOverlayService.Notify applies to the same link given directly.
    private string? SafeDetailsHref => OmniUriPolicy.EnsureSafe(DetailsHref, nameof(DetailsHref));

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
        return OnHeldChange.InvokeAsync(held);
    }
}
