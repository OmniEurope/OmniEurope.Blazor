using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniScheduler.Orientation: Vertical keeps the time grid as it was, Horizontal runs the time along the row,
/// and a move reaches OnAppointmentMove with the same start through the drag path and the button path in both.
/// </summary>
public sealed class SchedulerOrientationTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> RenderWeek(
        OmniOrientation? orientation,
        IReadOnlyList<OmniSchedulerAppointment> items,
        Action<OmniSchedulerAppointmentMove>? onMove = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.View, OmniCalendarView.Week)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"))
                .Add(component => component.DayStart, new TimeOnly(8, 0))
                .Add(component => component.DayEnd, new TimeOnly(12, 0))
                .Add(component => component.Items, items);
            if (orientation is { } value)
            {
                parameters.Add(component => component.Orientation, value);
            }

            if (onMove is not null)
            {
                parameters.Add(component => component.OnAppointmentMove, onMove);
            }
        });

    /// <summary>The cell of a day (0 is Monday) and a slot (0 is 08:00), wherever the orientation puts it.</summary>
    private static IElement Cell(IRenderedComponent<OmniScheduler> scheduler, OmniOrientation orientation, int day, int slot)
    {
        var (row, column) = orientation == OmniOrientation.Horizontal ? (day, slot) : (slot, day);
        return scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[row].QuerySelectorAll("td")[column];
    }

    [Fact]
    public void Vertical_IsTheDefault_AndKeepsTheTimeDownTheGrid()
    {
        var scheduler = RenderWeek(null, [new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))]);

        Assert.Equal(OmniOrientation.Vertical, scheduler.Instance.Orientation);
        Assert.DoesNotContain("omni-scheduler--horizontal", scheduler.Find(".omni-scheduler").ClassList);
        Assert.Empty(scheduler.FindAll(".omni-scheduler-grid__corner"));

        // One header per day across the top, one row per slot, the time as the header of each row.
        var headers = scheduler.FindAll(".omni-scheduler-grid__table thead th");
        Assert.Equal(8, headers.Count);
        Assert.Contains("lun.", headers[1].TextContent);
        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(4, rows.Count);
        Assert.Equal("08:00", rows[0].QuerySelector("th time")!.GetAttribute("datetime"));
        Assert.Equal("11:00", rows[3].QuerySelector("th time")!.GetAttribute("datetime"));

        // The appointment of Monday 09:00 starts in the second row, in the first day column.
        var block = scheduler.Find("[data-omni-scheduler-appointment='a']");
        Assert.Same(Cell(scheduler, OmniOrientation.Vertical, 0, 1), block.ParentElement);
    }

    [Fact]
    public void Horizontal_RunsTheTimeAlongTheRow_AndDrawsEachAppointmentOnItsDayRow()
    {
        var scheduler = RenderWeek(OmniOrientation.Horizontal,
        [
            new("a", "Revue", Monday.AddHours(9), Monday.AddHours(11)),
            new("b", "Comité", Monday.AddDays(2).AddHours(8), Monday.AddDays(2).AddHours(9)),
            new("c", "Appel", Monday.AddDays(2).AddHours(8), Monday.AddDays(2).AddHours(9))
        ]);

        Assert.Contains("omni-scheduler--horizontal", scheduler.Find(".omni-scheduler").ClassList);

        // The slot times head the columns after the corner; one row per day, the day as the header of its row.
        var headers = scheduler.FindAll(".omni-scheduler-grid__table thead th");
        Assert.Equal(5, headers.Count);
        Assert.Single(scheduler.FindAll("thead th.omni-scheduler-grid__corner"));
        Assert.Equal(["08:00", "09:00", "10:00", "11:00"], headers.Skip(1).Select(header => header.QuerySelector("time")!.GetAttribute("datetime")));
        Assert.All(headers, header => Assert.Equal("col", header.GetAttribute("scope")));
        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(7, rows.Count);
        Assert.All(rows, row => Assert.Equal("row", row.QuerySelector("th")!.GetAttribute("scope")));
        Assert.Contains("lun.", rows[0].QuerySelector("th")!.TextContent);
        Assert.All(rows, row => Assert.Equal(4, row.QuerySelectorAll("td").Length));

        // Monday 09:00 to 11:00 starts in the Monday row, in the 09:00 cell, and is two slots long.
        var block = scheduler.Find("[data-omni-scheduler-appointment='a']");
        Assert.Same(Cell(scheduler, OmniOrientation.Horizontal, 0, 1), block.ParentElement);
        Assert.Contains("omni-scheduler__len-2-2", block.ClassList);

        // Two appointments that share their time stand in two lanes, and their row is two lanes high.
        Assert.Contains("omni-scheduler-grid__row--lanes-2", rows[2].ClassList);
        Assert.DoesNotContain(rows[0].ClassList, name => name.StartsWith("omni-scheduler-grid__row--lanes", StringComparison.Ordinal));
        Assert.Same(Cell(scheduler, OmniOrientation.Horizontal, 2, 0), scheduler.Find("[data-omni-scheduler-appointment='b']").ParentElement);
        Assert.Same(Cell(scheduler, OmniOrientation.Horizontal, 2, 0), scheduler.Find("[data-omni-scheduler-appointment='c']").ParentElement);
        Assert.Contains("omni-scheduler__lanes-2", scheduler.Find("[data-omni-scheduler-appointment='b']").ClassList);
    }

    [Theory]
    [InlineData(OmniOrientation.Vertical)]
    [InlineData(OmniOrientation.Horizontal)]
    public void DraggingOntoACell_ReportsTheStartOfThatDayAndSlot(OmniOrientation orientation)
    {
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = RenderWeek(orientation, [new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10).AddMinutes(30))], move => moved = move);

        scheduler.Find("[data-omni-scheduler-appointment='a']").DragStart();
        Cell(scheduler, orientation, 1, 3).DragEnter();
        Assert.Contains("omni-scheduler__drop", Cell(scheduler, orientation, 1, 3).ClassList);
        Cell(scheduler, orientation, 1, 3).Drop();

        // Tuesday 11:00, the duration kept.
        Assert.Equal(Monday.AddDays(1).AddHours(11), moved!.Start);
        Assert.Equal(Monday.AddDays(1).AddHours(12).AddMinutes(30), moved.End);
        Assert.Equal("a", moved.Appointment.Id);
    }

    [Theory]
    [InlineData(OmniOrientation.Vertical)]
    [InlineData(OmniOrientation.Horizontal)]
    public void MovingWithTheButtons_ReportsTheStartOfThatDayAndSlot(OmniOrientation orientation)
    {
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = RenderWeek(orientation, [new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10))], move => moved = move);

        scheduler.Find("[data-omni-scheduler-appointment='a'] .omni-scheduler__move").Click();
        Cell(scheduler, orientation, 4, 0).QuerySelector(".omni-scheduler__place")!.Click();

        // Friday 08:00.
        Assert.Equal(Monday.AddDays(4).AddHours(8), moved!.Start);
        Assert.Equal(Monday.AddDays(4).AddHours(9), moved.End);
    }

    [Theory]
    [InlineData(OmniOrientation.Vertical)]
    [InlineData(OmniOrientation.Horizontal)]
    public void MonthView_IsTheSameInBothOrientations(OmniOrientation orientation)
    {
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniCalendarView.Month)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Orientation, orientation));

        Assert.Empty(scheduler.FindAll(".omni-scheduler-grid"));
        Assert.Single(scheduler.FindAll(".omni-month-view"));
        Assert.Equal(7, scheduler.FindAll(".omni-month-view__weekday").Count);
    }

    [Fact]
    public void Horizontal_NightGridCrossingMidnight_MarksTheColumnOfTheNextDay()
    {
        // 22:00 to 02:00 in one-hour slots: slots 0 and 1 are the evening, slots 2 and 3 the next morning.
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Orientation, OmniOrientation.Horizontal)
            .Add(component => component.DayStart, new TimeOnly(22, 0))
            .Add(component => component.DayEnd, new TimeOnly(2, 0)));

        const string Marker = "omni-scheduler-grid__col--next-day";
        var headers = scheduler.FindAll(".omni-scheduler-grid__table thead th.omni-scheduler-grid__time");
        Assert.Equal(4, headers.Count);
        Assert.Equal("00:00", headers[2].QuerySelector("time")!.GetAttribute("datetime"));
        Assert.Equal([2], headers.Select((header, index) => (header, index)).Where(item => item.header.ClassList.Contains(Marker)).Select(item => item.index));

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(7, rows.Count);
        Assert.All(rows, row =>
        {
            var cells = row.QuerySelectorAll("td");
            Assert.Equal(4, cells.Length);
            Assert.Equal([2], cells.Select((cell, index) => (cell, index)).Where(item => item.cell.ClassList.Contains(Marker)).Select(item => item.index));
        });
    }

    [Fact]
    public void Horizontal_DayStartingAtMidnightOrSameDayGrid_HasNoNextDayMarker()
    {
        var scheduler = RenderWeek(OmniOrientation.Horizontal, []);

        Assert.Empty(scheduler.FindAll(".omni-scheduler-grid__col--next-day"));
    }

    [Fact]
    public void AnAppointmentRemovedFromTheItemsBeforeTheClick_IsNotReported_NotPickedUp_AndNotDragged()
    {
        var items = new List<OmniSchedulerAppointment>
        {
            new("a", "Revue", Monday.AddHours(9), Monday.AddHours(10)),
            new("b", "Comité", Monday.AddHours(10), Monday.AddHours(11)),
            new("c", "Appel", Monday.AddHours(8), Monday.AddHours(9))
        };
        var clicked = new List<string>();
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.DayStart, new TimeOnly(8, 0))
            .Add(component => component.DayEnd, new TimeOnly(12, 0))
            .Add(component => component.Items, items)
            .Add(component => component.OnAppointmentClick, (OmniSchedulerAppointment appointment) => clicked.Add(appointment.Id))
            .Add(component => component.OnAppointmentMove, (OmniSchedulerAppointmentMove move) => moved = move));
        var open = scheduler.Find("[data-omni-scheduler-appointment='a'] .omni-scheduler__open");
        var pickUp = scheduler.Find("[data-omni-scheduler-appointment='b'] .omni-scheduler__move");
        var dragged = scheduler.Find("[data-omni-scheduler-appointment='c']");

        // The list changes under the scheduler without a new render: each handler still holds its old appointment.
        items.RemoveAt(0);
        open.Click();
        items.RemoveAt(0);
        pickUp.Click();
        items.RemoveAt(0);
        dragged.DragStart();
        Cell(scheduler, OmniOrientation.Vertical, 2, 2).Drop();

        Assert.Empty(clicked);
        Assert.Empty(scheduler.FindAll(".omni-scheduler__place"));
        Assert.Null(moved);
    }

    [Fact]
    public void Horizontal_MarksTheRowOfTodayAndOnlyThatRow()
    {
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Monday.AddDays(1).AddHours(10)));

        var scheduler = RenderWeek(OmniOrientation.Horizontal, []);

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal("date", rows[1].QuerySelector("th")!.GetAttribute("aria-current"));
        Assert.Contains("omni-scheduler-grid__day--today", rows[1].QuerySelector("th")!.ClassList);
        Assert.Equal([1], rows.Select((row, index) => (row, index)).Where(item => item.row.QuerySelector("th")!.HasAttribute("aria-current")).Select(item => item.index));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}