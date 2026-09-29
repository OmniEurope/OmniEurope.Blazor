using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The scheduler and the Gantt chart as one family: the month view as a real table, no heading per
/// day, the culture driving dates and texts alike, the state the component keeps apart from its
/// parameters, the remote-data contract, slot durations and the component clock.
/// </summary>
public sealed class SchedulingCoherenceTests : OmniBunitContext
{
    private static readonly DateTimeOffset August = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MonthView_IsATableOfWeeks_EachDayACellUnderItsColumnHeader()
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.View, OmniCalendarView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR")));

        var table = scheduler.Find(".omni-month-view");
        Assert.Equal("table", table.GetAttribute("role"));
        var rows = table.Children;
        Assert.All(rows, row => Assert.Equal("row", row.GetAttribute("role")));
        Assert.Equal(7, rows[0].QuerySelectorAll("[role=columnheader]").Length);
        // August 2026 starts on a Saturday: five leading cells, six weeks.
        Assert.Equal(7, rows.Length);
        Assert.All(rows.Skip(1), row => Assert.Equal(7, row.Children.Count(cell => cell.GetAttribute("role") == "cell")));
        Assert.DoesNotContain(scheduler.FindAll("[role=columnheader]"), header => header.ParentElement?.GetAttribute("role") != "row");
    }

    [Theory]
    [InlineData(OmniCalendarView.Day)]
    [InlineData(OmniCalendarView.Week)]
    [InlineData(OmniCalendarView.Month)]
    public void NoView_WritesAHeadingPerDay(OmniCalendarView view)
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.View, view)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        Assert.Empty(scheduler.FindAll("h1, h2, h3, h4, h5, h6"));
        Assert.NotEmpty(scheduler.FindAll(".omni-scheduler__date"));
    }

    [Fact]
    public void Scheduler_Culture_DrivesTheTextsAsWellAsTheDates()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var scheduler = Render<OmniScheduler>(parameters => parameters
                .Add(component => component.Date, August)
                .Add(component => component.View, OmniCalendarView.Month)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("en-US")));

            Assert.Equal("Month view", scheduler.Find(".omni-month-view").GetAttribute("aria-label"));
            Assert.Equal(["Day", "Week", "Month"], scheduler.FindAll("[role=radio]").Select(radio => radio.TextContent));
            Assert.Equal("Sun", scheduler.Find("[role=columnheader]").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Gantt_Culture_DrivesTheTextsAsWellAsTheDates()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var gantt = Render<OmniGantt>(parameters => parameters
                .Add(component => component.Tasks, [new OmniGanttTask { Id = "a", Title = "Build", Start = new(2026, 3, 2), End = new(2026, 3, 6) }])
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("en-US")));

            Assert.Equal("Gantt chart", gantt.Find("section").GetAttribute("aria-label"));
            Assert.StartsWith("W", gantt.Find(".omni-gantt__unit").TextContent, StringComparison.Ordinal);
            Assert.Contains(gantt.FindAll(".omni-gantt__period"), period => period.TextContent == "March 2026");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Scheduler_KeepsTheReadersPeriodAndView_WithoutWritingItsOwnParameters()
    {
        DateTimeOffset? reportedDate = null;
        OmniCalendarView? reportedView = null;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.DateChanged, value => reportedDate = value)
            .Add(component => component.View, OmniCalendarView.Month)
            .Add(component => component.ViewChanged, value => reportedView = value)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        scheduler.FindAll(".omni-scheduler__header > button")[2].Click();
        scheduler.FindAll("[role=radio]")[1].Click();

        Assert.Equal(August.AddMonths(1), reportedDate);
        Assert.Equal(OmniCalendarView.Week, reportedView);
        Assert.Equal(August.AddMonths(1), scheduler.Instance.CurrentDate);
        Assert.Equal(OmniCalendarView.Week, scheduler.Instance.CurrentView);
        Assert.Equal(August, scheduler.Instance.Date);
        Assert.Equal(OmniCalendarView.Month, scheduler.Instance.View);
        Assert.NotNull(scheduler.Find(".omni-week-view"));
    }

    [Fact]
    public void Scheduler_ANewParameterFromTheHost_ReplacesWhatTheReaderPicked()
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.View, OmniCalendarView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));
        scheduler.FindAll("[role=radio]")[0].Click();
        Assert.Equal(OmniCalendarView.Day, scheduler.Instance.CurrentView);

        scheduler.Render(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        Assert.Equal(OmniCalendarView.Week, scheduler.Instance.CurrentView);
    }

    [Fact]
    public void Scheduler_AFailedLoad_IsReported_AndShownWithTheHostsContent()
    {
        Exception? reported = null;
        var failure = new InvalidOperationException("réseau");
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (_, _, _) => Task.FromException<IReadOnlyList<OmniSchedulerAppointment>>(failure))
            .Add(component => component.OnLoadError, exception => reported = exception)
            .Add(component => component.ErrorContent, (RenderFragment<Exception>)(exception => builder => builder.AddContent(0, $"Erreur : {exception.Message}"))));

        Assert.Same(failure, reported);
        var state = scheduler.Find(".omni-scheduler__state[role=alert]");
        Assert.Contains("Erreur : réseau", state.TextContent, StringComparison.Ordinal);
        Assert.NotNull(state.QuerySelector("button"));
    }

    [Fact]
    public void Scheduler_WhileLoading_ShowsTheHostsLoadingContent()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>();
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (_, _, _) => pending.Task)
            .Add(component => component.LoadingContent, (RenderFragment)(builder => builder.AddContent(0, "Chargement du planning"))));

        Assert.Equal("Chargement du planning", scheduler.Find(".omni-scheduler__state[role=status]").TextContent.Trim());
        pending.SetResult([]);
    }

    [Fact]
    public void Scheduler_SlotDuration_CutsTheTimeGrid()
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, August)
            .Add(component => component.View, OmniCalendarView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(10, 0))
            .Add(component => component.SlotDuration, TimeSpan.FromMinutes(30)));

        var times = scheduler.FindAll("tbody th time").Select(time => time.GetAttribute("datetime"));
        Assert.Equal(["08:00", "08:30", "09:00", "09:30"], times);
    }

    [Fact]
    public void Gantt_MarksTodayFromTheComponentClock()
    {
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero)));
        var tasks = new[] { new OmniGanttTask { Id = "a", Title = "Build", Start = new(2026, 3, 2), End = new(2026, 3, 6) } };

        var gantt = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, tasks)
            .Add(component => component.Scale, OmniCalendarView.Day));

        var layout = gantt.Instance.Layout;
        Assert.Equal(layout.X(new DateOnly(2026, 3, 4)) + (layout.PixelsPerDay / 2), layout.TodayX);
        Assert.Single(gantt.FindAll(".omni-gantt__today"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
