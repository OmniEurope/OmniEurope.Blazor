using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Scheduling;

/// <summary>The Gantt page: the zoom, picker, groups, dependencies and today line of a Gantt chart, then two charts with a fixed zoom.</summary>
public partial class GanttPage
{
    [Inject]
    private IStringLocalizer<SchedulingStrings> Text { get; set; } = default!;

    private OmniCalendarView Scale { get; set; } = OmniCalendarView.Week;

    private bool ShowScalePicker { get; set; } = true;

    private bool ShowGroups { get; set; } = true;

    private bool ShowDependencies { get; set; } = true;

    private bool ShowToday { get; set; } = true;

    private IReadOnlyList<OmniOption<OmniCalendarView>> ScaleOptions =>
    [
        new(OmniCalendarView.Day, Text["GanttScaleDay"]),
        new(OmniCalendarView.Week, Text["GanttScaleWeek"]),
        new(OmniCalendarView.Month, Text["GanttScaleMonth"])
    ];
}
