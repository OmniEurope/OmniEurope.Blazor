namespace OmniEurope.Blazor.Components;

/// <summary>
/// Which side of its line an <see cref="OmniTimeline"/> sets its entries on. Named after the reading
/// direction rather than left and right, so the meaning holds right to left as well: Start is the
/// side text begins on.
/// </summary>
public enum OmniTimelineLayout
{
    /// <summary>The line runs down the start edge and every entry sits after it.</summary>
    End,

    /// <summary>The line runs down the end edge and every entry sits before it, aligned against it.</summary>
    Start,

    /// <summary>
    /// The line runs down the middle and the entries take turns on either side of it, the first on
    /// <see cref="OmniTimeline.FirstSide"/>. A timeline narrower than 30rem stacks them after the line.
    /// </summary>
    Alternate
}
