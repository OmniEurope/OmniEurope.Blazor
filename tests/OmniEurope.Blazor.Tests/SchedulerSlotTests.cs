using Bunit;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class SchedulerSlotTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> RenderScheduler(
        OmniCalendarView view,
        bool timeGrid,
        Action<ComponentParameterCollectionBuilder<OmniScheduler>>? more = null,
        TimeZoneInfo? timeZone = null,
        DateTimeOffset? date = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, date ?? Monday)
                .Add(component => component.View, view)
                .Add(component => component.TimeZone, timeZone ?? TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"));
            if (timeGrid)
            {
                parameters
                    .Add(component => component.DayStart, new TimeOnly(8, 0))
                    .Add(component => component.DayEnd, new TimeOnly(11, 30));
            }

            more?.Invoke(parameters);
        });

    [Fact]
    public void WithoutOnSlotClick_NoSlotIsAButton()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Week, timeGrid: true);

        Assert.Empty(scheduler.FindAll(".omni-scheduler__slot"));
    }

    [Fact]
    public void TimeGridSlot_IsANamedButton_ReportingItsStartEndAndDuration()
    {
        OmniSchedulerSlot? clicked = null;
        var scheduler = RenderScheduler(OmniCalendarView.Week, timeGrid: true, parameters => parameters
            .Add(component => component.OnSlotClick, value => clicked = value));

        // Rows 08:00, 09:00, 10:00, 11:00; columns Monday to Sunday (fr-FR starts on Monday).
        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(4, rows.Count);
        Assert.Equal(4 * 7, scheduler.FindAll(".omni-scheduler__slot").Count);
        var slot = rows[1].QuerySelectorAll("td")[1].QuerySelector(".omni-scheduler__slot")!;
        Assert.Equal("BUTTON", slot.TagName);
        Assert.Equal("button", slot.GetAttribute("type"));
        Assert.Equal("Nouveau rendez-vous : mardi 9 juin 2026 09:00", slot.GetAttribute("aria-label"));

        slot.Click();

        Assert.NotNull(clicked);
        Assert.Equal(Monday.AddDays(1).AddHours(9), clicked!.Start);
        Assert.Equal(Monday.AddDays(1).AddHours(10), clicked.End);
        Assert.Equal(TimeSpan.FromHours(1), clicked.Duration);
        Assert.DoesNotContain("style=", scheduler.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LastSlot_EndsAtTheEndOfTheTimeGrid()
    {
        OmniSchedulerSlot? clicked = null;
        var scheduler = RenderScheduler(OmniCalendarView.Day, timeGrid: true, parameters => parameters
            .Add(component => component.OnSlotClick, value => clicked = value));

        scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[3].QuerySelector(".omni-scheduler__slot")!.Click();

        Assert.Equal(Monday.AddHours(11), clicked!.Start);
        Assert.Equal(Monday.AddHours(11).AddMinutes(30), clicked.End);
    }

    [Fact]
    public void MonthDay_IsAWholeDaySlot_NamedByItsDate()
    {
        OmniSchedulerSlot? clicked = null;
        var scheduler = RenderScheduler(OmniCalendarView.Month, timeGrid: false, parameters => parameters
            .Add(component => component.OnSlotClick, value => clicked = value));

        // Thirty days, one button each; the days outside the month have none.
        Assert.Equal(30, scheduler.FindAll(".omni-scheduler__slot").Count);
        Assert.Empty(scheduler.FindAll(".omni-month-view__day--outside .omni-scheduler__slot"));
        var slot = scheduler.Find("[data-date='2026-06-10'] > .omni-scheduler__slot");
        Assert.Equal("Nouveau rendez-vous : mercredi 10 juin 2026", slot.GetAttribute("aria-label"));

        slot.Click();

        Assert.Equal(Monday.AddDays(2), clicked!.Start);
        Assert.Equal(Monday.AddDays(3), clicked.End);
        Assert.Equal(TimeSpan.FromDays(1), clicked.Duration);
    }

    [Fact]
    public void WeekListAndDayList_OfferAWholeDaySlot()
    {
        var clicked = new List<OmniSchedulerSlot>();
        var week = RenderScheduler(OmniCalendarView.Week, timeGrid: false, parameters => parameters
            .Add(component => component.OnSlotClick, value => clicked.Add(value)));
        Assert.Equal(7, week.FindAll(".omni-week-view__day > .omni-scheduler__slot").Count);
        week.FindAll(".omni-week-view__day > .omni-scheduler__slot")[6].Click();

        var day = RenderScheduler(OmniCalendarView.Day, timeGrid: false, parameters => parameters
            .Add(component => component.OnSlotClick, value => clicked.Add(value)));
        day.Find(".omni-day-view > .omni-scheduler__slot").Click();

        Assert.Equal([Monday.AddDays(6), Monday], clicked.Select(slot => slot.Start));
        Assert.All(clicked, slot => Assert.Equal(TimeSpan.FromDays(1), slot.Duration));
    }

    [Fact]
    public void Slot_CarriesTheOffsetOfTheSchedulerTimeZone()
    {
        var paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        OmniSchedulerSlot? clicked = null;
        var scheduler = RenderScheduler(
            OmniCalendarView.Day,
            timeGrid: true,
            parameters => parameters.Add(component => component.OnSlotClick, value => clicked = value),
            paris,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.FromHours(2)));

        scheduler.Find(".omni-scheduler__slot").Click();

        Assert.Equal(new DateTimeOffset(2026, 6, 8, 8, 0, 0, TimeSpan.FromHours(2)), clicked!.Start);
        Assert.Equal(TimeSpan.FromHours(2), clicked.Start.Offset);
    }

    [Fact]
    public void SlotButtons_StepAsideWhileAnAppointmentIsCarried_AndAppointmentsKeepTheirClick()
    {
        var given = new OmniSchedulerAppointment("a", "Revue", Monday.AddHours(9), Monday.AddHours(10));
        OmniSchedulerAppointment? opened = null;
        var slots = 0;
        var scheduler = RenderScheduler(OmniCalendarView.Week, timeGrid: true, parameters => parameters
            .Add(component => component.Items, new[] { given })
            .Add(component => component.OnSlotClick, _ => slots++)
            .Add(component => component.OnAppointmentClick, value => opened = value)
            .Add(component => component.OnAppointmentMove, _ => { }));

        // The slot button comes first in its cell, the appointment after it, above it.
        var cell = scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[1].QuerySelectorAll("td")[0];
        Assert.Contains("omni-scheduler__slot", cell.Children[0].ClassList);
        scheduler.Find(".omni-scheduler__open").Click();
        Assert.Same(given, opened);
        Assert.Equal(0, slots);

        scheduler.Find(".omni-scheduler__move").Click();
        Assert.Empty(scheduler.FindAll(".omni-scheduler__slot"));

        scheduler.Find(".omni-scheduler__cancel").Click();
        Assert.Equal(4 * 7, scheduler.FindAll(".omni-scheduler__slot").Count);
    }
}
