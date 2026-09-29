namespace OmniEurope.Blazor.Components;

/// <summary>An appointment of <see cref="OmniScheduler"/>.</summary>
/// <param name="Id">Identifies the appointment among the others, and names it to the host when it is clicked or moved.</param>
/// <param name="Title">The name shown on the appointment.</param>
/// <param name="Start">When it starts.</param>
/// <param name="End">When it ends.</param>
/// <param name="Description">A line shown under the title in the day list.</param>
public sealed record OmniSchedulerAppointment(
    string Id,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Description = null)
{
    /// <summary>Classes added to the appointment's element, for a host that marks some appointments apart.</summary>
    public string? Class { get; init; }
}
