namespace OmniEurope.Blazor.Components;

/// <summary>
/// Texts of the mind map actions, for a host whose wording differs from the library's. Every text
/// left null falls back to the library resources of the current UI culture.
/// </summary>
/// <remarks>
/// The same texts are used by the context menu, <see cref="OmniMindMapToolbar"/> and the live
/// announcements, so a host renames an action once for every place it appears.
/// </remarks>
public sealed record OmniMindMapLabels
{
    /// <summary>Creates a node.</summary>
    public string? AddNode { get; init; }

    /// <summary>The label a newly created node starts with.</summary>
    public string? NewNodeLabel { get; init; }

    /// <summary>Asks for the selected node to be renamed.</summary>
    public string? Rename { get; init; }

    /// <summary>Copies the selected node beside itself, with the links that lead to it.</summary>
    public string? Duplicate { get; init; }

    /// <summary>Starts drawing a link between two nodes.</summary>
    public string? AddLink { get; init; }

    /// <summary>Shown while a link waits for the node it starts from.</summary>
    public string? LinkSelectSource { get; init; }

    /// <summary>Shown while a link waits for the node it points to.</summary>
    public string? LinkSelectTarget { get; init; }

    /// <summary>Brings the selected node to the middle of the canvas.</summary>
    public string? Center { get; init; }

    /// <summary>Removes the selected nodes or link.</summary>
    public string? Delete { get; init; }

    /// <summary>Lays the whole map out again around its central node.</summary>
    public string? AutoLayout { get; init; }

    /// <summary>Zooms and pans so that every node is visible.</summary>
    public string? FitView { get; init; }
}
