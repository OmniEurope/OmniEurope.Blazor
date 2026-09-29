namespace OmniEurope.Blazor.Components;

/// <summary>
/// The span of time a calendar shows at once: the view of an <see cref="OmniScheduler"/> and the zoom
/// of an <see cref="OmniGantt"/>.
/// </summary>
public enum OmniCalendarView
{
    /// <summary>One day: the day list or time grid of a scheduler, one column per day (week-ends shaded) in a Gantt chart.</summary>
    Day,

    /// <summary>One week: the seven days of a scheduler from the culture's first day, one column per ISO week in a Gantt chart.</summary>
    Week,

    /// <summary>One month: the month grid of a scheduler, one column per month (the widest zoom) in a Gantt chart.</summary>
    Month
}
