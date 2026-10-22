using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The previous and next buttons move by calendar day, week or month of the scheduler's time zone, not by
/// a fixed number of hours: around a clock change a day is 23 or 25 hours long, and a button that adds 24
/// hours skips a day or stays on the same one.
/// </summary>
public sealed class SchedulerDstNavigationTests : OmniBunitContext
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    private IRenderedComponent<OmniScheduler> Render(OmniCalendarView view, DateTimeOffset date) =>
        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.View, view)
            .Add(component => component.Date, date)
            .Add(component => component.TimeZone, Paris)
            .Add(component => component.Culture, System.Globalization.CultureInfo.GetCultureInfo("fr-FR")));

    private static DateTime LocalOf(IRenderedComponent<OmniScheduler> scheduler) =>
        TimeZoneInfo.ConvertTime(scheduler.Instance.CurrentDate, Paris).DateTime;

    private static void Press(IRenderedComponent<OmniScheduler> scheduler, string arrow) =>
        scheduler.FindAll("button.omni-button").Single(button => button.TextContent.Trim() == arrow).Click();

    [Theory]
    // Spring forward on Sunday 29 March 2026 (CET to CEST), late in the evening before.
    [InlineData("2026-03-28T23:30:00+01:00", "›", "2026-03-29")]
    [InlineData("2026-03-30T00:30:00+02:00", "‹", "2026-03-29")]
    // Fall back on Sunday 25 October 2026 (CEST to CET), just after midnight.
    [InlineData("2026-10-25T00:30:00+02:00", "›", "2026-10-26")]
    [InlineData("2026-10-25T23:30:00+01:00", "‹", "2026-10-24")]
    [InlineData("2026-10-26T00:30:00+01:00", "‹", "2026-10-25")]
    public void TheDayView_MovesByOneCalendarDayAcrossAClockChange(string from, string arrow, string expectedDay)
    {
        var scheduler = Render(OmniCalendarView.Day, DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture));

        Press(scheduler, arrow);

        Assert.Equal(DateTime.Parse(expectedDay, System.Globalization.CultureInfo.InvariantCulture).Date, LocalOf(scheduler).Date);
    }

    [Theory]
    [InlineData("2026-03-24T23:30:00+01:00", "›", "2026-03-31")]
    [InlineData("2026-04-01T00:30:00+02:00", "‹", "2026-03-25")]
    [InlineData("2026-10-20T00:30:00+02:00", "›", "2026-10-27")]
    [InlineData("2026-10-27T23:30:00+01:00", "‹", "2026-10-20")]
    public void TheWeekView_MovesBySevenCalendarDaysAcrossAClockChange(string from, string arrow, string expectedDay)
    {
        var scheduler = Render(OmniCalendarView.Week, DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture));

        Press(scheduler, arrow);

        Assert.Equal(DateTime.Parse(expectedDay, System.Globalization.CultureInfo.InvariantCulture).Date, LocalOf(scheduler).Date);
    }

    [Theory]
    [InlineData("2026-03-31T23:30:00+02:00", "›", "2026-04-30")]
    [InlineData("2026-04-01T00:30:00+02:00", "‹", "2026-03-01")]
    [InlineData("2026-09-30T00:30:00+02:00", "›", "2026-10-30")]
    [InlineData("2026-10-31T23:30:00+01:00", "‹", "2026-09-30")]
    public void TheMonthView_MovesByOneCalendarMonth_KeepingTheTimeOfDay(string from, string arrow, string expectedDay)
    {
        var scheduler = Render(OmniCalendarView.Month, DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture));

        Press(scheduler, arrow);

        var local = LocalOf(scheduler);
        Assert.Equal(DateTime.Parse(expectedDay, System.Globalization.CultureInfo.InvariantCulture).Date, local.Date);
        Assert.Equal(DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture).TimeOfDay, local.TimeOfDay);
    }

    [Fact]
    public void ThePeriodShown_FollowsTheNavigation_WhateverTheClockChange()
    {
        var scheduler = Render(OmniCalendarView.Day, new DateTimeOffset(2026, 3, 28, 23, 30, 0, TimeSpan.FromHours(1)));

        Press(scheduler, "›");
        Assert.Contains("29", scheduler.Find("time.omni-scheduler__date, .omni-scheduler__date").TextContent, StringComparison.Ordinal);
        Press(scheduler, "›");
        Assert.Contains("30", scheduler.Find("time.omni-scheduler__date, .omni-scheduler__date").TextContent, StringComparison.Ordinal);
    }
}
