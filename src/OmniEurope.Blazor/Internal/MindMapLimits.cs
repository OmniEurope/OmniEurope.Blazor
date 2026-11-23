namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The bounds and steps of an <see cref="Components.OmniMindMap"/>, kept apart from the component
/// so that its helpers read them without depending on it.
/// </summary>
internal static class MindMapLimits
{
    /// <summary>The most nodes the editor lets a map grow to.</summary>
    public const int MaxNodes = 500;

    /// <summary>The most links the editor lets a map grow to.</summary>
    public const int MaxEdges = 1000;

    /// <summary>The most documents the undo history keeps, the current one included.</summary>
    public const int MaxHistory = 30;

    /// <summary>The smallest zoom of the view.</summary>
    public const double MinZoom = 0.1;

    /// <summary>The largest zoom of the view.</summary>
    public const double MaxZoom = 5;

    /// <summary>The factor of one zoom in or out from the keyboard.</summary>
    public const double ZoomStep = 1.2;

    /// <summary>How far an arrow key moves the selected nodes, in map units.</summary>
    public const double KeyboardMoveStep = 10;

    /// <summary>The width of the context menu, in pixels.</summary>
    public const double MenuWidth = 224;
}
