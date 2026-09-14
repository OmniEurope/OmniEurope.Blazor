namespace OmniEurope.Blazor.Components;

public partial class OmniTimeline
{
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TimelineLabel")
        : Label;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the entries sit relative to the line: after it (default), before it, or alternating.</summary>
    [Parameter] public OmniTimelineLayout Layout { get; set; } = OmniTimelineLayout.End;

    /// <summary>
    /// With <see cref="OmniTimelineLayout.Alternate"/>, the side the first entry takes; the next one
    /// takes the other, and so on. Ignored by the other layouts.
    /// </summary>
    [Parameter] public OmniTimelineSide FirstSide { get; set; } = OmniTimelineSide.End;

    // The alternation is drawn by the stylesheet from each entry's position in the list, so an entry
    // added or removed in the middle moves the ones after it to their new side without a render.
    private string LayoutClass => $"omni-timeline--{Layout.ToString().ToLowerInvariant()}";

    private string? FirstSideClass => Layout == OmniTimelineLayout.Alternate
        ? $"omni-timeline--first-{FirstSide.ToString().ToLowerInvariant()}"
        : null;
}
