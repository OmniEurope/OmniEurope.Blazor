using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SchedulerComponentTests : BunitContext
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

    [Fact]
    public async Task Scheduler_IgnoresAnOlderLoadThatCompletesLast()
    {
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var stale = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, OmniSchedulerView.Day)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Load, (_, _, _) => ++callCount switch
            {
                1 => Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]),
                2 => stale.Task,
                _ => latest.Task
            }));

        var staleReload = scheduler.InvokeAsync(() => scheduler.Instance.ReloadAsync());
        Assert.Equal(2, callCount);
        var latestReload = scheduler.InvokeAsync(() => scheduler.Instance.ReloadAsync());
        Assert.Equal(3, callCount);

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
        Assert.Equal(2, scheduler.FindAll(".omni-scheduler__appointments li").Count);
    }
}
