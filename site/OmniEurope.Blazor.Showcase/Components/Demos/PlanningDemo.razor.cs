namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class PlanningDemo
{
    // The planning sits around today, the day the Gantt chart marks: the Monday of last week starts it.
    private static readonly DateOnly Monday = MondayBefore(DateOnly.FromDateTime(DateTime.Today)).AddDays(-7);

    // The run started twenty minutes ago, so the step still running reaches the present time.
    private static readonly DateTimeOffset RunStart = DateTimeOffset.UtcNow.AddMinutes(-20).AddSeconds(-15);

    private static readonly IReadOnlyList<OmniGanttTask> Tasks =
    [
        new() { Id = "cadrage", Title = "Cadrage", Start = Monday, End = Monday.AddDays(2), Progress = 1 },
        new() { Id = "maquettes", Title = "Maquettes", Start = Monday.AddDays(3), End = Monday.AddDays(11), Progress = 0.6, Group = "Conception", DependsOn = ["cadrage"], ColorIndex = 1 },
        new() { Id = "relecture", Title = "Relecture", Start = Monday.AddDays(14), End = Monday.AddDays(16), Group = "Conception", DependsOn = ["maquettes"], ColorIndex = 1 },
        new() { Id = "dev", Title = "Développement", Start = Monday.AddDays(14), End = Monday.AddDays(32), Progress = 0.1, Group = "Réalisation", DependsOn = ["maquettes"], ColorIndex = 2 },
        new() { Id = "recette", Title = "Recette", Start = Monday.AddDays(35), End = Monday.AddDays(39), Group = "Réalisation", DependsOn = ["dev"], ColorIndex = 3 }
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

    private OmniCalendarView Scale { get; set; } = OmniCalendarView.Week;

    private string? SelectedTaskId { get; set; }

    private string ScaleName => Scale switch
    {
        OmniCalendarView.Day => "jour",
        OmniCalendarView.Month => "mois",
        _ => "semaine"
    };

    private static DateOnly MondayBefore(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
