namespace OmniEurope.Blazor.Components;

/// <summary>
/// The context menu of an <see cref="OmniHtmlEditor"/>: the package's one menu engine
/// (<see cref="OmniMenuController"/>, <c>omni-focus.js</c>) opened at the pointer over the visual surface
/// and drawn in place around its <see cref="OmniMenuItem"/> rows, which close it when chosen.
/// </summary>
internal sealed class HtmlEditorContextMenu(OmniHtmlEditor owner, IJSRuntime javaScript, Func<string> label) : IOmniMenu, IAsyncDisposable
{
    private OmniMenuController? _controller;
    private RenderFragment<RenderFragment>? _list;

    /// <summary>The identifier of the <c>role="menu"</c> element.</summary>
    internal string MenuId => owner.SurfaceId + "-menu";

    private OmniMenuController Controller => _controller ??= new OmniMenuController(
        javaScript,
        restoreFocus => owner.DispatchAsync(() => CloseAsync(restoreFocus)));

    /// <summary>Whether the menu is open over the visual face.</summary>
    internal bool IsOpen => owner.CurrentMode == OmniHtmlEditorMode.Visual && _controller is not null && _controller.IsOpen(null);

    /// <summary>The <c>role="menu"</c> list around the given rows, drawn by the shared engine.</summary>
    internal RenderFragment<RenderFragment> List => _list ??= items => builder => OmniMenuController.BuildMenuList(
        builder,
        owner,
        MenuId,
        "omni-menu omni-html-editor__menu",
        label(),
        null,
        this,
        items,
        HandleKeyAsync);

    /// <summary>Opens the menu at this point of the viewport, its first item focused.</summary>
    internal Task OpenAsync(double x, double y)
    {
        Controller.RequestPlacement(focusLast: false, x, y);
        return Controller.SetOpenAsync(true, null, default, owner.Rerender);
    }

    /// <summary>Closes the menu; the focus goes back to the surface with <paramref name="restoreFocus"/>.</summary>
    public Task CloseAsync(bool restoreFocus) => _controller is null
        ? Task.CompletedTask
        : _controller.SetOpenAsync(false, null, default, owner.Rerender, restoreFocus);

    /// <summary>Brings the script in line with the menu's open state, placed at the pointer. Called after each render.</summary>
    internal async Task SyncAsync()
    {
        if (_controller is not null)
        {
            await _controller.SyncAsync(IsOpen, MenuId, owner.SurfaceElement, OmniMenuPlacement.Pointer);
        }
    }

    private Task HandleKeyAsync(KeyboardEventArgs args) => Controller.HandleMenuKeyAsync(args, MenuId, CloseAsync);

    /// <summary>Detaches the script from a menu still open and releases its module; a lost circuit is ignored there.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_controller is not null)
        {
            await _controller.DisposeAsync();
        }
    }
}
