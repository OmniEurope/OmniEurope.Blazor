namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SchedulerDemo
{
    private static readonly DateTimeOffset Day = new(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<OmniOption<OmniSchedulerView>> ViewOptions =
    [
        new(OmniSchedulerView.Day, "Jour"),
        new(OmniSchedulerView.Week, "Semaine"),
        new(OmniSchedulerView.Month, "Mois")
    ];

    private IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } =
    [
        new("a1", "Instruction du dossier", Day.AddHours(9), Day.AddHours(10).AddMinutes(30)),
        new("a2", "Comité de lecture", Day.AddHours(14), Day.AddHours(15), "Salle 2") { CssClass = "showcase-appointment--decision" },
        new("a3", "Point hebdomadaire", Day.AddDays(2).AddHours(11), Day.AddDays(2).AddHours(12), "Salle 1")
    ];

    private DateTimeOffset Anchor { get; set; } = Day;

    private OmniSchedulerView View { get; set; } = OmniSchedulerView.Week;

    private string Chosen { get; set; } = string.Empty;

    private void Open(OmniSchedulerAppointment appointment) =>
        Chosen = $"{appointment.Title}, {appointment.Start:dd/MM HH:mm}–{appointment.End:HH:mm}";

    private void Move(OmniSchedulerAppointmentMove move) =>
        Appointments = Appointments
            .Select(item => item.Id == move.Appointment.Id ? item with { Start = move.Start, End = move.End } : item)
            .ToArray();
}
