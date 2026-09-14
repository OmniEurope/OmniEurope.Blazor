namespace OmniEurope.Blazor.Components;

/// <summary>The time unit an <see cref="OmniGantt"/> is drawn in, which is also how far it zooms.</summary>
public enum OmniGanttScale
{
    /// <summary>One column per day, with week-ends shaded: the closest zoom.</summary>
    Day,

    /// <summary>One column per ISO week, starting on Monday.</summary>
    Week,

    /// <summary>One column per month: the widest view.</summary>
    Month
}
