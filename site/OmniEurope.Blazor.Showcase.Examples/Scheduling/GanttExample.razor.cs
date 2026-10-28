using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>A Gantt chart whose zoom, picker, groups, dependencies and today line are parameters, with the task chosen.</summary>
public partial class GanttExample
{
    private readonly IReadOnlyList<OmniGanttTask> _tasks = GanttSampleTasks.Create();

    private OmniGanttTask? _selected;

    /// <summary>The zoom: one column per day, per week or per month.</summary>
    [Parameter]
    public OmniCalendarView View { get; set; } = OmniCalendarView.Week;

    /// <summary>Raised with the zoom the reader picks in the chart's own picker.</summary>
    [Parameter]
    public EventCallback<OmniCalendarView> ViewChanged { get; set; }

    /// <summary>Whether the day, week and month picker is shown above the chart.</summary>
    [Parameter]
    public bool ShowViewPicker { get; set; } = true;

    /// <summary>Whether the tasks are listed under their group.</summary>
    [Parameter]
    public bool ShowGroups { get; set; } = true;

    /// <summary>Whether an arrow joins a task to the one that waits for it.</summary>
    [Parameter]
    public bool ShowDependencies { get; set; } = true;

    /// <summary>Whether a dashed line runs down the column of today.</summary>
    [Parameter]
    public bool ShowToday { get; set; } = true;

    private string ScaleName => View switch
    {
        OmniCalendarView.Day => "day",
        OmniCalendarView.Month => "month",
        _ => "week"
    };

    private string SelectedText => _selected is { } task
        ? string.Format(
            CultureInfo.CurrentCulture,
            "Selected task: {0}, from {1} to {2}, {3} done.",
            task.Title,
            task.Start.ToString("d MMMM", CultureInfo.CurrentCulture),
            task.End.ToString("d MMMM", CultureInfo.CurrentCulture),
            task.Progress.ToString("P0", CultureInfo.CurrentCulture))
        : "Click a bar, or reach it with Tab and press Enter, to select its task.";

    private async Task ChangeView(OmniCalendarView value)
    {
        View = value;
        await ViewChanged.InvokeAsync(value);
    }

    private void Select(OmniGanttTask task) => _selected = task;
}
