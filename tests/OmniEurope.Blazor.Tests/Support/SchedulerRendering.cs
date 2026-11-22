using System.Globalization;
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

    /// <summary>The scheduler on <see cref="Monday"/>, in UTC, in <paramref name="view"/> and <paramref name="culture"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> OnMonday(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, OmniCalendarView view, string culture = "fr-FR") =>
        parameters.InUtc(Monday, view, CultureInfo.GetCultureInfo(culture));

    /// <summary>The week of <see cref="Monday"/>, from 08:00 to 12:00, holding <paramref name="items"/>.</summary>
    public static ComponentParameterCollectionBuilder<OmniScheduler> MorningWeek(
        this ComponentParameterCollectionBuilder<OmniScheduler> parameters, IReadOnlyList<OmniSchedulerAppointment> items) =>
        parameters
            .OnMonday(OmniCalendarView.Week)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(12, 0))
            .Add(component => component.Items, items);
}
