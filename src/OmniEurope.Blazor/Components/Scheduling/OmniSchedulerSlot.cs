namespace OmniEurope.Blazor.Components;

/// <summary>
/// The free stretch of time of an <see cref="OmniScheduler"/> the user activated: a slot of the time
/// grid, or a whole day of the week list, of the month or of the day list.
/// </summary>
/// <param name="Start">Its start, with the offset of the scheduler's time zone on that day.</param>
/// <param name="End">
/// Its end: the start plus the slot duration, capped by the end of the time grid, or the next midnight
/// for a whole day; with the offset of the scheduler's time zone on that day.
/// </param>
public sealed record OmniSchedulerSlot(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>How long it lasts, <see cref="End"/> minus <see cref="Start"/>.</summary>
    public TimeSpan Duration => End - Start;
}
