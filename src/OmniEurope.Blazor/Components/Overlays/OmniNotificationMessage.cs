namespace OmniEurope.Blazor.Components;

/// <param name="Duration">
/// How long the notification is meant to stay, once the store has resolved the default. Carried on
/// the message rather than kept by the store alone, because a countdown bar has to know what it is
/// counting down; null means it stays until dismissed.
/// </param>
public sealed record OmniNotificationMessage(
    Guid Id,
    string Message,
    OmniNotificationSeverity Severity = OmniNotificationSeverity.Information,
    string? Title = null,
    TimeSpan? Duration = null);
