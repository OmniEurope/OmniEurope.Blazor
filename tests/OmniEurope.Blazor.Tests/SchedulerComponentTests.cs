using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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
    public void TimelineItem_OmitsItsHeader_WhenTheContentAlreadyNamesItself()
    {
        // An entry that carries its own author and date would otherwise show both twice, and an
        // empty header is a heading with nothing under it for a screen reader.
        var timeline = Render<OmniTimeline>(parameters => parameters
            .AddChildContent<OmniTimelineItem>(item => item
                .AddChildContent("<p class=\"probe\">Une entrée qui se nomme elle-même</p>")));

        Assert.Empty(timeline.FindAll("header"));
        Assert.Empty(timeline.FindAll("time"));
        Assert.Single(timeline.FindAll(".omni-timeline__content .probe"));
    }

    [Fact]
    public void TimelineItem_KeepsItsHeader_ForADateAlone()
    {
        var timeline = Render<OmniTimeline>(parameters => parameters
            .AddChildContent<OmniTimelineItem>(item => item
                .Add(component => component.DateText, "hier")
                .AddChildContent("Contenu")));

        Assert.Equal("hier", timeline.Find("time").TextContent);
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
    public void Scheduler_TodayUsesTheRegisteredTimeProvider()
    {
        var expected = new DateTimeOffset(2030, 5, 6, 12, 0, 0, TimeSpan.Zero);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(expected));
        var selected = DateTimeOffset.MinValue;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.DateChanged, value => selected = value));

        scheduler.FindAll(".omni-scheduler__header > button")[1].Click();

        Assert.Equal(expected, selected);
    }

    [Fact]
    public void MonthView_AlignsDaysWithTheCulturesFirstWeekday()
    {
        var month = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(component => component.View, OmniSchedulerView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR")));

        var cells = month.FindAll(".omni-month-view__day");
        Assert.Equal(42, cells.Count);
        Assert.All(cells.Take(5), cell => Assert.Contains("omni-month-view__day--outside", cell.ClassList));
        Assert.Equal("2026-08-01", cells[5].GetAttribute("data-date"));
        Assert.Equal("lun.", month.FindAll(".omni-month-view__weekday")[0].TextContent);
    }

    [Fact]
    public void Scheduler_DateChangedWhileLoading_OrAfterAFailure_LoadsTheNewRange()
    {
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var pending = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>();
        var starts = new List<DateTimeOffset>();
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>> load = (start, _, _) =>
        {
            starts.Add(start);
            return starts.Count switch
            {
                1 => pending.Task,
                2 => Task.FromException<IReadOnlyList<OmniSchedulerAppointment>>(new InvalidOperationException("offline")),
                _ => Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([new("next", "Suivant", start.AddHours(9), start.AddHours(10))])
            };
        };
        void RenderOn(IRenderedComponent<OmniScheduler> scheduler, DateTimeOffset day) => scheduler.Render(parameters => parameters
            .Add(component => component.Date, day)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, load));

        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, load));

        RenderOn(scheduler, date.AddDays(1));
        Assert.Equal([date, date.AddDays(1)], starts);
        Assert.Equal("alert", scheduler.Find(".omni-scheduler__state").GetAttribute("role"));

        RenderOn(scheduler, date.AddDays(2));
        Assert.Equal([date, date.AddDays(1), date.AddDays(2)], starts);
        Assert.Contains("Suivant", scheduler.Markup, StringComparison.Ordinal);
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

    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> RenderWeekGrid(
        IReadOnlyList<OmniSchedulerAppointment> items,
        Action<ComponentParameterCollectionBuilder<OmniScheduler>>? more = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.View, OmniSchedulerView.Week)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"))
                .Add(component => component.DayStart, new TimeOnly(8, 0))
                .Add(component => component.DayEnd, new TimeOnly(12, 0))
                .Add(component => component.Items, items);
            more?.Invoke(parameters);
        });

    [Fact]
    public void TimeGrid_PlacesEachAppointmentInTheSlotWhereItStarts_AndMarksToday()
    {
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Monday.AddDays(1).AddHours(10)));
        var scheduler = RenderWeekGrid(
        [
            new("early", "Avant la grille", Monday.AddHours(6), Monday.AddHours(7)),
            new("mid", "Revue", Monday.AddDays(1).AddHours(9).AddMinutes(30), Monday.AddDays(1).AddHours(10).AddMinutes(30)) { CssClass = "host-billable" }
        ]);

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(4, rows.Count);
        Assert.Equal("08:00", rows[0].QuerySelector("time")!.GetAttribute("datetime"));
        // Columns: the time header, then Monday, Tuesday...; an appointment before the grid sits in the first slot.
        Assert.Contains("Avant la grille", rows[0].QuerySelectorAll("td")[0].TextContent, StringComparison.Ordinal);
        var revue = rows[1].QuerySelectorAll("td")[1].QuerySelector(".omni-scheduler__appointment")!;
        Assert.Contains("host-billable", revue.ClassList);
        Assert.Contains("Revue", revue.TextContent, StringComparison.Ordinal);

        var todayHeader = scheduler.FindAll("thead th")[2];
        Assert.Equal("date", todayHeader.GetAttribute("aria-current"));
        Assert.Contains("omni-scheduler-grid__day--today", todayHeader.ClassList);
        Assert.All(rows, row => Assert.Contains("omni-scheduler-grid__cell--today", row.QuerySelectorAll("td")[1].ClassList));
        Assert.DoesNotContain("style=", scheduler.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadOnlyScheduler_HasNoButtonsOnAppointments_AndLoadsNoScript()
    {
        var scheduler = RenderWeekGrid([new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))]);

        var appointment = scheduler.Find(".omni-scheduler__appointment");
        Assert.Empty(appointment.QuerySelectorAll("button"));
        Assert.Null(appointment.GetAttribute("draggable"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void AppointmentClicked_ReportsTheAppointmentAsTheHostGaveIt()
    {
        var paris = new DateTimeOffset(2026, 6, 8, 11, 0, 0, TimeSpan.FromHours(2));
        var given = new OmniSchedulerAppointment("a", "Revue", paris, paris.AddHours(1));
        OmniSchedulerAppointment? clicked = null;
        var scheduler = RenderWeekGrid([given], parameters => parameters
            .Add(component => component.AppointmentClicked, value => clicked = value));

        scheduler.Find(".omni-scheduler__open").Click();

        Assert.Same(given, clicked);
    }

    [Fact]
    public void MoveButton_PicksUpTheAppointment_AndAPlaceButtonMovesItKeepingItsDuration()
    {
        var given = new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9), Monday.AddHours(10).AddMinutes(30));
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = RenderWeekGrid([given], parameters => parameters
            .Add(component => component.AppointmentMoved, value => moved = value));

        Assert.Empty(scheduler.FindAll(".omni-scheduler__place"));
        scheduler.Find(".omni-scheduler__move").Click();

        Assert.Equal("true", scheduler.Find(".omni-scheduler__move").GetAttribute("aria-pressed"));
        Assert.Contains("Revue", scheduler.Instance.Announcement, StringComparison.Ordinal);
        // Every slot of the week but the one it already starts at offers to take it.
        Assert.Equal(4 * 7 - 1, scheduler.FindAll(".omni-scheduler__place").Count);
        // The visible words start the accessible name, which adds what and where.
        var place = scheduler.FindAll(".omni-scheduler__place")[0];
        Assert.StartsWith(place.TextContent, place.GetAttribute("aria-label"), StringComparison.Ordinal);
        Assert.Contains("Revue", place.GetAttribute("aria-label"), StringComparison.Ordinal);

        // Wednesday, third slot (10:00).
        scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[2].QuerySelectorAll("td")[2]
            .QuerySelector(".omni-scheduler__place")!.Click();

        Assert.NotNull(moved);
        Assert.Same(given, moved!.Appointment);
        Assert.Equal(Monday.AddDays(2).AddHours(10), moved.Start);
        Assert.Equal(Monday.AddDays(2).AddHours(11).AddMinutes(30), moved.End);
        Assert.Empty(scheduler.FindAll(".omni-scheduler__place"));
        // The scheduler leaves its items alone: the host applies the move.
        Assert.Equal(Monday.AddHours(9), given.Start);
    }

    [Fact]
    public void Escape_PutsTheCarriedAppointmentBack()
    {
        var moves = 0;
        var scheduler = RenderWeekGrid([new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))], parameters => parameters
            .Add(component => component.AppointmentMoved, _ => moves++));

        scheduler.Find(".omni-scheduler__move").Click();
        scheduler.Find("section.omni-scheduler").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(scheduler.FindAll(".omni-scheduler__place"));
        Assert.Empty(scheduler.FindAll(".omni-scheduler__cancel"));
        Assert.Equal(0, moves);
    }

    [Fact]
    public void DragOntoAMonthDay_KeepsTheTimeOfDay_AndLoadsTheDragScript()
    {
        var given = new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9).AddMinutes(15), Monday.AddHours(10));
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniSchedulerView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"))
            .Add(component => component.Items, new[] { given })
            .Add(component => component.AppointmentMoved, value => moved = value));

        var appointment = scheduler.Find("[data-omni-scheduler-appointment='a']");
        Assert.Equal("true", appointment.GetAttribute("draggable"));
        appointment.DragStart();
        scheduler.Find("[data-date='2026-06-12']").DragEnter();
        Assert.Contains("omni-scheduler__drop", scheduler.Find("[data-date='2026-06-12']").ClassList);
        scheduler.Find("[data-date='2026-06-12']").Drop();

        Assert.Equal(Monday.AddDays(4).AddHours(9).AddMinutes(15), moved!.Start);
        Assert.Equal(Monday.AddDays(4).AddHours(10), moved.End);
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "import"
            && invocation.Arguments.Contains("./_content/OmniEurope.Blazor/omni-scheduler.js"));
    }

    [Fact]
    public void DropOnTheDayItAlreadyStarts_ReportsNoMove()
    {
        var moves = 0;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniSchedulerView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Items, new[] { new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9), Monday.AddHours(10)) })
            .Add(component => component.AppointmentMoved, _ => moves++));

        scheduler.Find("[data-omni-scheduler-appointment='a']").DragStart();
        scheduler.Find("[data-date='2026-06-08']").Drop();

        Assert.Equal(0, moves);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
