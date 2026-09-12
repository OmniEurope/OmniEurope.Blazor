namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a horizontal stack does once its content no longer fits.
/// </summary>
public enum OmniStackOverflow
{
    /// <summary>Nothing: the items wrap or spill as the surrounding layout dictates.</summary>
    None,

    /// <summary>
    /// The stack scrolls sideways so every item stays reachable at its full size. No scrollbar shows:
    /// a chevron on each side that still hides items scrolls the row, and goes at the end, as in the
    /// tabs header. The row is then wrapped, and the host's class and attributes go on the wrapper.
    /// </summary>
    Scroll,

    /// <summary>
    /// Labels go and icons stay. Needs the items to carry an icon, or the row goes blank; a button
    /// with no icon keeps its label.
    /// </summary>
    Collapse
}
