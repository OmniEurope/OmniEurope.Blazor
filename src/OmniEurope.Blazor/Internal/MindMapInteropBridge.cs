using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-mindmap.js</c> can call. It carries the outcome of a gesture, never
/// the gesture itself: a drag reports where the nodes were dropped, a pan where it stopped. Kept
/// apart from the component so that the callable surface is internal rather than public API.
/// </summary>
internal sealed class MindMapInteropBridge(OmniMindMap owner)
{
    [JSInvokable]
    public Task NodePressed(string nodeId) => owner.DispatchAsync(() => owner.Gestures.NodePressedAsync(nodeId));

    [JSInvokable]
    public Task NodesMoved(MindMapNodeMove[] moves) => owner.DispatchAsync(() => owner.Gestures.NodesMovedAsync(moves));

    [JSInvokable]
    public Task EdgePressed(int index) => owner.DispatchAsync(() => owner.Gestures.EdgePressedAsync(index));

    [JSInvokable]
    public Task BackgroundPressed() => owner.DispatchAsync(owner.Gestures.BackgroundPressedAsync);

    [JSInvokable]
    public Task LassoSelected(double left, double top, double right, double bottom) =>
        owner.DispatchAsync(() => owner.Gestures.LassoAsync(left, top, right, bottom));

    [JSInvokable]
    public Task ViewChanged(double panX, double panY, double zoom) =>
        owner.DispatchAsync(() => owner.Gestures.ViewChangedAsync(panX, panY, zoom));

    [JSInvokable]
    public Task NodeDoubleClicked(string nodeId) => owner.DispatchAsync(() => owner.Gestures.NodeDoubleClickedAsync(nodeId));

    [JSInvokable]
    public Task CanvasDoubleClicked(double mapX, double mapY) =>
        owner.DispatchAsync(() => owner.Gestures.CanvasDoubleClickedAsync(mapX, mapY));

    [JSInvokable]
    public Task ContextMenuRequested(string? nodeId, double left, double top, double mapX, double mapY) =>
        owner.DispatchAsync(() => owner.Gestures.ContextMenuRequestedAsync(nodeId, left, top, mapX, mapY));

    [JSInvokable]
    public Task MenuDismissed() => owner.DispatchAsync(owner.Gestures.MenuDismissedAsync);

    [JSInvokable]
    public Task Resized(double width, double height) => owner.DispatchAsync(() => owner.Gestures.ResizedAsync(width, height));
}
