using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>Tonight's runs in a grid from 8 pm to 8 am, with the overlaps the host reads and each move it applies.</summary>
public partial class SchedulerNightExample
{
    private static readonly DateTimeOffset Tonight = new(DateTime.Today);

    private IReadOnlyList<OmniSchedulerAppointment> Runs { get; set; } =
    [
        new("nightly-build", "Nightly build", Tonight.AddHours(20.5), Tonight.AddHours(22)),
        new("integration-tests", "Integration tests", Tonight.AddHours(21.5), Tonight.AddHours(23.25)),
        new("container-images", "Container images", Tonight.AddHours(21.75), Tonight.AddHours(22.5)),
        new("backup", "Backup", Tonight.AddHours(23.5), Tonight.AddHours(25.25)),
        new("packaging", "Packaging", Tonight.AddHours(25), Tonight.AddHours(25.75)),
        new("reports", "Reports", Tonight.AddHours(26.5), Tonight.AddHours(26.75)),
        new("cleanup", "Cleanup", Tonight.AddHours(29), Tonight.AddHours(30.5))
    ];

    private IReadOnlyList<OmniSchedulerOverlap> Overlaps => OmniSchedulerOverlap.Find(Runs);

    private void OnRunMoved(OmniSchedulerAppointmentMove move) =>
        Runs = [.. Runs.Select(run => run.Id == move.Appointment.Id ? run with { Start = move.Start, End = move.End } : run)];
}
