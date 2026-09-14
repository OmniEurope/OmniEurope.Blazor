namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class PlanningDemo
{
    private static readonly DateOnly Today = new(2026, 3, 11);

    private static readonly DateTimeOffset RunStart = new(2026, 3, 11, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset RunNow = RunStart.AddMinutes(20).AddSeconds(15);

    private static readonly IReadOnlyList<OmniGanttTask> Tasks =
    [
        new() { Id = "cadrage", Title = "Cadrage", Start = new(2026, 3, 2), End = new(2026, 3, 4), Progress = 1 },
        new() { Id = "maquettes", Title = "Maquettes", Start = new(2026, 3, 5), End = new(2026, 3, 13), Progress = 0.6, Group = "Conception", DependsOn = ["cadrage"], ColorIndex = 1 },
        new() { Id = "relecture", Title = "Relecture", Start = new(2026, 3, 16), End = new(2026, 3, 18), Group = "Conception", DependsOn = ["maquettes"], ColorIndex = 1 },
        new() { Id = "dev", Title = "Développement", Start = new(2026, 3, 16), End = new(2026, 4, 3), Progress = 0.1, Group = "Réalisation", DependsOn = ["maquettes"], ColorIndex = 2 },
        new() { Id = "recette", Title = "Recette", Start = new(2026, 4, 6), End = new(2026, 4, 10), Group = "Réalisation", DependsOn = ["dev"], ColorIndex = 3 }
    ];

    private static readonly IReadOnlyList<OmniStepTimelineStep> Steps =
    [
        new("Prepare", RunStart, RunStart.AddSeconds(35), OmniStepTimelineStatus.Success, Secondary: true),
        new("Restore", RunStart.AddSeconds(35), RunStart.AddMinutes(3), OmniStepTimelineStatus.Success),
        new("Build", RunStart.AddMinutes(3), RunStart.AddMinutes(11), OmniStepTimelineStatus.Success),
        new("Test", RunStart.AddMinutes(6), RunStart.AddMinutes(14), OmniStepTimelineStatus.Failed),
        new("Package", RunStart.AddMinutes(14), RunStart.AddMinutes(14).AddSeconds(5), OmniStepTimelineStatus.Cancelled),
        new("Publish", RunStart.AddMinutes(15), null, OmniStepTimelineStatus.Running),
        new("Cleanup", RunStart.AddMinutes(19), RunStart.AddMinutes(19).AddSeconds(20), OmniStepTimelineStatus.Skipped, Secondary: true),
        new("Confirm", null, null, OmniStepTimelineStatus.Skipped),
        new("Record release", null, null, OmniStepTimelineStatus.Skipped)
    ];

    private OmniGanttScale Scale { get; set; } = OmniGanttScale.Week;

    private string? SelectedTaskId { get; set; }

    private string ScaleName => Scale switch
    {
        OmniGanttScale.Day => "jour",
        OmniGanttScale.Month => "mois",
        _ => "semaine"
    };
}
