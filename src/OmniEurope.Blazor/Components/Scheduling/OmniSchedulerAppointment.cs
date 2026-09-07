namespace OmniEurope.Blazor.Components;

public sealed record OmniSchedulerAppointment(
    string Id,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Description = null,
    string? RecurrenceRule = null);
