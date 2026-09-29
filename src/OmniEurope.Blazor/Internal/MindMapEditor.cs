using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The edits and the selection of an <see cref="OmniMindMap"/>, shared by its keyboard, context
/// menu, toolbar, panel and script: every change of the document goes through
/// <see cref="CommitAsync"/>, which records it in the history and raises
/// <see cref="OmniMindMap.DocumentChanged"/>, and every change of the single selection raises
/// <see cref="OmniMindMap.OnNodeSelect"/>.
/// </summary>
internal sealed class MindMapEditor(
    OmniMindMap owner,
    MindMapModel model,
    MindMapSelection selection,
    MindMapHistory history,
    MindMapLabelSizes sizes,
    MindMapViewport viewport)
{
    /// <summary>The single selected node, or null.</summary>
    public OmniMindMapNode? SelectedNode =>
        selection.NodeId is not null && model.TryGet(selection.NodeId, out var node) ? node : null;

    /// <summary>
    /// Takes a document from the host. The first one is laid out when it carries no positions and
    /// starts the history; a later one keeps the selection whose nodes survive and leaves the history
    /// alone. Returns whether the single selected node was dropped.
    /// </summary>
    public bool Adopt(OmniMindMapDocument document, bool initial)
    {
        Load(document);
        if (!initial)
        {
            // A document replaced from outside (a remote refresh) keeps the reader's selection when
            // its nodes survive and leaves the history alone, as the original editor did.
            return selection.Prune(model);
        }

        if (model.Drawable.Count > 0 && model.Drawable.All(node => node.X == 0 && node.Y == 0))
        {
            // A graph that carries no positions yet gets laid out once, without being reported
            // as a change: it becomes one when the reader first edits the map.
            var positions = AutomaticLayout(document.Edges);
            Load(model.ReplaceNodes(node => positions.TryGetValue(node.Id, out var position)
                ? node with { X = Math.Round(position.X), Y = Math.Round(position.Y) }
                : node));
        }

        history.Reset(model.Document);
        return false;
    }

    /// <summary>Adds a node around the middle of what is visible.</summary>
    public Task AddNodeAsync()
    {
        // Around the middle of what is visible, on a golden-angle spiral so repeated additions do
        // not stack exactly on top of each other.
        var angle = model.NextNumber * 2.399963;
        var (centerX, centerY) = viewport.VisibleCenter;
        return AddNodeAtAsync(centerX + (Math.Cos(angle) * 40), centerY + (Math.Sin(angle) * 40), OmniMindMapGroups.Green);
    }

    /// <summary>Adds a node of a colour group at a map point and selects it.</summary>
    public async Task AddNodeAtAsync(double x, double y, string group)
    {
        if (owner.ReadOnly)
        {
            return;
        }

        if (model.Drawable.Count >= OmniMindMap.MaxNodes)
        {
            owner.Announce(owner.Text("MindMapAnnounceLimit"));
            owner.NotifyStateChanged();
            return;
        }

        var node = new OmniMindMapNode
        {
            Id = model.TakeNextId(),
            Label = owner.NewNodeLabel,
            Group = group,
            X = Math.Round(x),
            Y = Math.Round(y)
        };
        await CommitAsync(model.Document with { Nodes = [.. model.Document.Nodes, node] }, null);
        await SelectNodeAsync(node.Id);
        owner.Announce(owner.Text("MindMapAnnounceAdded", node.Label));
    }

    /// <summary>Removes the selected nodes with their links and notes, or the selected link.</summary>
    public async Task DeleteSelectionAsync()
    {
        if (owner.ReadOnly)
        {
            return;
        }

        var document = model.Document;
        var removed = selection.Targets();
        if (removed is null)
        {
            if (selection.EdgeIndex >= 0 && selection.EdgeIndex < document.Edges.Count)
            {
                var index = selection.EdgeIndex;
                selection.EdgeIndex = -1;
                await CommitAsync(document with { Edges = [.. document.Edges.Where((_, position) => position != index)] }, owner.Text("MindMapAnnounceLinkDeleted"));
            }

            return;
        }

        var next = document with
        {
            Nodes = [.. document.Nodes.Where(node => !removed.Contains(node.Id))],
            Edges = [.. document.Edges.Where(edge => !removed.Contains(edge.From) && !removed.Contains(edge.To))],
            Notes = [.. document.Notes.Where(note => !removed.Contains(note.AttachedTo))]
        };
        var hadNode = selection.Clear();
        await CommitAsync(next, owner.Text("MindMapAnnounceDeleted", removed.Count));
        if (hadNode)
        {
            await owner.OnNodeSelect.InvokeAsync(null);
        }
    }

    /// <summary>Copies the selected node beside it, with the links that point to it.</summary>
    public async Task DuplicateSelectionAsync()
    {
        if (owner.ReadOnly || SelectedNode is not { } source || model.Drawable.Count >= OmniMindMap.MaxNodes)
        {
            return;
        }

        var document = model.Document;
        var copy = source with { Id = model.TakeNextId(), X = source.X + 40, Y = source.Y + 30 };
        var edges = document.Edges.ToList();
        foreach (var incoming in document.Edges.Where(edge => string.Equals(edge.To, source.Id, StringComparison.Ordinal)))
        {
            if (edges.Count < OmniMindMap.MaxEdges)
            {
                edges.Add(incoming with { To = copy.Id });
            }
        }

        await CommitAsync(document with { Nodes = [.. document.Nodes, copy], Edges = edges }, null);
        await SelectNodeAsync(copy.Id);
        owner.Announce(owner.Text("MindMapAnnounceDuplicated", copy.Label));
    }

    /// <summary>Starts drawing a link, from a given node when it is drawn.</summary>
    public Task StartLinkModeAsync(string? source)
    {
        if (owner.ReadOnly)
        {
            return Task.CompletedTask;
        }

        selection.IsLinking = true;
        selection.LinkSource = source is not null && model.Contains(source) ? source : null;
        if (selection.LinkSource is not null)
        {
            selection.NodeId = selection.LinkSource;
            selection.ClearMany();
            selection.EdgeIndex = -1;
        }

        owner.Announce(owner.LinkBannerText);
        owner.NotifyStateChanged();
        return Task.CompletedTask;
    }

    /// <summary>Stops drawing a link, if one is being drawn.</summary>
    public void CancelLinkMode()
    {
        if (!selection.IsLinking)
        {
            return;
        }

        selection.EndLink();
        owner.Announce(owner.Text("MindMapAnnounceLinkCancelled"));
        owner.NotifyStateChanged();
    }

    /// <summary>
    /// Takes a node as the next end of the link being drawn: its source first, then its target,
    /// which draws the link unless the two nodes are already linked or the link limit is reached.
    /// </summary>
    public async Task PickLinkEndAsync(string nodeId)
    {
        if (selection.LinkSource is null)
        {
            selection.LinkSource = nodeId;
            await SelectNodeAsync(nodeId);
            owner.Announce(owner.LinkSelectTargetLabel);
            owner.NotifyStateChanged();
            return;
        }

        if (string.Equals(selection.LinkSource, nodeId, StringComparison.Ordinal))
        {
            return;
        }

        var source = selection.LinkSource;
        selection.EndLink();
        var duplicate = model.Document.Edges.Any(edge =>
            (string.Equals(edge.From, source, StringComparison.Ordinal) && string.Equals(edge.To, nodeId, StringComparison.Ordinal))
            || (string.Equals(edge.From, nodeId, StringComparison.Ordinal) && string.Equals(edge.To, source, StringComparison.Ordinal)));
        await SelectNodeAsync(nodeId);
        if (duplicate || model.Document.Edges.Count >= OmniMindMap.MaxEdges)
        {
            owner.NotifyStateChanged();
            return;
        }

        await CommitAsync(
            model.Document with { Edges = [.. model.Document.Edges, new OmniMindMapEdge { From = source, To = nodeId }] },
            owner.Text("MindMapAnnounceLinked", model.Get(source).Label, model.Get(nodeId).Label));
    }

    /// <summary>Lays every node out again with the automatic layout.</summary>
    public async Task AutoLayoutAsync()
    {
        if (owner.ReadOnly || model.Drawable.Count == 0)
        {
            return;
        }

        var positions = AutomaticLayout(model.Document.Edges);
        await CommitAsync(model.ReplaceNodes(node => positions.TryGetValue(node.Id, out var position)
            ? node with { X = Math.Round(position.X), Y = Math.Round(position.Y) }
            : node), owner.Text("MindMapAnnounceLaidOut"));
    }

    /// <summary>Steps the document back in the history.</summary>
    public async Task UndoAsync()
    {
        if (owner.ReadOnly || !history.CanUndo)
        {
            return;
        }

        await RestoreAsync(history.Undo(), owner.Text("MindMapAnnounceUndone"));
    }

    /// <summary>Steps the document forward in the history.</summary>
    public async Task RedoAsync()
    {
        if (owner.ReadOnly || !history.CanRedo)
        {
            return;
        }

        await RestoreAsync(history.Redo(), owner.Text("MindMapAnnounceRedone"));
    }

    /// <summary>Replaces the node of the same identifier, as the properties panel edits it.</summary>
    public async Task UpdateNodeAsync(OmniMindMapNode updated)
    {
        if (owner.ReadOnly || !model.TryGet(updated.Id, out var current) || current == updated)
        {
            return;
        }

        var replaced = false;
        await CommitAsync(model.ReplaceNodes(node =>
        {
            if (replaced || !string.Equals(node.Id, updated.Id, StringComparison.Ordinal))
            {
                return node;
            }

            replaced = true;
            return updated;
        }), null);
    }

    /// <summary>Gives a drawn node a colour group.</summary>
    public Task SetNodeGroupAsync(string nodeId, string group) =>
        model.TryGet(nodeId, out var node) ? UpdateNodeAsync(node with { Group = group }) : Task.CompletedTask;

    /// <summary>Moves the selected nodes by an offset.</summary>
    public async Task MoveSelectionAsync(double dx, double dy)
    {
        if (owner.ReadOnly || selection.Targets() is not { } moving)
        {
            return;
        }

        await CommitAsync(model.ReplaceNodes(node => moving.Contains(node.Id) ? node with { X = node.X + dx, Y = node.Y + dy } : node), null);
    }

    /// <summary>Puts nodes where a drag dropped them, ignoring unknown nodes and invalid points.</summary>
    public async Task MoveNodesAsync(IReadOnlyList<MindMapNodeMove> moves)
    {
        if (owner.ReadOnly || moves.Count == 0)
        {
            return;
        }

        var targets = moves
            .Where(move => model.Contains(move.Id) && double.IsFinite(move.X) && double.IsFinite(move.Y))
            .GroupBy(move => move.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        if (targets.Count == 0)
        {
            return;
        }

        await CommitAsync(model.ReplaceNodes(node => targets.TryGetValue(node.Id, out var move)
            ? node with { X = Math.Round(move.X), Y = Math.Round(move.Y) }
            : node), null);
    }

    /// <summary>Selects one node, or nothing for null or a node that is not drawn.</summary>
    public async Task SelectNodeAsync(string? nodeId)
    {
        viewport.TakeFromCanvas();
        selection.ClearMany();
        selection.EdgeIndex = -1;
        if (nodeId is not null && !model.Contains(nodeId))
        {
            nodeId = null;
        }

        if (string.Equals(selection.NodeId, nodeId, StringComparison.Ordinal))
        {
            owner.NotifyStateChanged();
            return;
        }

        selection.NodeId = nodeId;
        var node = SelectedNode;
        owner.Announce(node is null ? owner.Text("MindMapAnnounceNoSelection") : DescribeSelection(node));
        owner.NotifyStateChanged();
        await owner.OnNodeSelect.InvokeAsync(node);
    }

    /// <summary>Selects one link, by its index in the document.</summary>
    public async Task SelectEdgeAsync(int index)
    {
        var hadNode = selection.Clear();
        selection.EdgeIndex = index;
        owner.Announce(owner.Text("MindMapAnnounceLinkSelected"));
        owner.NotifyStateChanged();
        if (hadNode)
        {
            await owner.OnNodeSelect.InvokeAsync(null);
        }
    }

    /// <summary>Selects the nodes whose centre lies in a map rectangle, as a lasso does.</summary>
    public async Task SelectInsideAsync(double left, double top, double right, double bottom)
    {
        var inside = model.Drawable
            .Where(node => node.X >= left && node.X <= right && node.Y >= top && node.Y <= bottom)
            .Select(node => node.Id)
            .ToArray();
        if (inside.Length == 1)
        {
            await SelectNodeAsync(inside[0]);
            return;
        }

        var hadNode = selection.Clear();
        selection.SetMany(inside);
        if (inside.Length > 1)
        {
            owner.Announce(owner.Text("MindMapAnnounceManySelected", inside.Length));
        }

        owner.NotifyStateChanged();
        if (hadNode)
        {
            await owner.OnNodeSelect.InvokeAsync(null);
        }
    }

    /// <summary>Selects nothing and announces it.</summary>
    public async Task ClearSelectionAsync()
    {
        viewport.TakeFromCanvas();
        var hadNode = selection.Clear();
        owner.Announce(owner.Text("MindMapAnnounceNoSelection"));
        owner.NotifyStateChanged();
        if (hadNode)
        {
            await owner.OnNodeSelect.InvokeAsync(null);
        }
    }

    private void Load(OmniMindMapDocument document)
    {
        model.Set(document);
        sizes.Pending = true;
    }

    private async Task CommitAsync(OmniMindMapDocument next, string? announcement)
    {
        viewport.TakeFromCanvas();
        Load(next);
        history.Record(next);
        if (announcement is not null)
        {
            owner.Announce(announcement);
        }

        owner.NotifyStateChanged();
        await owner.DocumentChanged.InvokeAsync(next);
    }

    private async Task RestoreAsync(OmniMindMapDocument snapshot, string announcement)
    {
        var hadNode = selection.NodeId is not null;
        Load(snapshot);
        selection.Clear();
        owner.Announce(announcement);
        owner.NotifyStateChanged();
        await owner.DocumentChanged.InvokeAsync(snapshot);
        if (hadNode)
        {
            await owner.OnNodeSelect.InvokeAsync(null);
        }
    }

    private string DescribeSelection(OmniMindMapNode node)
    {
        var links = model.RenderedEdges.Count(edge =>
            string.Equals(edge.From.Id, node.Id, StringComparison.Ordinal) || string.Equals(edge.To.Id, node.Id, StringComparison.Ordinal));
        return owner.Text("MindMapAnnounceSelected", node.Label, links);
    }

    /// <summary>
    /// New centres for every drawn node: the layered layout when <see cref="OmniMindMap.LayeredLayout"/>
    /// is set, centred on the canvas, otherwise the radial one.
    /// </summary>
    private Dictionary<string, (double X, double Y)> AutomaticLayout(IReadOnlyList<OmniMindMapEdge> edges)
    {
        if (owner.LayeredLayout is not { } options)
        {
            return MindMapLayout.Radial(model.Drawable, edges, model.Document.RootId, viewport.CanvasWidth, viewport.CanvasHeight, sizes.SizeOf);
        }

        var result = OmniGraphLayout.Layered(
            [.. model.Drawable.Select(node =>
            {
                var (width, height) = sizes.SizeOf(node);
                return new OmniGraphLayoutNode(node.Id, width, height);
            })],
            [.. edges.Select(edge => new OmniGraphLayoutEdge(edge.From, edge.To))],
            options);
        var offsetX = (viewport.CanvasWidth - result.Width) / 2;
        var offsetY = (viewport.CanvasHeight - result.Height) / 2;
        return result.Positions.ToDictionary(
            entry => entry.Key,
            entry => (entry.Value.X + offsetX, entry.Value.Y + offsetY),
            StringComparer.Ordinal);
    }
}
