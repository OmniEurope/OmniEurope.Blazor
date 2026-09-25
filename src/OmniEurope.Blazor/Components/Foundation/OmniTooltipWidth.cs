namespace OmniEurope.Blazor.Components;

/// <summary>
/// The widest an open tooltip may grow before its text wraps. Drawn from a class, never from an
/// inline style, so the strict content policy of the package still holds.
/// </summary>
public enum OmniTooltipWidth
{
    /// <summary>18rem, the default.</summary>
    Standard,

    /// <summary>12rem, for a label or a few words.</summary>
    Narrow,

    /// <summary>28rem, for a description of several sentences.</summary>
    Wide
}
