namespace OmniEurope.Blazor.Components;

/// <summary>
/// How the notification region behaves, decided once by the host rather than per message: a stack
/// whose entries each chose their own corner, their own countdown and their own close button would
/// not read as one stack.
/// </summary>
/// <param name="Position">Which corner or edge the stack grows from.</param>
/// <param name="ShowCountdown">Whether each notification drains a bar for the time it has left.</param>
/// <param name="Dismissible">Whether each notification carries a close button.</param>
/// <param name="Group">
/// Whether several notifications collapse into one stack of cards behind a count, instead of a full
/// list. A burst of them otherwise covers the page it is talking about.
/// </param>
public sealed record OmniNotificationOptions(
    OmniNotificationPosition Position = OmniNotificationPosition.TopEnd,
    bool ShowCountdown = false,
    bool Dismissible = true,
    bool Group = false);
