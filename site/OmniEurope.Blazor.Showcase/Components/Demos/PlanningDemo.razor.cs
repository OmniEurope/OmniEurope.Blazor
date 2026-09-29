using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class PlanningDemo
{
    // The planning sits around today, the day the Gantt chart marks: the Monday of last week starts it.
    private static readonly DateOnly Monday = MondayBefore(DateOnly.FromDateTime(DateTime.Today)).AddDays(-7);

    // The run started twenty minutes ago, so the step still running reaches the present time.
    private static readonly DateTimeOffset RunStart = DateTimeOffset.UtcNow.AddMinutes(-20).AddSeconds(-15);

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

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<OmniGanttTask> Tasks { get; set; } = [];

    private OmniCalendarView Scale { get; set; } = OmniCalendarView.Week;

    private string? SelectedTaskId { get; set; }

    private string ScaleName => Scale switch
    {
        OmniCalendarView.Day => Text["DemoPlanningScaleDay"],
        OmniCalendarView.Month => Text["DemoPlanningScaleMonth"],
        _ => Text["DemoPlanningScaleWeek"]
    };

    protected override void OnInitialized()
    {
        string design = Text["DemoPlanningGroupDesign"], build = Text["DemoPlanningGroupBuild"];
        Tasks =
        [
            new() { Id = "cadrage", Title = Text["DemoPlanningTaskScoping"], Start = Monday, End = Monday.AddDays(2), Progress = 1 },
            new() { Id = "maquettes", Title = Text["DemoPlanningTaskMockups"], Start = Monday.AddDays(3), End = Monday.AddDays(11), Progress = 0.6, Group = design, DependsOn = ["cadrage"], ColorIndex = 1 },
            new() { Id = "relecture", Title = Text["DemoPlanningTaskProofreading"], Start = Monday.AddDays(14), End = Monday.AddDays(16), Group = design, DependsOn = ["maquettes"], ColorIndex = 1 },
            new() { Id = "dev", Title = Text["DemoPlanningTaskDevelopment"], Start = Monday.AddDays(14), End = Monday.AddDays(32), Progress = 0.1, Group = build, DependsOn = ["maquettes"], ColorIndex = 2 },
            new() { Id = "recette", Title = Text["DemoPlanningTaskAcceptance"], Start = Monday.AddDays(35), End = Monday.AddDays(39), Group = build, DependsOn = ["dev"], ColorIndex = 3 }
        ];
    }

    /// <summary>A day of March 2026 as the culture writes a day and month ("2 mars", "2 March").</summary>
    private static string DayText(int day) => new DateOnly(2026, 3, day).ToString("M", CultureInfo.CurrentCulture);

    private static DateOnly MondayBefore(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
