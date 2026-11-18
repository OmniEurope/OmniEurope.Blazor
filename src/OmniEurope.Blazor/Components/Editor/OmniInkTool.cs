namespace OmniEurope.Blazor.Components;

/// <summary>What the pointer does on an <see cref="OmniInkCanvas"/>.</summary>
public enum OmniInkTool
{
    /// <summary>The pointer draws a stroke, in the colour and width of the pen.</summary>
    Pen,

    /// <summary>
    /// The pointer erases: every whole stroke its path touches is removed when the pointer is released. A stroke
    /// is touched when the path comes within its drawn width and a small tolerance of it; a stroke is never cut in two.
    /// </summary>
    Eraser
}
