using OmniEurope.Blazor.Showcase.Examples.Scheduling;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Scheduling;

/// <summary>The Run steps page: the outcome, elapsed time and listed steps of a pipeline run drawn as a step timeline.</summary>
public partial class StepTimelinePage
{
    [Inject]
    private IStringLocalizer<SchedulingStrings> Text { get; set; } = default!;

    private StepTimelineOutcome Outcome { get; set; } = StepTimelineOutcome.Succeeded;

    private double ElapsedMinutes { get; set; } = 13;

    private bool ShowSecondary { get; set; } = true;

    private bool ShowNeverStarted { get; set; } = true;

    private IReadOnlyList<OmniOption<StepTimelineOutcome>> OutcomeOptions =>
    [
        new(StepTimelineOutcome.Succeeded, Text["StepTimelineOutcomeSucceeded"]),
        new(StepTimelineOutcome.Running, Text["StepTimelineOutcomeRunning"]),
        new(StepTimelineOutcome.Failed, Text["StepTimelineOutcomeFailed"])
    ];
}
