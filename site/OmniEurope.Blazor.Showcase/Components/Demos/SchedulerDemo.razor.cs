using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SchedulerDemo
{
    private static readonly DateTimeOffset Day = new(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<OmniOption<OmniCalendarView>> ViewOptions { get; set; } = [];

    private IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } = [];

    private DateTimeOffset Anchor { get; set; } = Day;

    private OmniCalendarView View { get; set; } = OmniCalendarView.Week;

    private bool TimeGrid { get; set; } = true;

    private string Chosen { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        ViewOptions =
        [
            new(OmniCalendarView.Day, Text["DemoSchedulerViewDay"]),
            new(OmniCalendarView.Week, Text["DemoSchedulerViewWeek"]),
            new(OmniCalendarView.Month, Text["DemoSchedulerViewMonth"])
        ];
        Appointments =
        [
            new("a1", Text["DemoSchedulerCaseReview"], Day.AddHours(9), Day.AddHours(10).AddMinutes(30)),
            new("a2", Text["DemoSchedulerReadingCommittee"], Day.AddHours(14), Day.AddHours(15), Text["DemoSchedulerRoom", 2]) { Class = "showcase-appointment--decision" },
            new("a3", Text["DemoSchedulerWeeklyMeeting"], Day.AddDays(2).AddHours(11), Day.AddDays(2).AddHours(12), Text["DemoSchedulerRoom", 1])
        ];
    }

    private void Open(OmniSchedulerAppointment appointment) =>
        Chosen = $"{appointment.Title}, {appointment.Start:dd/MM HH:mm}–{appointment.End:HH:mm}";

    private void PickSlot(OmniSchedulerSlot slot) =>
        Chosen = Text["DemoSchedulerFreeSlot", $"{slot.Start:dd/MM HH:mm}–{slot.End:dd/MM HH:mm}"];

    private void Move(OmniSchedulerAppointmentMove move) =>
        Appointments = Appointments
            .Select(item => item.Id == move.Appointment.Id ? item with { Start = move.Start, End = move.End } : item)
            .ToArray();
}
