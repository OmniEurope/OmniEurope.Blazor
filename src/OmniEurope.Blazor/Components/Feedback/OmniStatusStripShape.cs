namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniStatusStrip"/> draws each status.</summary>
public enum OmniStatusStripShape
{
    /// <summary>A round point per item, side by side: the last runs of a job.</summary>
    Dot,

    /// <summary>Bars sharing the whole width with no gap: an availability window cut into slices.</summary>
    Segment
}
