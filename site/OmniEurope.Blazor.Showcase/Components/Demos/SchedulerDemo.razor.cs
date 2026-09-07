namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SchedulerDemo
{
    private static readonly DateTimeOffset Day = new(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<OmniSchedulerAppointment> Appointments =
    [
        new("a1", "Instruction du dossier", Day.AddHours(9), Day.AddHours(10).AddMinutes(30)),
        new("a2", "Comité de lecture", Day.AddHours(14), Day.AddHours(15), "Salle 2"),
        new("a3", "Point hebdomadaire", Day.AddDays(2).AddHours(11), Day.AddDays(2).AddHours(12),
            "Récurrent", "FREQ=WEEKLY;COUNT=4")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniSchedulerView>> ViewOptions =
    [
        new(OmniSchedulerView.Day, "Jour"),
        new(OmniSchedulerView.Week, "Semaine"),
        new(OmniSchedulerView.Month, "Mois")
    ];

    private DateTimeOffset Anchor { get; set; } = Day;

    private OmniSchedulerView View { get; set; } = OmniSchedulerView.Week;
}
