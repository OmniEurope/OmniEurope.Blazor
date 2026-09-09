namespace OmniEurope.Blazor.Components;

/// <summary>
/// What opening a sidebar does to the content beside it.
/// </summary>
public enum OmniSidebarReveal
{
    /// <summary>The sidebar takes its own width in the flow, so the content moves aside for it.</summary>
    Push,

    /// <summary>
    /// The sidebar floats over the content, which keeps its width and its line breaks whether the
    /// sidebar is open or closed.
    /// </summary>
    Overlay
}
