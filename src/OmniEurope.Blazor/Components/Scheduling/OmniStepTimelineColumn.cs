namespace OmniEurope.Blazor.Components;

/// <summary>
/// A column of values drawn to the right of the bars of an <see cref="OmniStepTimeline"/>, aligned
/// from row to row: the usual duration of each step, its last duration, what it consumed.
/// </summary>
/// <param name="Title">The column header.</param>
/// <param name="Value">The text of a step's cell; null or empty leaves it blank. A column no step has
/// a value for is not drawn at all.</param>
public sealed record OmniStepTimelineColumn(string Title, Func<OmniStepTimelineStep, string?> Value)
{
    /// <summary>What the column means, shown as the tooltip of its header.</summary>
    public string? Description { get; init; }
}