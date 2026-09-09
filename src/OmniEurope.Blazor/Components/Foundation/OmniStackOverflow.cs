namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a horizontal stack does once its content no longer fits.
/// </summary>
public enum OmniStackOverflow
{
    /// <summary>Nothing: the items wrap or spill as the surrounding layout dictates.</summary>
    None,

    /// <summary>
    /// The stack scrolls sideways, the way a wide table does, so every item stays reachable at its
    /// full size.
    /// </summary>
    Scroll,

    /// <summary>
    /// Labels go and icons stay. Needs the items to carry an icon, or the row goes blank; a button
    /// with no icon keeps its label.
    /// </summary>
    Collapse
}
