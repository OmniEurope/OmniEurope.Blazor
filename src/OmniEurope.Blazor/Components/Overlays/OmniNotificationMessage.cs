namespace OmniEurope.Blazor.Components;

/// <param name="Duration">
/// How long the notification is meant to stay, once the store has resolved the default. Carried on
/// the message rather than kept by the store alone, because the countdown has to know what it is
/// counting down; null means it stays until dismissed.
/// </param>
/// <param name="DetailsHref">
/// Where the full account behind a very long message can be read, a page or a detail view. Offered
/// from the notification only when its text is too long to read in one.
/// </param>
public sealed record OmniNotificationMessage(
    Guid Id,
    string Message,
    OmniNotificationSeverity Severity = OmniNotificationSeverity.Information,
    string? Title = null,
    TimeSpan? Duration = null,
    string? DetailsHref = null);
