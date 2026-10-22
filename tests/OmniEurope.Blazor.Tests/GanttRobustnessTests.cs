using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Gantt chart against what a host can hand it: two tasks with the same identifier, dates at the edge of
/// the calendar or a span no chart can draw, and a reader whose day is not the server's.
/// </summary>
public sealed class GanttRobustnessTests : OmniBunitContext
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static string WeekLabel(int week) => "S" + week.ToString(CultureInfo.InvariantCulture);

    private static OmniGanttTask Task(string id, DateOnly start, DateOnly end, params string[] after) =>
        new() { Id = id, Title = "Tâche " + id, Start = start, End = end, DependsOn = after };

    private static GanttLayout Build(OmniCalendarView scale, params OmniGanttTask[] tasks) =>
        GanttLayout.Build(tasks, scale, true, new DateOnly(2026, 3, 11), French, WeekLabel);

    // ---- identifiers ------------------------------------------------------------------------------------

    [Fact]
    public void TwoTasksWithTheSameId_AreRefusedWithAMessageThatNamesIt()
    {
        var error = Assert.Throws<ArgumentException>(() => Build(
            OmniCalendarView.Week,
            Task("a", new(2026, 3, 2), new(2026, 3, 3)),
            Task("b", new(2026, 3, 4), new(2026, 3, 5), "a"),
            Task("a", new(2026, 3, 6), new(2026, 3, 7))));

        Assert.Equal("tasks", error.ParamName);
        Assert.Contains("'a'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheComponent_RefusesDuplicateIdsAtRender()
    {
        var error = Assert.Throws<ArgumentException>(() => Render<OmniGantt>(parameters => parameters.Add(component => component.Tasks,
        [
            Task("a", new(2026, 3, 2), new(2026, 3, 3)),
            Task("a", new(2026, 3, 6), new(2026, 3, 7))
        ])));

        Assert.Contains("'a'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IdsThatDifferOnlyByCase_AreDifferentTasks()
    {
        var layout = Build(OmniCalendarView.Week, Task("a", new(2026, 3, 2), new(2026, 3, 3)), Task("A", new(2026, 3, 4), new(2026, 3, 5), "a"));

        Assert.Equal(2, layout.Rows.Count(row => row.Task is not null));
        Assert.Single(layout.Dependencies);
    }

    // ---- extreme dates --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(OmniCalendarView.Day)]
    [InlineData(OmniCalendarView.Week)]
    [InlineData(OmniCalendarView.Month)]
    public void ASentinelDateFarFromTheOtherTasks_IsRefusedInsteadOfDrawingMillionsOfCells(OmniCalendarView scale)
    {
        var error = Assert.Throws<ArgumentException>(() => Build(
            scale,
            Task("old", DateOnly.MinValue, DateOnly.MinValue),
            Task("now", new(2026, 3, 2), new(2026, 3, 3))));

        Assert.Equal("tasks", error.ParamName);
        Assert.Contains("days", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OmniCalendarView.Day)]
    [InlineData(OmniCalendarView.Week)]
    [InlineData(OmniCalendarView.Month)]
    public void DatesAtTheEdgeOfTheCalendar_DrawWithTheAxisSaturatedAtTheCalendarLimits(OmniCalendarView scale)
    {
        var low = Build(scale, Task("low", DateOnly.MinValue, DateOnly.MinValue.AddDays(1)));
        var high = Build(scale, Task("high", DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue));

        Assert.Equal(DateOnly.MinValue, low.RangeStart);
        Assert.Equal(DateOnly.MaxValue, high.RangeEnd);
        foreach (var layout in new[] { low, high })
        {
            var bar = Assert.Single(layout.Rows).Bar!;
            Assert.True(bar.X >= 0 && bar.Width > 0, $"bar {bar.X} + {bar.Width}");
            Assert.True(bar.X + bar.Width <= layout.Width, $"bar ends at {bar.X + bar.Width}, chart is {layout.Width} wide");
            Assert.All(layout.TopTier.Concat(layout.BottomTier), cell => Assert.True(cell.X >= 0 && cell.X + cell.Width <= layout.Width));
        }
    }

    [Fact]
    public void TheComponent_RendersTasksOnTheFirstAndTheLastDaysOfTheCalendar()
    {
        var chart = Render<OmniGantt>(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.Tasks, [Task("high", DateOnly.MaxValue.AddDays(-3), DateOnly.MaxValue)]));

        Assert.NotEmpty(chart.FindAll("svg"));
    }

    [Fact]
    public void AWideButReasonableSpan_StillDraws()
    {
        var layout = Build(OmniCalendarView.Month, Task("a", new(1990, 1, 1), new(1990, 2, 1)), Task("b", new(2026, 3, 2), new(2026, 3, 3)));

        Assert.True(layout.Width > 0);
        Assert.Equal(2, layout.Rows.Count);
    }

    [Fact]
    public void TheComponent_RefusesAnAbsurdSpanAtRender()
    {
        Assert.Throws<ArgumentException>(() => Render<OmniGantt>(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Day)
            .Add(component => component.Tasks, [Task("old", DateOnly.MinValue, DateOnly.MinValue), Task("now", new(2026, 3, 2), new(2026, 3, 3))])));
    }

    // ---- today in the reader's zone ---------------------------------------------------------------------------

    private void UseNow(DateTimeOffset now) => Services.AddSingleton<TimeProvider>(new FixedClock(now));

    [Fact]
    public void Today_IsTheDateInTheTimeZoneOfTheChart_NotTheServers()
    {
        // 23:30 UTC on 10 March is already 11 March, 00:30, in Paris.
        UseNow(new DateTimeOffset(2026, 3, 10, 23, 30, 0, TimeSpan.Zero));
        var paris = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, [Task("a", new(2026, 3, 2), new(2026, 3, 20))])
            .Add(component => component.TimeZone, TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")));
        var utc = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, [Task("a", new(2026, 3, 2), new(2026, 3, 20))])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        Assert.Equal(paris.Instance.Layout.X(new DateOnly(2026, 3, 11)) + (paris.Instance.Layout.PixelsPerDay / 2), paris.Instance.Layout.TodayX);
        Assert.Equal(utc.Instance.Layout.X(new DateOnly(2026, 3, 10)) + (utc.Instance.Layout.PixelsPerDay / 2), utc.Instance.Layout.TodayX);
    }

    [Fact]
    public void TheTimeZone_CannotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniGantt>(parameters => parameters.Add(component => component.TimeZone, null!)));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void ASpanOfSeveralCenturies_IsRefusedWithAMessageThatNamesTheDates()
    {
        var error = Assert.Throws<ArgumentException>(() => Build(
            OmniCalendarView.Month,
            Task("a", new(2000, 1, 1), new(2000, 1, 5)),
            Task("b", new(2200, 1, 1), new(2200, 1, 5))));

        Assert.Equal("tasks", error.ParamName);
        Assert.Contains("2000-01-01", error.Message, StringComparison.Ordinal);
        Assert.Contains("2200-01-05", error.Message, StringComparison.Ordinal);
    }

    // ---- a clock at the edge of the calendar --------------------------------------------------------------

    [Theory]
    [InlineData(OmniCalendarView.Day, true)]
    [InlineData(OmniCalendarView.Week, true)]
    [InlineData(OmniCalendarView.Month, true)]
    [InlineData(OmniCalendarView.Day, false)]
    [InlineData(OmniCalendarView.Week, false)]
    [InlineData(OmniCalendarView.Month, false)]
    public void AChartWithoutTasksWhoseTodayIsTheFirstOrTheLastDay_StopsItsMarginsAtTheCalendarInsteadOfOverflowing(OmniCalendarView scale, bool last)
    {
        var today = last ? DateOnly.MaxValue : DateOnly.MinValue;

        var layout = GanttLayout.Build([], scale, true, today, French, WeekLabel);

        Assert.True(layout.RangeStart >= DateOnly.MinValue);
        Assert.True(layout.RangeEnd <= DateOnly.MaxValue);
        Assert.True(layout.RangeStart <= layout.RangeEnd);
        Assert.NotEmpty(layout.BottomTier);
    }

    [Fact]
    public void TheComponent_RendersWhenItsClockIsOnTheLastDayOfTheCalendar()
    {
        UseNow(new DateTimeOffset(9999, 12, 30, 12, 0, 0, TimeSpan.Zero));

        var cut = Render<OmniGantt>(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        Assert.True(cut.Instance.Layout.Width > 0);
    }
}
