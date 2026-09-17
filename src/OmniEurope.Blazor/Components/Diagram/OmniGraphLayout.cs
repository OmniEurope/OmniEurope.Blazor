using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Automatic layouts of a directed graph, computed in .NET, for <see cref="OmniMindMap"/> or for a
/// host drawing its own graph.
/// </summary>
public static class OmniGraphLayout
{
    /// <summary>
    /// Lays a directed graph out in layers, the Sugiyama way: every edge points forward from one layer
    /// to a later one (a cycle is broken by drawing one of its edges backwards), long edges are routed
    /// through the layers they cross, the order within each layer is chosen to cut crossings, and each
    /// node is centred on its neighbours as far as the spacing allows.
    /// </summary>
    /// <param name="nodes">The nodes and the size of their boxes.</param>
    /// <param name="edges">The directed edges between them.</param>
    /// <param name="options">Direction and spacing; null takes the defaults of <see cref="OmniGraphLayoutOptions"/>.</param>
    /// <returns>The centre of every node and the size of the drawing. The same graph always gets the same drawing.</returns>
    public static OmniGraphLayoutResult Layered(
        IReadOnlyList<OmniGraphLayoutNode> nodes,
        IReadOnlyList<OmniGraphLayoutEdge> edges,
        OmniGraphLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        return GraphLayeredLayout.Build(nodes, edges, options ?? new OmniGraphLayoutOptions());
    }

    /// <summary>
    /// Returns the mind map document with every drawn node placed by <see cref="Layered(IReadOnlyList{OmniGraphLayoutNode}, IReadOnlyList{OmniGraphLayoutEdge}, OmniGraphLayoutOptions?)"/>,
    /// its links read as directed edges. A node with a fixed size keeps it; any other box is sized
    /// from its label as <see cref="OmniMindMap"/> first draws it. Positions are rounded to whole units
    /// and nothing else in the document changes.
    /// </summary>
    public static OmniMindMapDocument Layered(OmniMindMapDocument document, OmniGraphLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = Layered(
            [.. document.Nodes.Select(node => new OmniGraphLayoutNode(node.Id, MindMapGeometry.BoxOf(node).Width, MindMapGeometry.BoxOf(node).Height))],
            [.. document.Edges.Select(edge => new OmniGraphLayoutEdge(edge.From, edge.To))],
            options);
        return document with
        {
            Nodes = [.. document.Nodes.Select(node => result.Positions.TryGetValue(node.Id, out var point)
                ? node with { X = Math.Round(point.X), Y = Math.Round(point.Y) }
                : node)]
        };
    }
}
