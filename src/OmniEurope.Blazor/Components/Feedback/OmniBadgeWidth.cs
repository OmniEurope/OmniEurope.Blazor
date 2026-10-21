namespace OmniEurope.Blazor.Components;

/// <summary>
/// A common minimum width for the badges of one column or list, so that status words of different
/// lengths ("Failed", "Cancelled", "Success") line up on the same width. It is a floor, never a cut: a
/// longer text widens its own badge.
/// </summary>
/// <remarks>
/// Each step reads a token (<c>--omni-badge-width-small</c>, <c>-medium</c>, <c>-large</c>) in <c>em</c>
/// of the badge's own text, so the width follows the density and the font. A host that wants other
/// lengths sets the token in its stylesheet, on the grid or the list that holds the badges.
/// </remarks>
public enum OmniBadgeWidth
{
    /// <summary>The badge is as wide as its content. The default.</summary>
    Auto,

    /// <summary>A short floor, for a few letters ("SPF", "DKIM", "DMARC").</summary>
    Small,

    /// <summary>A middle floor, for one common status word ("Failed", "Degraded", "Compliant").</summary>
    Medium,

    /// <summary>A long floor, for the longest words and short phrases of a status column.</summary>
    Large
}
