using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class SchedulerPastSlotTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    // Tuesday 9 June 2026, 09:30 UTC.
    private static readonly DateTimeOffset Now = Monday.AddDays(1).AddHours(9).AddMinutes(30);

    public SchedulerPastSlotTests() => Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));

    private IRenderedComponent<OmniScheduler> RenderScheduler(
        OmniCalendarView view,
        bool timeGrid,
        bool markPast = true,
        TimeZoneInfo? timeZone = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.View, view)
                .Add(component => component.TimeZone, timeZone ?? TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"))
                .Add(component => component.MarkPastSlots, markPast);
            if (timeGrid)
            {
                parameters
                    .Add(component => component.DayStart, new TimeOnly(8, 0))
                    .Add(component => component.DayEnd, new TimeOnly(12, 0));
            }
        });

    [Theory]
    [InlineData(OmniCalendarView.Week, true)]
    [InlineData(OmniCalendarView.Week, false)]
    [InlineData(OmniCalendarView.Month, false)]
    public void WithoutMarkPastSlots_NothingIsMarked(OmniCalendarView view, bool timeGrid)
    {
        var scheduler = RenderScheduler(view, timeGrid, markPast: false);

        Assert.Empty(scheduler.FindAll(".omni-scheduler__past"));
    }

    [Fact]
    public void TimeGrid_MarksEverySlotWhoseEndHasPassed()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Week, timeGrid: true);

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        bool Past(int row, int column) => rows[row].QuerySelectorAll("td")[column].ClassList.Contains("omni-scheduler__past");

        // Monday is over; on Tuesday, 08:00-09:00 is over, 09:00-10:00 is under way; Wednesday is ahead.
        Assert.All(Enumerable.Range(0, 4), row => Assert.True(Past(row, 0)));
        Assert.True(Past(0, 1));
        Assert.False(Past(1, 1));
        Assert.False(Past(3, 1));
        Assert.All(Enumerable.Range(0, 4), row => Assert.False(Past(row, 2)));
        Assert.Equal(4 + 1, scheduler.FindAll(".omni-scheduler__past").Count);
        // Today keeps its own mark next to the past one: the hatch is drawn over the tint.
        Assert.Contains("omni-scheduler-grid__cell--today", rows[0].QuerySelectorAll("td")[1].ClassList);
        Assert.DoesNotContain("style=", scheduler.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MonthAndWeekList_MarkTheDaysBeforeToday()
    {
        var month = RenderScheduler(OmniCalendarView.Month, timeGrid: false);
        Assert.Contains("omni-scheduler__past", month.Find("[data-date='2026-06-01']").ClassList);
        Assert.Contains("omni-scheduler__past", month.Find("[data-date='2026-06-08']").ClassList);
        Assert.DoesNotContain("omni-scheduler__past", month.Find("[data-date='2026-06-09']").ClassList);
        Assert.DoesNotContain("omni-scheduler__past", month.Find("[data-date='2026-06-30']").ClassList);
        Assert.Equal(8, month.FindAll(".omni-scheduler__past").Count);

        var week = RenderScheduler(OmniCalendarView.Week, timeGrid: false);
        var days = week.FindAll(".omni-week-view__day");
        Assert.True(days[0].ClassList.Contains("omni-scheduler__past"));
        Assert.False(days[1].ClassList.Contains("omni-scheduler__past"));
    }

    [Fact]
    public void DayList_IsMarkedWhenTheDayIsOver()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Day, timeGrid: false);

        Assert.Contains("omni-scheduler__past", scheduler.Find(".omni-day-view").ClassList);
    }

    [Fact]
    public void Now_IsReadInTheSchedulerTimeZone()
    {
        // 09:30 UTC is 11:30 in Paris: the Paris slots 08:00 to 11:00 of Tuesday are over.
        var scheduler = RenderScheduler(OmniCalendarView.Week, timeGrid: true, timeZone: TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(
            [true, true, true, false],
            rows.Select(row => row.QuerySelectorAll("td")[1].ClassList.Contains("omni-scheduler__past")));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
