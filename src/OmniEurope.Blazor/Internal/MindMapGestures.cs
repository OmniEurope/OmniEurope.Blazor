using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="OmniMindMap"/> does with the outcome of a pointer gesture reported by
/// <c>omni-mindmap.js</c> through <see cref="MindMapInteropBridge"/>. Every gesture but a pan ends
/// the view following the canvas size (see <see cref="MindMapViewport.TakeFromCanvas"/>), and a
/// press elsewhere closes the context menu.
/// </summary>
internal sealed class MindMapGestures(
    OmniMindMap owner,
    MindMapModel model,
    MindMapSelection selection,
    MindMapViewport viewport,
    MindMapEditor editor,
    MindMapContextMenu menu)
{
    /// <summary>A node was pressed: it becomes the selection, or the next end of the link being drawn.</summary>
    public async Task NodePressedAsync(string nodeId)
    {
        viewport.TakeFromCanvas();
        menu.Dismiss();
        if (!model.Contains(nodeId))
        {
            return;
        }

        if (selection.IsLinking)
        {
            await editor.PickLinkEndAsync(nodeId);
            return;
        }

        await editor.SelectNodeAsync(nodeId);
    }

    /// <summary>Nodes were dragged and dropped.</summary>
    public Task NodesMovedAsync(IReadOnlyList<MindMapNodeMove> moves) => editor.MoveNodesAsync(moves);

    /// <summary>A link was pressed: it becomes the selection, unless a link is being drawn.</summary>
    public async Task EdgePressedAsync(int index)
    {
        viewport.TakeFromCanvas();
        menu.Dismiss();
        if (selection.IsLinking || !model.RenderedEdges.Any(edge => edge.Index == index))
        {
            return;
        }

        await editor.SelectEdgeAsync(index);
    }

    /// <summary>The canvas background was pressed: the selection is cleared.</summary>
    public async Task BackgroundPressedAsync()
    {
        viewport.TakeFromCanvas();
        menu.Dismiss();
        if (selection.HasAny)
        {
            await editor.ClearSelectionAsync();
        }
    }

    /// <summary>A lasso was drawn over a map rectangle.</summary>
    public Task LassoAsync(double left, double top, double right, double bottom)
    {
        viewport.TakeFromCanvas();
        return editor.SelectInsideAsync(left, top, right, bottom);
    }

    /// <summary>A pan or a zoom stopped; a point that is not a number is ignored.</summary>
    public Task ViewChangedAsync(double panX, double panY, double zoom) =>
        double.IsFinite(panX) && double.IsFinite(panY) && double.IsFinite(zoom)
            ? viewport.SetAsync(panX, panY, zoom)
            : Task.CompletedTask;

    /// <summary>A node was double clicked: it is selected and a rename is asked for.</summary>
    public async Task NodeDoubleClickedAsync(string nodeId)
    {
        viewport.TakeFromCanvas();
        if (!model.Contains(nodeId))
        {
            return;
        }

        await editor.SelectNodeAsync(nodeId);
        await owner.RequestRenameAsync();
    }

    /// <summary>The background was double clicked: a node of the next rotation colour is added there.</summary>
    public Task CanvasDoubleClickedAsync(double mapX, double mapY)
    {
        viewport.TakeFromCanvas();
        return owner.ReadOnly || !double.IsFinite(mapX) || !double.IsFinite(mapY)
            ? Task.CompletedTask
            : editor.AddNodeAtAsync(mapX, mapY, OmniMindMapGroups.Rotation[model.NextNumber % OmniMindMapGroups.Rotation.Count]);
    }

    /// <summary>A context menu was asked for, on a node or on the background.</summary>
    public Task ContextMenuRequestedAsync(string? nodeId, double left, double top, double mapX, double mapY)
    {
        viewport.TakeFromCanvas();
        return menu.OpenAsync(nodeId, left, top, mapX, mapY);
    }

    /// <summary>The context menu was dismissed by a press outside it.</summary>
    public Task MenuDismissedAsync()
    {
        menu.Dismiss();
        return Task.CompletedTask;
    }

    /// <summary>The canvas was resized.</summary>
    public Task ResizedAsync(double width, double height) => viewport.ResizeAsync(width, height);
}
