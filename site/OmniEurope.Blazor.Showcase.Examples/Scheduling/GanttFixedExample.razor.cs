using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>Two Gantt charts whose zoom is fixed by the host, each with some of its parts turned off.</summary>
public partial class GanttFixedExample
{
    private readonly IReadOnlyList<OmniGanttTask> _tasks = GanttSampleTasks.Create();
}
