using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the reader has selected on an <see cref="OmniMindMap"/>: one node, several nodes (a lasso),
/// or one link, plus the link being drawn, if any. Plain state: the map decides when it changes.
/// </summary>
internal sealed class MindMapSelection
{
    private readonly HashSet<string> _many = new(StringComparer.Ordinal);

    /// <summary>The single selected node, or null.</summary>
    public string? NodeId { get; set; }

    /// <summary>The nodes a lasso selected, empty when none.</summary>
    public IReadOnlyCollection<string> Many => _many;

    /// <summary>The document index of the selected link, -1 when none.</summary>
    public int EdgeIndex { get; set; } = -1;

    /// <summary>Whether a link is being drawn.</summary>
    public bool IsLinking { get; set; }

    /// <summary>The node the link being drawn starts from, null until it is picked.</summary>
    public string? LinkSource { get; set; }

    /// <summary>Whether anything is selected.</summary>
    public bool HasAny => NodeId is not null || _many.Count > 0 || EdgeIndex >= 0;

    /// <summary>Whether a node is selected, alone or among several.</summary>
    public bool IsSelected(string nodeId) =>
        string.Equals(NodeId, nodeId, StringComparison.Ordinal) || _many.Contains(nodeId);

    /// <summary>Whether a node is among the nodes a lasso selected.</summary>
    public bool InMany(string nodeId) => _many.Contains(nodeId);

    /// <summary>Drops the nodes a lasso selected.</summary>
    public void ClearMany() => _many.Clear();

    /// <summary>Selects these nodes, and only them, as a lasso does.</summary>
    public void SetMany(IEnumerable<string> nodeIds)
    {
        _many.Clear();
        _many.UnionWith(nodeIds);
    }

    /// <summary>Selects nothing; returns whether a single node was selected.</summary>
    public bool Clear()
    {
        var hadNode = NodeId is not null;
        NodeId = null;
        _many.Clear();
        EdgeIndex = -1;
        return hadNode;
    }

    /// <summary>The selected nodes an action applies to: the lasso's, else the single one, else null.</summary>
    public HashSet<string>? Targets() =>
        _many.Count > 0
            ? new HashSet<string>(_many, StringComparer.Ordinal)
            : NodeId is not null ? new HashSet<string>(StringComparer.Ordinal) { NodeId } : null;

    /// <summary>Stops drawing a link.</summary>
    public void EndLink()
    {
        IsLinking = false;
        LinkSource = null;
    }

    /// <summary>
    /// Forgets what a new document no longer draws; returns whether the single selected node was
    /// among it.
    /// </summary>
    public bool Prune(MindMapModel model)
    {
        var lostNode = false;
        if (NodeId is not null && !model.Contains(NodeId))
        {
            NodeId = null;
            lostNode = true;
        }

        _many.RemoveWhere(id => !model.Contains(id));
        if (EdgeIndex >= model.Document.Edges.Count)
        {
            EdgeIndex = -1;
        }

        if (LinkSource is not null && !model.Contains(LinkSource))
        {
            LinkSource = null;
        }

        return lostNode;
    }
}
