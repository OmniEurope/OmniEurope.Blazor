namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>How the sample run of the step timeline example stands.</summary>
public enum StepTimelineOutcome
{
    /// <summary>The run finished well: every step has an end.</summary>
    Succeeded,

    /// <summary>The run is still going: the steps in progress reach the instant looked at.</summary>
    Running,

    /// <summary>The tests failed: the build still going is cancelled and the delivery never happens.</summary>
    Failed
}
