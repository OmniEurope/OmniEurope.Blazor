using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>
/// The sample project of the Gantt examples. It starts two weeks before the Monday of the current week, so
/// the line of today always falls inside it: started two weeks ago, delivered in four.
/// </summary>
public static class GanttSampleTasks
{
    /// <summary>The tasks of the sample project: eight tasks in two groups, with their progress and dependencies.</summary>
    public static IReadOnlyList<OmniGanttTask> Create() =>
    [
        NewTask("kickoff", "Kick-off", -14, -12, 1, null, [], 0),
        NewTask("interviews", "User interviews", -11, -5, 1, "Design", ["kickoff"], 1),
        NewTask("mockups", "Mock-ups", -4, 4, 0.6, "Design", ["interviews"], 1),
        NewTask("review", "Design review", 7, 9, 0, "Design", ["mockups"], 1),
        NewTask("backend", "Back end", 0, 16, 0.2, "Build", ["interviews"], 2),
        NewTask("frontend", "Front end", 7, 20, 0, "Build", ["mockups"], 2),
        NewTask("testing", "Acceptance testing", 21, 25, 0, "Build", ["backend", "frontend"], 3),
        NewTask("launch", "Go live", 28, 28, 0, null, ["testing", "review"], 4)
    ];

    private static DateOnly Monday
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        }
    }

    private static OmniGanttTask NewTask(string id, string title, int start, int end, double progress, string? group, string[] dependsOn, int color) => new()
    {
        Id = id,
        Title = title,
        Start = Monday.AddDays(start),
        End = Monday.AddDays(end),
        Progress = progress,
        Group = group,
        DependsOn = dependsOn,
        ColorIndex = color
    };
}
