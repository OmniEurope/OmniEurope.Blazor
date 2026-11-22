using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The parameters the scheduler test classes start from: Monday 8 June 2026, in UTC.</summary>
internal static class SchedulerRendering
{
    public static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The scheduler on <paramref name="date"/>, in UTC, in <paramref name="view"/> and <paramref name="culture"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> InUtc(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, DateTimeOffset date, OmniCalendarView view, CultureInfo culture) =>
        parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, view)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, culture);

    /// <summary>A scheduler on <paramref name="date"/> in <paramref name="view"/> and <paramref name="timeZone"/>, its appointments asked of <paramref name="load"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> Loading(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, DateTimeOffset date, OmniCalendarView view, TimeZoneInfo timeZone,
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? load) =>
        parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, view)
            .Add(component => component.TimeZone, timeZone)
            .Add(component => component.Load, load);

    /// <summary>The scheduler on <see cref="Monday"/>, in UTC, in <paramref name="view"/> and <paramref name="culture"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> OnMonday(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, OmniCalendarView view, string culture = "fr-FR") =>
        parameters.InUtc(Monday, view, CultureInfo.GetCultureInfo(culture));

    /// <summary>The week of <see cref="Monday"/>, from 08:00 to 12:00, holding <paramref name="items"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> MorningWeek(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, IReadOnlyList<OmniSchedulerAppointment> items) =>
        parameters
            .MorningWeekInTheHostCulture(items)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"));

    /// <summary>The same week, in UTC, with no culture given: the scheduler takes the culture of the host.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> MorningWeekInTheHostCulture(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, IReadOnlyList<OmniSchedulerAppointment> items) =>
        parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(12, 0))
            .Add(component => component.Items, items);

    /// <summary>The grid cell of a day (0 is Monday) and a slot (0 is the first one), wherever the orientation puts it.</summary>
    public static IElement GridCell(this IRenderedComponent<OmniScheduler> scheduler, OmniOrientation orientation, int day, int slot)
    {
        var (row, column) = orientation == OmniOrientation.Horizontal ? (day, slot) : (slot, day);
        return scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[row].QuerySelectorAll("td")[column];
    }
}
