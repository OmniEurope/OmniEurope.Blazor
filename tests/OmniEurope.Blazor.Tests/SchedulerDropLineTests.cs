using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniScheduler, the drop line of a drag in the time grid: a line at the start of the slot under the pointer, with its time,
/// following the pointer slot by slot, gone on the drop and on a cancelled drag, and absent where a drop would change nothing.
/// The tests drive the drag handlers of the cells; the real pointer, the layout and the hiding on leaving the grid
/// (omni-scheduler.js, tests/js/scheduler-drag.test.mjs) stay the business of the browser.
/// </summary>
public sealed class SchedulerDropLineTests : OmniBunitContext
{
    private const string Line = ".omni-scheduler__drop-line";
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> Render(OmniOrientation orientation, OmniCalendarView view = OmniCalendarView.Week, string culture = "fr-FR") =>
        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, view)
            .Add(component => component.Orientation, orientation)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo(culture))
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(12, 0))
            .Add(component => component.SlotDuration, TimeSpan.FromMinutes(30))
            .Add(component => component.Items, [new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))])
            .Add(component => component.OnAppointmentMove, (OmniSchedulerAppointmentMove _) => { }));

    /// <summary>The cell of a day (0 is Monday) and a slot (0 is 08:00), wherever the orientation puts it.</summary>
    private static IElement Cell(IRenderedComponent<OmniScheduler> scheduler, OmniOrientation orientation, int day, int slot)
    {
        var (row, column) = orientation == OmniOrientation.Horizontal ? (day, slot) : (slot, day);
        return scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[row].QuerySelectorAll("td")[column];
    }

    private static IElement Block(IRenderedComponent<OmniScheduler> scheduler) => scheduler.Find("[data-omni-scheduler-appointment='a']");

    [Theory]
    [InlineData(OmniOrientation.Vertical)]
    [InlineData(OmniOrientation.Horizontal)]
    public void Dragging_DrawsOneLineAtTheTargetedSlot_WithItsTime_AndNoneAfterTheDrop(OmniOrientation orientation)
    {
        var scheduler = Render(orientation);

        Assert.Empty(scheduler.FindAll(Line));
        Block(scheduler).DragStart();
        Assert.Empty(scheduler.FindAll(Line));

        // Tuesday, the fifth slot of the grid: 10:00 at half an hour a slot.
        Cell(scheduler, orientation, 1, 4).DragEnter();
        var line = Assert.Single(scheduler.FindAll(Line));
        Assert.Same(Cell(scheduler, orientation, 1, 4), line.ParentElement);
        Assert.Equal("10:00", line.TextContent);
        Assert.Equal("true", line.GetAttribute("aria-hidden"));
        Assert.Null(line.GetAttribute("style"));

        Cell(scheduler, orientation, 1, 4).Drop();
        Assert.Empty(scheduler.FindAll(Line));
    }

    [Fact]
    public void TheLineFollowsThePointer_SlotBySlot_AcrossDays()
    {
        var scheduler = Render(OmniOrientation.Vertical);
        Block(scheduler).DragStart();

        Cell(scheduler, OmniOrientation.Vertical, 0, 5).DragEnter();
        Assert.Equal("10:30", Assert.Single(scheduler.FindAll(Line)).TextContent);

        Cell(scheduler, OmniOrientation.Vertical, 0, 6).DragEnter();
        var line = Assert.Single(scheduler.FindAll(Line));
        Assert.Equal("11:00", line.TextContent);
        Assert.Same(Cell(scheduler, OmniOrientation.Vertical, 0, 6), line.ParentElement);

        Cell(scheduler, OmniOrientation.Vertical, 3, 0).DragEnter();
        line = Assert.Single(scheduler.FindAll(Line));
        Assert.Equal("08:00", line.TextContent);
        Assert.Same(Cell(scheduler, OmniOrientation.Vertical, 3, 0), line.ParentElement);
    }

    [Fact]
    public void TheTimeIsFormattedWithTheCultureOfTheScheduler_LikeItsOwnTimeLabels()
    {
        var scheduler = Render(OmniOrientation.Vertical, culture: "en-US");
        Block(scheduler).DragStart();

        Cell(scheduler, OmniOrientation.Vertical, 1, 4).DragEnter();

        var line = Assert.Single(scheduler.FindAll(Line));
        Assert.Equal("10:00 AM", line.TextContent);
        Assert.Equal(scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[4].QuerySelector("th time")!.TextContent, line.TextContent);
    }

    [Fact]
    public void ADayViewDrawsTheLineToo()
    {
        var scheduler = Render(OmniOrientation.Vertical, OmniCalendarView.Day);
        Block(scheduler).DragStart();

        scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[6].QuerySelector("td")!.DragEnter();

        Assert.Equal("11:00", Assert.Single(scheduler.FindAll(Line)).TextContent);
    }

    [Theory]
    [InlineData(OmniOrientation.Vertical)]
    [InlineData(OmniOrientation.Horizontal)]
    public void TheSlotWhereTheDropWouldChangeNothing_ShowsNoLine_AndSaysTheAppointmentStays(OmniOrientation orientation)
    {
        var scheduler = Render(orientation);
        Block(scheduler).DragStart();

        // The appointment starts on Monday 09:00, the third slot.
        Cell(scheduler, orientation, 0, 2).DragEnter();

        Assert.Empty(scheduler.FindAll(Line));
        Assert.Equal("Revue reste à sa place.", scheduler.Instance.Announcement);

        // A slot where it can land draws the line again.
        Cell(scheduler, orientation, 0, 3).DragEnter();
        Assert.Equal("09:30", Assert.Single(scheduler.FindAll(Line)).TextContent);
    }

    [Fact]
    public void TheLandingTimeIsAnnouncedInTheLiveRegion_OncePerSlot()
    {
        var scheduler = Render(OmniOrientation.Vertical);
        Block(scheduler).DragStart();

        Cell(scheduler, OmniOrientation.Vertical, 1, 6).DragEnter();

        var announced = scheduler.Instance.Announcement;
        Assert.StartsWith("Relâchez pour déplacer Revue au ", announced, StringComparison.Ordinal);
        Assert.Contains("11:00", announced, StringComparison.Ordinal);
        Assert.Equal(announced, scheduler.Find("[role='status'][aria-live='polite'].omni-visually-hidden").TextContent);

        // Entering the same slot again, from one of its children, says nothing new.
        Cell(scheduler, OmniOrientation.Vertical, 1, 6).DragEnter();
        Assert.Equal(announced, scheduler.Instance.Announcement);
    }

    [Fact]
    public void ACancelledDrag_RemovesTheLine_AndAnnouncesThatTheAppointmentStays()
    {
        var scheduler = Render(OmniOrientation.Vertical);
        Block(scheduler).DragStart();
        Cell(scheduler, OmniOrientation.Vertical, 1, 6).DragEnter();
        Assert.Single(scheduler.FindAll(Line));

        // Escape or a drop outside the grid: the browser ends the drag without a drop.
        Block(scheduler).DragEnd();

        Assert.Empty(scheduler.FindAll(Line));
        Assert.Empty(scheduler.FindAll(".omni-scheduler__drop"));
        Assert.Equal("Revue reste à sa place.", scheduler.Instance.Announcement);
        Assert.DoesNotContain("omni-scheduler--dragging", scheduler.Find(".omni-scheduler").ClassList);
    }

    [Fact]
    public void ADragEndAfterTheDrop_AnnouncesNothingMore()
    {
        var scheduler = Render(OmniOrientation.Vertical);
        Block(scheduler).DragStart();
        Cell(scheduler, OmniOrientation.Vertical, 1, 6).DragEnter();
        Cell(scheduler, OmniOrientation.Vertical, 1, 6).Drop();
        var announced = scheduler.Instance.Announcement;

        Block(scheduler).DragEnd();

        Assert.Equal(announced, scheduler.Instance.Announcement);
        Assert.StartsWith("Revue déplacé", announced, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutAMoveHandler_NoDragStartsAndNoLineAppears()
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(12, 0))
            .Add(component => component.Items, [new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))]));

        Cell(scheduler, OmniOrientation.Vertical, 1, 2).DragEnter();

        Assert.Empty(scheduler.FindAll(Line));
    }
}
