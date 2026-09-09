namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an open tooltip does when the pointer keeps moving over its trigger.
/// </summary>
public enum OmniTooltipTracking
{
    /// <summary>It follows the pointer.</summary>
    Pointer,

    /// <summary>
    /// It is placed once, where the pointer entered, and stays there. Steadier to read, and it never
    /// slides under the pointer that is trying to reach what it covers.
    /// </summary>
    Pinned
}
