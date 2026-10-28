using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>A pipeline run drawn as a step timeline: its outcome, the minutes elapsed and which steps are listed are parameters.</summary>
public partial class StepTimelineExample
{
    /// <summary>A finished run began before its last step, which ends at 20 min 15.</summary>
    private const double FinishedRunMinutes = 21;

    /// <summary>The outcome of the run: finished, still going or failed.</summary>
    [Parameter]
    public StepTimelineOutcome Outcome { get; set; } = StepTimelineOutcome.Succeeded;

    /// <summary>For a run still going: the minutes since it started, which the steps in progress reach.</summary>
    [Parameter]
    public double ElapsedMinutes { get; set; } = 13;

    /// <summary>Whether the secondary steps (prepare, clean up) are listed; their names are set in italics.</summary>
    [Parameter]
    public bool ShowSecondary { get; set; } = true;

    /// <summary>Whether the steps that never started are listed under the bars.</summary>
    [Parameter]
    public bool ShowNeverStarted { get; set; } = true;

    /// <summary>
    /// The component reads the host clock, so the start of the run is taken from that clock, the elapsed time back:
    /// a step in progress then runs up to now.
    /// </summary>
    private DateTimeOffset RunStart => TimeProvider.System.GetUtcNow()
        .AddMinutes(-(Outcome == StepTimelineOutcome.Running ? ElapsedMinutes : FinishedRunMinutes));

    /// <summary>
    /// A pipeline run: preparation and clean-up as secondary steps, a build and tests that overlap, then the
    /// delivery steps.
    /// </summary>
    private IReadOnlyList<OmniStepTimelineStep> Steps
    {
        get
        {
            var steps = Outcome switch
            {
                StepTimelineOutcome.Running => RunningSteps(),
                StepTimelineOutcome.Failed => FailedSteps(),
                _ => [.. PlannedSteps().Select(planned => planned.Step)]
            };

            return [.. steps
                .Where(step => ShowSecondary || !step.Secondary)
                .Where(step => ShowNeverStarted || step.StartedAt is not null)];
        }
    }

    private static OmniStepTimelineStep Skipped(string name) => new(name, null, null, OmniStepTimelineStatus.Skipped);

    /// <summary>The whole run, each step with its start and end in seconds.</summary>
    private List<(OmniStepTimelineStep Step, int Start, int End)> PlannedSteps() =>
    [
        Planned("Prepare", 0, 35, secondary: true),
        Planned("Restore", 35, 180),
        Planned("Build", 180, 660),
        Planned("Test", 360, 850),
        Planned("Package", 850, 900),
        Planned("Publish", 900, 1170),
        Planned("Cleanup", 1170, 1215, secondary: true),
        (Skipped("Confirm"), 0, 0),
        (Skipped("Record release"), 0, 0),
        (Skipped("Commit"), 0, 0),
        (Skipped("Retain images"), 0, 0)
    ];

    /// <summary>
    /// The run seen at the chosen instant: a step not yet started has no start, a step started but not finished
    /// has no end and runs up to that instant.
    /// </summary>
    private List<OmniStepTimelineStep> RunningSteps()
    {
        var elapsed = (int)(ElapsedMinutes * 60);
        return [.. PlannedSteps()
            .Select(planned => planned.Step.StartedAt is null || planned.Start >= elapsed
                ? planned.Step with { StartedAt = null, CompletedAt = null }
                : planned.End > elapsed
                    ? planned.Step with { CompletedAt = null, Status = OmniStepTimelineStatus.Running }
                    : planned.Step)];
    }

    /// <summary>The tests fail at 12 min 40: the build still going is cancelled and the delivery does not happen.</summary>
    private List<OmniStepTimelineStep> FailedSteps() =>
    [
        Step("Prepare", 0, 35, OmniStepTimelineStatus.Success, secondary: true),
        Step("Restore", 35, 180, OmniStepTimelineStatus.Success),
        Step("Build", 180, 760, OmniStepTimelineStatus.Cancelled),
        Step("Test", 360, 760, OmniStepTimelineStatus.Failed),
        Skipped("Package"),
        Skipped("Publish"),
        Step("Cleanup", 760, 800, OmniStepTimelineStatus.Success, secondary: true),
        Skipped("Confirm"),
        Skipped("Record release")
    ];

    private (OmniStepTimelineStep Step, int Start, int End) Planned(string name, int start, int end, bool secondary = false) =>
        (Step(name, start, end, OmniStepTimelineStatus.Success, secondary), start, end);

    private OmniStepTimelineStep Step(string name, int start, int end, OmniStepTimelineStatus status, bool secondary = false) =>
        new(name, RunStart.AddSeconds(start), RunStart.AddSeconds(end), status, secondary);
}
