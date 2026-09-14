namespace OmniEurope.Blazor.Components;

/// <summary>
/// How a pushing sidebar gets from open to closed and back.
/// </summary>
public enum OmniSidebarTransition
{
    /// <summary>The sidebar appears and disappears at once, the content jumping to its new width.</summary>
    Instant,

    /// <summary>
    /// The sidebar widens and narrows over a short animation, the content sliding with it. Only the
    /// push mode moves this way; a floating sidebar is not affected, and a reader who asked the
    /// system for reduced motion gets the instant change.
    /// </summary>
    Smooth
}
