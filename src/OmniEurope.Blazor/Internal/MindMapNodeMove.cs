namespace OmniEurope.Blazor.Internal;

/// <summary>Where a dragged node was dropped, as reported by <c>omni-mindmap.js</c>.</summary>
internal sealed record MindMapNodeMove(string Id, double X, double Y);
