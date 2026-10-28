using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Scheduling family of the gallery: its pages, in menu order.</summary>
internal static class SchedulingFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyScheduling", OmniIconName.Timer, typeof(SchedulingStrings),
    [
        new("scheduler", "SchedulerTitle", "SchedulerDescription", OmniIconName.Timer,
            ["OmniScheduler"],
            [
                "Scheduling/SchedulerExample.razor", "Scheduling/SchedulerExample.razor.cs"
            ]),
        new("timeline", "TimelineTitle", "TimelineDescription", OmniIconName.Path,
            ["OmniTimeline", "OmniTimelineItem"],
            [
                "Scheduling/TimelineExample.razor", "Scheduling/TimelineExample.razor.cs",
                "Scheduling/TimelineGalleryExample.razor", "Scheduling/TimelineGalleryExample.razor.cs",
                "Scheduling/TimelineDateMode.cs"
            ]),
        new("gantt", "GanttTitle", "GanttDescription", OmniIconName.Rows,
            ["OmniGantt"],
            [
                "Scheduling/GanttExample.razor", "Scheduling/GanttExample.razor.cs",
                "Scheduling/GanttFixedExample.razor", "Scheduling/GanttFixedExample.razor.cs",
                "Scheduling/GanttSampleTasks.cs"
            ]),
        new("step-timeline", "StepTimelineTitle", "StepTimelineDescription", OmniIconName.Path,
            ["OmniStepTimeline"],
            [
                "Scheduling/StepTimelineExample.razor", "Scheduling/StepTimelineExample.razor.cs",
                "Scheduling/StepTimelineOutcome.cs"
            ])
    ]);
}
