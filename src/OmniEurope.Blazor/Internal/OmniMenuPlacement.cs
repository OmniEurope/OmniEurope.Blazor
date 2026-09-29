namespace OmniEurope.Blazor.Internal;

/// <summary>Where the menu engine of <c>omni-focus.js</c> places an open menu.</summary>
internal enum OmniMenuPlacement
{
    /// <summary>Under its anchor, their start edges aligned (the end edges in a right-to-left page).</summary>
    Start,

    /// <summary>Under its anchor, their end edges aligned (the start edges in a right-to-left page).</summary>
    End,

    /// <summary>
    /// At the pointer of a right-click, opening towards the other side past an edge; under its anchor,
    /// start edges aligned, when the keyboard opened it. A right-click on the anchor moves it.
    /// </summary>
    Pointer
}
