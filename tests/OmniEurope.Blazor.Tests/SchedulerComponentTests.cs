using Bunit;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class SchedulerComponentTests : OmniBunitContext
{
    [Fact]
    public void Timeline_RendersSemanticDatesWithoutInlineStyles()
    {
        var timeline = Render<OmniTimeline>(parameters => parameters
            .Add(component => component.Label, "Historique")
            .AddChildContent<OmniTimelineItem>(item => item
                .Add(component => component.Title, "Création")
                .Add(component => component.Date, new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero))
                .AddChildContent("Dossier créé")));

        Assert.Equal("2026-08-10T09:00:00.0000000+00:00", timeline.Find("time").GetAttribute("datetime"));
        Assert.Equal("Historique", timeline.Find("section").GetAttribute("aria-label"));
        Assert.DoesNotContain("style=", timeline.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scheduler_ChangesPeriodAndViewWithTimezoneAwareAppointments()
    {
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var view = OmniSchedulerView.Month;
        var appointments = new[]
        {
            new OmniSchedulerAppointment("1", "Réunion", date.AddHours(9), date.AddHours(10))
        };
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.DateChanged, value => date = value)
            .Add(component => component.View, view)
            .Add(component => component.ViewChanged, value => view = value)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Items, appointments));

        Assert.Contains("Réunion", scheduler.Markup, StringComparison.Ordinal);
        scheduler.FindAll(".omni-scheduler__header > button")[2].Click();
        Assert.Equal(new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero), date);

        scheduler.FindAll("[role=radio]")[1].Click();
        Assert.Equal(OmniSchedulerView.Week, view);
        Assert.NotNull(scheduler.Find(".omni-week-view"));
    }

    [Fact]
    public void Scheduler_LoadsTheVisibleRangeWithCancellation()
    {
        DateTimeOffset receivedStart = default;
        DateTimeOffset receivedEnd = default;
        CancellationToken receivedToken = default;
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (start, end, token) =>
            {
                receivedStart = start;
                receivedEnd = end;
                receivedToken = token;
                return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
            }));

        Assert.Equal(TimeSpan.FromDays(1), receivedEnd - receivedStart);
        Assert.False(receivedToken.IsCancellationRequested);
        Assert.NotNull(scheduler.Find(".omni-day-view"));
    }

    [Theory]
    [InlineData("fr-FR", "2026-08-10", "2026-08-17")]
    [InlineData("en-US", "2026-08-09", "2026-08-16")]
    public void Scheduler_WeekLoadRangeUsesTheCulturesFirstDay(string cultureName, string expectedStart, string expectedEnd)
    {
        DateTimeOffset receivedStart = default;
        DateTimeOffset receivedEnd = default;

        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(component => component.View, OmniSchedulerView.Week)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo(cultureName))
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (start, end, _) =>
            {
                receivedStart = start;
                receivedEnd = end;
                return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
            }));

        Assert.Equal(expectedStart, receivedStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Assert.Equal(expectedEnd, receivedEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Scheduler_KeysEmptyLoadsByRangeTimezoneViewAndDelegate()
    {
        var firstCalls = 0;
        var secondCalls = 0;
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>> first = (_, _, _) =>
        {
            firstCalls++;
            return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
        };
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>> second = (_, _, _) =>
        {
            secondCalls++;
            return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
        };
        var initial = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, initial)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, first));

        scheduler.Render(parameters => parameters
            .Add(component => component.Date, initial)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, first));
        Assert.Equal(1, firstCalls);

        scheduler.Render(parameters => parameters
            .Add(component => component.Date, initial.AddDays(1))
            .Add(component => component.View, OmniSchedulerView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, second));
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void Scheduler_UsesTheTargetTimezoneForDaylightSavingBoundaries()
    {
        var paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        DateTimeOffset receivedStart = default;
        DateTimeOffset receivedEnd = default;

        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero))
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, paris)
            .Add(component => component.Load, (start, end, _) =>
            {
                receivedStart = start;
                receivedEnd = end;
                return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
            }));

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.FromHours(1)), receivedStart);
        Assert.Equal(new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.FromHours(2)), receivedEnd);
        Assert.Equal(TimeSpan.FromHours(23), receivedEnd - receivedStart);
    }

    [Fact]
    public void Scheduler_TodayUsesTheProvidedTimeProvider()
    {
        var expected = new DateTimeOffset(2030, 5, 6, 12, 0, 0, TimeSpan.Zero);
        var selected = DateTimeOffset.MinValue;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.TimeProvider, new FixedTimeProvider(expected))
            .Add(component => component.DateChanged, value => selected = value));

        scheduler.FindAll(".omni-scheduler__header > button")[1].Click();

        Assert.Equal(expected, selected);
    }

    [Fact]
    public void MonthView_AlignsDaysWithTheCulturesFirstWeekday()
    {
        var month = Render<OmniMonthView>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR")));

        var cells = month.FindAll(".omni-month-view__day");
        Assert.Equal(42, cells.Count);
        Assert.All(cells.Take(5), cell => Assert.Contains("omni-month-view__day--outside", cell.ClassList));
        Assert.Equal("2026-08-01", cells[5].GetAttribute("data-date"));
        Assert.Equal("lun.", month.FindAll(".omni-month-view__weekday")[0].TextContent);
    }

    [Fact]
    public async Task Scheduler_IgnoresAnOlderLoadThatCompletesLast()
    {
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var stale = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        CancellationToken staleToken = default;
        CancellationToken latestToken = default;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (_, _, token) =>
            {
                callCount++;
                if (callCount == 1) return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
                if (callCount == 2) { staleToken = token; return stale.Task; }
                latestToken = token;
                return latest.Task;
            }));

        var staleReload = scheduler.InvokeAsync(() => scheduler.Instance.ReloadAsync());
        Assert.Equal(2, callCount);
        var latestReload = scheduler.InvokeAsync(() => scheduler.Instance.ReloadAsync());
        Assert.Equal(3, callCount);
        Assert.True(staleToken.IsCancellationRequested);
        Assert.False(latestToken.IsCancellationRequested);

        latest.SetResult([new("latest", "Récent", date.AddHours(9), date.AddHours(10))]);
        await latestReload;
        stale.SetResult([new("stale", "Obsolète", date.AddHours(11), date.AddHours(12))]);
        await staleReload;
        scheduler.Render(parameters => { });

        Assert.Contains("Récent", scheduler.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Obsolète", scheduler.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Scheduler_PreservesOverlapsAcrossADaylightSavingBoundary()
    {
        var paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var date = new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero);
        var items = new[]
        {
            new OmniSchedulerAppointment("before", "Avant", date.AddMinutes(30), date.AddHours(2)),
            new OmniSchedulerAppointment("after", "Après", date.AddHours(1.5), date.AddHours(2.5))
        };
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, paris)
            .Add(component => component.Items, items));

        Assert.Contains("Avant", scheduler.Markup, StringComparison.Ordinal);
        Assert.Contains("Après", scheduler.Markup, StringComparison.Ordinal);
        var rendered = scheduler.FindAll(".omni-scheduler__appointments li");
        Assert.Equal(2, rendered.Count);
        Assert.Equal("2026-03-29T01:30:00.0000000+01:00", rendered[0].GetAttribute("data-start"));
        Assert.Equal("2026-03-29T04:00:00.0000000+02:00", rendered[0].GetAttribute("data-end"));
        Assert.Equal("90", rendered[0].GetAttribute("data-duration-minutes"));
        Assert.Equal("2026-03-29T03:30:00.0000000+02:00", rendered[1].GetAttribute("data-start"));
        Assert.True(items[0].Start < items[1].End && items[1].Start < items[0].End);
    }

    [Fact]
    public void Scheduler_PreservesBothOffsetsDuringTheAmbiguousAutumnHour()
    {
        var paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero))
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, paris)
            .Add(component => component.Items, new[]
            {
                new OmniSchedulerAppointment("summer", "Été", new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero)),
                new OmniSchedulerAppointment("winter", "Hiver", new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.Zero))
            }));

        var rendered = scheduler.FindAll(".omni-scheduler__appointments li");
        Assert.Equal("2026-10-25T02:30:00.0000000+02:00", rendered[0].GetAttribute("data-start"));
        Assert.Equal("2026-10-25T02:30:00.0000000+01:00", rendered[1].GetAttribute("data-start"));
        Assert.All(rendered, item => Assert.Equal("30", item.GetAttribute("data-duration-minutes")));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
