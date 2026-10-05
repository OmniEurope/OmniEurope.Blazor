using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The scheduling components at their edges: the scheduler's day and week steps without a loader, moves
/// started, dropped or put back, loads replaced or failing late, a time zone whose midnight is skipped or
/// repeated, its script on a lost circuit; the Gantt chart's zoom and odd progress; a step timeline
/// without columns.
/// </summary>
public sealed class SchedulingEdgeTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private static OmniSchedulerAppointment Meeting(string id = "1") => new(id, "Réunion " + id, Monday.AddHours(9), Monday.AddHours(10));

    private IRenderedComponent<OmniScheduler> RenderScheduler(Action<ComponentParameterCollectionBuilder<OmniScheduler>>? extra = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Items, [Meeting()]);
            extra?.Invoke(parameters);
        });

    [Theory]
    [InlineData(OmniCalendarView.Day, -1, 9)]
    [InlineData(OmniCalendarView.Week, 1, 17)]
    public void DayAndWeekSteps_WithoutALoader_MoveTheDate(OmniCalendarView view, int direction, int expectedDay)
    {
        var dates = new List<DateTimeOffset>();
        var scheduler = RenderScheduler(parameters => parameters
            .Add(component => component.View, view)
            .Add(component => component.DateChanged, date => dates.Add(date)));

        scheduler.Find($"button[aria-label='{(direction < 0 ? "Période précédente" : "Période suivante")}']").Click();

        Assert.Equal(expectedDay, Assert.Single(dates).Day);
    }

    [Fact]
    public void AppointmentRemovedWhileCarried_EndsTheMove_ButARemoteSchedulerKeepsCarrying()
    {
        var scheduler = RenderScheduler(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.OnAppointmentMove, _ => { }));
        scheduler.Find(".omni-scheduler__move").Click();
        Assert.Contains("omni-scheduler--moving", scheduler.Find("section.omni-scheduler").ClassList);

        // Items handed again with the carried appointment still among them: the move goes on.
        scheduler.Render(parameters => parameters.Add(component => component.Items, [Meeting()]));
        Assert.Contains("omni-scheduler--moving", scheduler.Find("section.omni-scheduler").ClassList);

        scheduler.Render(parameters => parameters.Add(component => component.Items, Array.Empty<OmniSchedulerAppointment>()));
        Assert.DoesNotContain("omni-scheduler--moving", scheduler.Find("section.omni-scheduler").ClassList);

        // A remote scheduler holds only the period it loaded: its items say nothing of the carried one.
        var remote = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.OnAppointmentMove, _ => { })
            .Add(component => component.Load, (_, _, _) => Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([Meeting()])));
        remote.WaitForAssertion(() => Assert.NotEmpty(remote.FindAll(".omni-scheduler__move")));
        remote.Find(".omni-scheduler__move").Click();
        remote.Render(parameters => parameters.Add(component => component.DayStart, new TimeOnly(7, 0)));

        Assert.Contains("omni-scheduler--moving", remote.Find("section.omni-scheduler").ClassList);
    }

    [Fact]
    public void Moves_PickedUpTwice_PutBack_EscapeWithoutMove_AndDragsThatCannotHappen()
    {
        var moves = new List<OmniSchedulerAppointmentMove>();
        var scheduler = RenderScheduler(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(18, 0))
            .Add(component => component.OnAppointmentMove, move => moves.Add(move)));

        scheduler.Find("section.omni-scheduler").KeyDown("Escape");
        scheduler.Find(".omni-scheduler__move").Click();
        Assert.Contains("omni-scheduler--moving", scheduler.Find("section.omni-scheduler").ClassList);
        scheduler.Find(".omni-scheduler__move").Click();
        Assert.DoesNotContain("omni-scheduler--moving", scheduler.Find("section.omni-scheduler").ClassList);

        // A drag over a cell without a drag under way marks no target.
        scheduler.Find("td.omni-scheduler-grid__cell").DragEnter();
        scheduler.Find("td.omni-scheduler-grid__cell").Drop();
        scheduler.Find("section.omni-scheduler").KeyDown("Enter");
        Assert.Empty(moves);
    }

    [Fact]
    public void Scheduler_ThatCannotMove_StartsNoDrag_AndAMoveOfAnAppointmentRemovedEnds()
    {
        var still = RenderScheduler();
        still.Find("[data-omni-scheduler-appointment]").DragStart();
        Assert.DoesNotContain("omni-scheduler--moving", still.Find("section.omni-scheduler").ClassList);

        var movable = RenderScheduler(parameters => parameters.Add(component => component.OnAppointmentMove, _ => { }));
        movable.Find(".omni-scheduler__move").Click();
        movable.Render(parameters => parameters.Add(component => component.Items, [Meeting("2")]));

        Assert.DoesNotContain("omni-scheduler--moving", movable.Find("section.omni-scheduler").ClassList);
    }

    [Fact]
    public async Task LoadsReplacedBeforeTheyEnd_AreCancelledOrForgotten()
    {
        var first = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>();
        var second = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>();
        var calls = 0;
        var errors = new List<Exception>();
        var scheduler = RenderScheduler(parameters => parameters
            .Add(component => component.OnLoadError, error => errors.Add(error))
            .Add(component => component.Load, (_, _, token) => ++calls == 1 ? first.Task : second.Task));

        // The first load, started by the first render, is still running when a second one replaces it.
        var reload = scheduler.InvokeAsync(scheduler.Instance.ReloadAsync);
        first.SetException(new InvalidOperationException("trop tard"));
        second.SetResult([Meeting()]);
        await reload.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        var cancelled = RenderScheduler(parameters => parameters
            .Add(component => component.Load, async (_, _, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return [];
            }));
        // The load of the first render is cancelled by this one, which waits until the scheduler goes.
        _ = cancelled.InvokeAsync(cancelled.Instance.ReloadAsync);

        Assert.Empty(errors);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimeZone_WhoseMidnightIsSkippedOrRepeated_StartsTheDayAtItsFirstMoment(bool skipped)
    {
        var day = new DateTime(2026, 3, 8);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromHours(1),
            skipped
                ? TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 3, 8)
                : TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 1, 2),
            skipped
                ? TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 11, 1)
                : TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 8));
        var zone = TimeZoneInfo.CreateCustomTimeZone("Essai", TimeSpan.Zero, "Essai", "Essai", "Essai été", [rule]);
        var ranges = new List<DateTimeOffset>();

        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, new DateTimeOffset(day.AddHours(12), skipped ? TimeSpan.FromHours(1) : TimeSpan.FromHours(1)))
            .Add(component => component.View, OmniCalendarView.Day)
            .Add(component => component.TimeZone, zone)
            .Add(component => component.Load, (start, _, _) =>
            {
                ranges.Add(start);
                return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
            }));

        var start = Assert.Single(ranges);
        Assert.Equal(day.Date, start.Date);
    }

    [Fact]
    public async Task SchedulerScript_GoneWhileLoading_OrLost_IsQuiet()
    {
        var held = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(held);
        var gone = RenderScheduler(parameters => parameters.Add(component => component.OnAppointmentMove, _ => { }));
        await gone.Instance.DisposeAsync();
        held.PendingImport.SetResult(held.Module);
        await held.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(held.Module.Calls);
    }

    [Fact]
    public async Task SchedulerScript_OnALostCircuit_IsQuiet()
    {
        var lost = new ManualJSRuntime();
        lost.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        lost.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(lost);
        var scheduler = RenderScheduler(parameters => parameters.Add(component => component.OnAppointmentMove, _ => { }));

        await scheduler.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], lost.Module.Calls);
    }

    // ---- Gantt and step timeline ------------------------------------------------------------------

    [Fact]
    public void Gantt_WithItsId_TheSameScaleAgain_AndAProgressThatIsNotANumber()
    {
        var task = new OmniGanttTask { Id = "a", Title = "Analyse", Start = new DateOnly(2026, 9, 1), End = new DateOnly(2026, 9, 5), Progress = double.NaN };
        var scales = new List<OmniCalendarView>();
        var gantt = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Id, "planning")
            .Add(component => component.Scale, OmniCalendarView.Day)
            .Add(component => component.ScaleChanged, scale => scales.Add(scale))
            .Add(component => component.Tasks, [task]));

        gantt.Render(parameters => parameters.Add(component => component.Scale, OmniCalendarView.Day));
        gantt.FindAll(".omni-select-bar__item")[2].Click();

        Assert.Equal([OmniCalendarView.Month], scales);
        Assert.Contains("0", gantt.Instance.Describe(task), StringComparison.Ordinal);
        Assert.Contains("planning", gantt.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void StepTimeline_WithoutColumns_DrawsItsSteps()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Columns, null!)
            .Add(component => component.Steps, [new OmniStepTimelineStep("Étape", new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 1, 8, 5, 0, TimeSpan.Zero))]));

        Assert.Contains("Étape", timeline.Markup, StringComparison.Ordinal);
    }
}
