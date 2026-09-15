namespace OmniEurope.Blazor.Components;

/// <summary>Which way the layers of <see cref="OmniGraphLayout.Layered(IReadOnlyList{OmniGraphLayoutNode}, IReadOnlyList{OmniGraphLayoutEdge}, OmniGraphLayoutOptions?)"/> follow each other.</summary>
public enum OmniGraphDirection
{
    /// <summary>Layers are columns: an edge goes from left to right.</summary>
    LeftToRight,

    /// <summary>Layers are rows: an edge goes from top to bottom.</summary>
    TopToBottom
}
