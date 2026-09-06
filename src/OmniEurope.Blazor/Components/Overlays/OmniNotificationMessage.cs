namespace OmniEurope.Blazor.Components;

public sealed record OmniNotificationMessage(
    Guid Id,
    string Message,
    OmniNotificationSeverity Severity = OmniNotificationSeverity.Information,
    string? Title = null);
