namespace OmniEurope.Blazor.Components;

/// <summary>How a step of an <see cref="OmniStepTimeline"/> ended, or that it has not ended yet.</summary>
public enum OmniStepTimelineStatus
{
    Success,

    /// <summary>Still going: without an end, its bar reaches the current time of the component clock.</summary>
    Running,

    Failed,

    /// <summary>Passed over. A skipped step that never started is listed below the bars rather than drawn.</summary>
    Skipped,

    Cancelled
}
