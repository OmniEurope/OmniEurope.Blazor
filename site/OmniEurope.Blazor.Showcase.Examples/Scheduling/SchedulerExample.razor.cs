using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>A scheduler whose date, view, culture, time grid and moves are parameters, with the events it reports.</summary>
public partial class SchedulerExample
{
    /// <summary>The moves the host applied: the scheduler reports them, it keeps nothing.</summary>
    private readonly Dictionary<string, (DateTimeOffset Start, DateTimeOffset End)> _moves = [];

    private IReadOnlyList<OmniSchedulerAppointment> _appointments = [];

    private OmniSchedulerSlot? _selectedSlot;

    private string? _lastEvent;

    /// <summary>The date the scheduler is on.</summary>
    [Parameter]
    public DateTimeOffset Date { get; set; } = Today;

    /// <summary>Raised with the date the reader navigates to.</summary>
    [Parameter]
    public EventCallback<DateTimeOffset> DateChanged { get; set; }

    /// <summary>The view: day, week or month.</summary>
    [Parameter]
    public OmniCalendarView View { get; set; } = OmniCalendarView.Week;

    /// <summary>Raised with the view the reader picks in the scheduler's own switch.</summary>
    [Parameter]
    public EventCallback<OmniCalendarView> ViewChanged { get; set; }

    /// <summary>The culture of the dates, the first day of the week and the texts; null follows the page.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Whether the sample list holds an appointment on Saturday.</summary>
    [Parameter]
    public bool IncludeWeekend { get; set; } = true;

    /// <summary>Whether the day and week views are a grid from 8 am to 6 pm instead of lists.</summary>
    [Parameter]
    public bool TimeGrid { get; set; } = true;

    /// <summary>Whether an appointment can be dragged onto another slot, or moved from the keyboard.</summary>
    [Parameter]
    public bool Movable { get; set; } = true;

    /// <summary>Whether what is already over is hatched.</summary>
    [Parameter]
    public bool MarkPastSlots { get; set; } = true;

    private static DateTimeOffset Today => new(DateTime.Today);

    /// <summary>The Monday of the current week: the sample appointments always fall in the week shown.</summary>
    private static DateTimeOffset WeekStart => Today.AddDays(-(((int)Today.DayOfWeek + 6) % 7));

    private static TimeSpan SlotDuration => TimeSpan.FromMinutes(30);

    private TimeOnly? DayStart => TimeGrid ? new TimeOnly(8, 0) : null;

    private TimeOnly? DayEnd => TimeGrid ? new TimeOnly(18, 0) : null;

    private CultureInfo Formats => Culture ?? CultureInfo.CurrentCulture;

    private EventCallback<OmniSchedulerAppointmentMove> MoveCallback => Movable
        ? EventCallback.Factory.Create<OmniSchedulerAppointmentMove>(this, OnAppointmentMoved)
        : default;

    private string Echo => string.Format(
        Formats,
        "View {0}, date {1}, {2} appointments.",
        View switch
        {
            OmniCalendarView.Day => "day",
            OmniCalendarView.Week => "week",
            _ => "month"
        },
        Date.ToString("D", Formats),
        _appointments.Count);

    protected override void OnParametersSet() => _appointments = BuildAppointments();

    private async Task ChangeDate(DateTimeOffset value)
    {
        Date = value;
        await DateChanged.InvokeAsync(value);
    }

    private async Task ChangeView(OmniCalendarView value)
    {
        View = value;
        await ViewChanged.InvokeAsync(value);
    }

    private void OnSlotClicked(OmniSchedulerSlot slot) => _selectedSlot = slot;

    private void OnAppointmentClicked(OmniSchedulerAppointment appointment) =>
        _lastEvent = string.Format(
            Formats,
            "click on \"{0}\", {1:g} to {2:t}",
            appointment.Title,
            appointment.Start,
            appointment.End);

    private void OnAppointmentMoved(OmniSchedulerAppointmentMove move)
    {
        _moves[move.Appointment.Id] = (move.Start, move.End);
        _lastEvent = string.Format(Formats, "\"{0}\" moved to {1:g}", move.Appointment.Title, move.Start);
        _appointments = BuildAppointments();
    }

    private List<OmniSchedulerAppointment> BuildAppointments()
    {
        var start = WeekStart;
        var appointments = new List<OmniSchedulerAppointment>
        {
            new("monday-review", "File review", start.AddHours(9), start.AddHours(10.5), "Office 3"),
            new("monday-committee", "Reading committee", start.AddHours(14), start.AddHours(15), "Room 2")
            {
                Class = "omni-u-bg-warning-subtle"
            },
            new("wednesday-weekly", "Weekly meeting", start.AddDays(2).AddHours(11), start.AddDays(2).AddHours(12), "Recurring, four weeks"),
            new("thursday-training", "Training", start.AddDays(3).AddHours(13.5), start.AddDays(3).AddHours(17)),
            new("friday-deadline", "Filing deadline", start.AddDays(4).AddHours(16), start.AddDays(4).AddHours(16.5), "Late files are refused"),
            new("today-call", "Morning call", Today.AddHours(8.5), Today.AddHours(9))
        };

        if (IncludeWeekend)
        {
            appointments.Add(new("saturday-move", "Office move", start.AddDays(5).AddHours(10), start.AddDays(5).AddHours(18), "Whole day"));
        }

        return [.. appointments.Select(appointment => _moves.TryGetValue(appointment.Id, out var moved)
            ? appointment with { Start = moved.Start, End = moved.End }
            : appointment)];
    }
}
