namespace OmniEurope.Blazor.Components;

/// <summary>
/// One step of a run drawn by <see cref="OmniStepTimeline"/>.
/// </summary>
/// <param name="Name">What the row shows on its left.</param>
/// <param name="StartedAt">When the step started; null when it never did, which lists it under the
/// bars instead of drawing one.</param>
/// <param name="CompletedAt">When it ended; null while it runs, its bar then reaching
/// <see cref="OmniStepTimeline.Now"/>.</param>
/// <param name="Status">How it ended, which gives the bar its colour.</param>
/// <param name="Secondary">A step the run needs but the reader rarely looks for, such as preparing
/// or cleaning up: its name is set in italics and a muted colour.</param>
public sealed record OmniStepTimelineStep(
    string Name,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    OmniStepTimelineStatus Status = OmniStepTimelineStatus.Success,
    bool Secondary = false)
{
    /// <summary>
    /// How long the step usually takes, over its last runs. A step still running then fills its bar up
    /// to that duration, and a second, stronger layer restarts from the left for the time spent beyond
    /// it, full at twice the usual duration. Null, or a finished step, draws the bar alone.
    /// </summary>
    public TimeSpan? ExpectedDuration { get; init; }
}
