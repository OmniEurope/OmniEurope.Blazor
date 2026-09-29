using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The context menu of an <see cref="OmniMindMap"/>: where it is open and on what, where it fits on
/// the canvas, its actions, and the focus moves it asks for (into the menu when it opens, back to the
/// canvas when an action closes it).
/// </summary>
internal sealed class MindMapContextMenu(
    OmniMindMap owner,
    MindMapModel model,
    MindMapSelection selection,
    MindMapViewport viewport,
    MindMapEditor editor)
{
    private const double MenuItemHeight = 44;
    private const double MenuSeparatorHeight = 9;
    private const double MenuColorsHeight = 148;
    private const double MenuPadding = 14;

    /// <summary>The open menu, or null.</summary>
    public MindMapMenuState? State { get; private set; }

    /// <summary>Whether the focus is to move into the menu once it is drawn.</summary>
    public bool FocusMenuPending { get; set; }

    /// <summary>Whether the focus is to go back to the canvas once the menu is gone.</summary>
    public bool FocusCanvasPending { get; set; }

    /// <summary>
    /// Opens the menu on a node, which becomes the selected one unless it is among several selected
    /// nodes, or on the canvas background for a null or unknown node.
    /// </summary>
    public async Task OpenAsync(string? nodeId, double left, double top, double mapX, double mapY)
    {
        if (nodeId is not null && model.Contains(nodeId))
        {
            if (!selection.InMany(nodeId))
            {
                await editor.SelectNodeAsync(nodeId);
            }
            else
            {
                selection.NodeId = nodeId;
                selection.EdgeIndex = -1;
            }

            State = new MindMapMenuState(nodeId, left, top, mapX, mapY);
        }
        else
        {
            State = new MindMapMenuState(null, left, top, mapX, mapY);
        }

        FocusMenuPending = true;
        owner.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the menu from the keyboard: on the selected node, or in the middle of the canvas when no
    /// single node is selected.
    /// </summary>
    public Task OpenFromKeyboardAsync()
    {
        if (editor.SelectedNode is { } node)
        {
            var (left, top) = viewport.ToCanvas(node.X, node.Y);
            return OpenAsync(node.Id, left, top, node.X, node.Y);
        }

        var (mapX, mapY) = viewport.VisibleCenter;
        return OpenAsync(null, viewport.CanvasWidth / 2, viewport.CanvasHeight / 2, mapX, mapY);
    }

    /// <summary>Forgets the menu without moving the focus.</summary>
    public void Dismiss() => State = null;

    /// <summary>Closes the menu on Escape (focus back to the canvas) or Tab (focus moves on).</summary>
    public Task KeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Escape" or "Tab")
        {
            Close(returnFocus: args.Key == "Escape");
        }

        return Task.CompletedTask;
    }

    /// <summary>The rename action.</summary>
    public Task RenameAsync()
    {
        Close(returnFocus: true);
        return owner.RequestRenameAsync();
    }

    /// <summary>The duplicate action.</summary>
    public Task DuplicateAsync()
    {
        Close(returnFocus: true);
        return editor.DuplicateSelectionAsync();
    }

    /// <summary>The add link action, starting from the node the menu was opened on.</summary>
    public Task LinkAsync() => editor.StartLinkModeAsync(Close(returnFocus: true));

    /// <summary>The centre action.</summary>
    public Task CenterAsync()
    {
        Close(returnFocus: true);
        return owner.CenterOnSelectionAsync();
    }

    /// <summary>The delete action.</summary>
    public Task DeleteAsync()
    {
        Close(returnFocus: true);
        return editor.DeleteSelectionAsync();
    }

    /// <summary>A colour swatch: gives the node the menu was opened on this colour group.</summary>
    public Task ColorAsync(string group)
    {
        var nodeId = Close(returnFocus: true);
        return nodeId is null ? Task.CompletedTask : editor.SetNodeGroupAsync(nodeId, group);
    }

    /// <summary>The add node action, at the map point the menu was opened on.</summary>
    public Task AddNodeAsync()
    {
        var menu = State;
        Close(returnFocus: true);
        return menu is null ? Task.CompletedTask : editor.AddNodeAtAsync(menu.MapX, menu.MapY, OmniMindMapGroups.Green);
    }

    /// <summary>The auto layout action.</summary>
    public Task AutoLayoutAsync()
    {
        Close(returnFocus: true);
        return editor.AutoLayoutAsync();
    }

    /// <summary>The fit action.</summary>
    public Task FitViewAsync()
    {
        Close(returnFocus: true);
        return viewport.FitAsync();
    }

    /// <summary>Whether the node the menu was opened on is of this colour group.</summary>
    public bool IsNodeGroup(MindMapMenuState menu, string group) =>
        menu.NodeId is not null
        && model.TryGet(menu.NodeId, out var node)
        && string.Equals(OmniMindMapGroups.Resolve(node.Group), group, StringComparison.Ordinal);

    /// <summary>The height of the menu, from the items it shows.</summary>
    public double Height(MindMapMenuState menu)
    {
        var (items, separators, colors) = (menu.NodeId is null, owner.ReadOnly) switch
        {
            (true, true) => (1, 0, false),
            (true, false) => (3, 1, false),
            (false, true) => (1, 0, false),
            (false, false) => (5, 2, true)
        };
        return (items * MenuItemHeight) + (separators * MenuSeparatorHeight) + (colors ? MenuColorsHeight : 0) + MenuPadding;
    }

    /// <summary>The left edge of the menu, kept inside the canvas.</summary>
    public double Left(MindMapMenuState menu) =>
        Math.Max(0, Math.Min(menu.Left, viewport.CanvasWidth - OmniMindMap.MenuWidth - 8));

    /// <summary>The top edge of the menu, kept inside the canvas.</summary>
    public double Top(MindMapMenuState menu) =>
        Math.Max(0, Math.Min(menu.Top, viewport.CanvasHeight - Height(menu) - 8));

    private string? Close(bool returnFocus)
    {
        var nodeId = State?.NodeId;
        State = null;
        FocusCanvasPending = returnFocus;
        owner.NotifyStateChanged();
        return nodeId;
    }
}
