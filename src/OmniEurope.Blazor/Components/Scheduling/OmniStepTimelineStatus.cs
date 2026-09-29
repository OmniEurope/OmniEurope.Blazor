namespace OmniEurope.Blazor.Components;

/// <summary>How a step of an <see cref="OmniStepTimeline"/> ended, or that it has not ended yet.</summary>
public enum OmniStepTimelineStatus
{
    /// <summary>Ended well; the default status of a step.</summary>
    Success,

    /// <summary>Still going: without an end, its bar reaches the current time of the component clock.</summary>
    Running,

    /// <summary>Ended in failure.</summary>
    Failed,

    /// <summary>Passed over. A skipped step that never started is listed below the bars rather than drawn.</summary>
    Skipped,

    /// <summary>Stopped before it ended.</summary>
    Cancelled
}
