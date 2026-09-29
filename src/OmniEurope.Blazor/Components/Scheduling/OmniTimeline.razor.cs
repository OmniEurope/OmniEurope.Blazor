namespace OmniEurope.Blazor.Components;

/// <summary>
/// A vertical line of dated entries, <see cref="OmniTimelineItem"/> children in the order they are
/// listed, set on one side of the line or alternating around it.
/// </summary>
public partial class OmniTimeline
{
    /// <summary>Accessible name of the timeline; null (the default) takes the localized "Timeline".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The entries, <see cref="OmniTimelineItem"/> components.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Where the entries sit relative to the line: after it (the default), before it, or alternating
    /// from either side.
    /// </summary>
    [Parameter] public OmniTimelineLayout Layout { get; set; } = OmniTimelineLayout.End;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("TimelineLabel") : Label;

    // The alternation is drawn by the stylesheet from each entry's position in the list, so an entry
    // added or removed in the middle moves the ones after it to their new side without a render.
    private string LayoutClass => Layout switch
    {
        OmniTimelineLayout.Start => "omni-timeline--start",
        OmniTimelineLayout.AlternateEnd => "omni-timeline--alternate omni-timeline--first-end",
        OmniTimelineLayout.AlternateStart => "omni-timeline--alternate omni-timeline--first-start",
        _ => "omni-timeline--end"
    };
}
