namespace OmniEurope.Blazor.Components;

/// <summary>
/// One notification of the stack. The positional members are the ones the record was published
/// with; what came later is carried by init properties, so the constructor and the deconstruction
/// every consumer is compiled against stay exactly as they were.
/// </summary>
public sealed record OmniNotificationMessage(
    Guid Id,
    string Message,
    OmniNotificationSeverity Severity = OmniNotificationSeverity.Information,
    string? Title = null)
{
    /// <summary>
    /// How long the notification is meant to stay, once the store has resolved the default. Carried
    /// on the message because the countdown has to know what it is counting down; null means it
    /// stays until dismissed.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Where the full account behind a very long message can be read, a page or a detail view.
    /// Offered from the notification only when its text is too long to read in one.
    /// </summary>
    public string? DetailsHref { get; init; }
}
