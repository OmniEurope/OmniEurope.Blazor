using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The grid script (omni-grid.js) seen from a grid: the module, imported only once something needs it,
/// the gestures and observers attached to the viewport and detached on disposal, the pass run after
/// each render and the wait for the images of the rows.
/// </summary>
internal sealed class GridScriptBridge<TItem>(OmniDataGrid<TItem> grid) : IAsyncDisposable
{
    private const string GridModulePath = OmniModules.Grid;

    // Longer than the image wait of omni-grid.js, so the script normally answers first.
    private static readonly TimeSpan PreparationTimeout = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _preparationLifetime = new();
    private IJSObjectReference? _gridModule;
    private DotNetObjectReference<OmniDataGrid<TItem>>? _selfReference;
    private bool _virtualAttached;
    private bool _resizeAttached;
    private bool _filterMenuAttached;
    private bool _fillAttached;
    private bool _frozenScrollAttached;
    private bool _cutTooltipsInstalled;
    private bool _columnHoverAttached;
    private string? _wheelScopeAttached;
    private bool _renderReady;

    /// <summary>The module, or null while nothing has needed it.</summary>
    internal IJSObjectReference? Module => _gridModule;

    /// <summary>The module, imported on first use.</summary>
    internal async Task<IJSObjectReference> ModuleAsync() =>
        _gridModule ??= await grid.JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);

    private DotNetObjectReference<OmniDataGrid<TItem>> SelfReference => _selfReference ??= DotNetObjectReference.Create(grid);

    internal bool Preparing => grid.LoadingContent is not null && !_renderReady;

    // Waiting for the images of rows already there. The headers always stay and the LoadingContent sits in
    // a row under them, while the rows load and while their images do: a veil over the whole grid hid the
    // headers, against the shared acceptance rule (RET-002 §3.8).
    internal bool Veiled => Preparing && !grid.View.Loading;

    /// <summary>Whether Enter on a resize handle can fit its column to content.</summary>
    // The resize gesture is attached only once the module is loaded.
    internal bool CanAutoFit => _resizeAttached;

    internal Task AutoFitColumnAsync(string key) => _gridModule!.InvokeVoidAsync("autoFitColumn", grid.Viewport, key).AsTask();

    /// <summary>Scrolls the viewport to an offset; nothing while the script is not loaded.</summary>
    internal async Task ScrollToOffsetAsync(double offset)
    {
        if (_gridModule is not null)
        {
            await _gridModule.InvokeVoidAsync("scrollToOffset", grid.Viewport, offset);
        }
    }

    /// <summary>The script work after a render, the virtual window included when the grid virtualizes.</summary>
    internal async Task AfterRenderAsync()
    {
        var layout = grid.LayoutInterop;
        if (!grid.View.Virtualized)
        {
            await DetachViewportAsync();
            await layout.ApplyLayoutAsync();
            await layout.ApplyMaxHeightAsync();
            await EnsureResizeInteropAsync();
            await EnsureFilterMenuInteropAsync();
            await EnsureFillInteropAsync();
            await EnsureWheelScopeInteropAsync();
            await EnsureFrozenScrollInteropAsync();
            await EnsureCutTooltipsAsync();
            await EnsureColumnHoverAsync();
            await CompletePreparationAsync();
            return;
        }

        var module = await ModuleAsync();
        await EnsureResizeInteropAsync();
        await EnsureFilterMenuInteropAsync();
        await EnsureFillInteropAsync();
        await EnsureWheelScopeInteropAsync();
        await EnsureFrozenScrollInteropAsync();
        await EnsureCutTooltipsAsync();
        await EnsureColumnHoverAsync();
        if (!_virtualAttached)
        {
            await module.InvokeVoidAsync("attach", grid.Viewport, SelfReference);
            _virtualAttached = true;
        }

        // A slot holds group headers and a detail row besides its item row, so it is measured even
        // when the item rows themselves have a fixed height. The spacers of the rows just rendered
        // are set before anything is read, so the scroll is never measured on a shortened content.
        var viewport = grid.Virtual;
        var snapshot = await module.InvokeAsync<GridViewportSnapshot?>("sync", grid.Viewport, viewport.FixedHeight is null || grid.View.StructuredVirtual, viewport.Range.TopSpacer, viewport.Range.BottomSpacer);
        var moved = viewport.ApplySnapshot(snapshot);
        var previous = viewport.Range;
        viewport.Sync();
        await layout.ApplyVirtualLayoutAsync(module);
        await layout.ApplyMaxHeightAsync();
        await layout.ApplyColumnLayoutAsync();
        await layout.ApplyRowHeightAsync(viewport.FixedHeight);
        await grid.View.EnsureVirtualDataAsync();
        if (moved || previous != viewport.Range)
        {
            grid.Render();
        }
        else
        {
            await CompletePreparationAsync();
        }
    }

    /// <summary>
    /// Once a row entered edit mode, its editor takes the focus with its text selected: the one of the
    /// cell just pressed in that row, otherwise the first one (recette R1-5). The request holds until a
    /// render has put the marked row in the page.
    /// </summary>
    internal async Task FocusEditorAsync()
    {
        if (!grid.Editing.FocusRequested || grid.DisposeRequested)
        {
            return;
        }

        var module = await ModuleAsync();
        if (await module.InvokeAsync<bool>("focusEditor", grid.Viewport))
        {
            grid.Editing.FocusGiven();
        }
    }

    /// <summary>
    /// A text cell cut by its ellipsis shows its whole value in the package tooltip while the pointer
    /// or the focus is on it (recette R-032). The page shares one set of listeners for every grid.
    /// </summary>
    private async Task EnsureCutTooltipsAsync()
    {
        if (_cutTooltipsInstalled)
        {
            return;
        }

        var module = await ModuleAsync();
        await module.InvokeVoidAsync("installPackageTooltips");
        _cutTooltipsInstalled = true;
    }

    /// <summary>
    /// Wires the column hover once the grid first highlights the hovered column; the script reads the
    /// grid root class on each move, so turning the option off again needs no detach.
    /// </summary>
    private async Task EnsureColumnHoverAsync()
    {
        if (_columnHoverAttached || !grid.HighlightColumnOnHover)
        {
            return;
        }

        var module = await ModuleAsync();
        await module.InvokeVoidAsync("attachColumnHover", grid.Viewport);
        _columnHoverAttached = true;
    }

    private async Task CompletePreparationAsync()
    {
        if (!Preparing || grid.View.Loading || grid.DisposeRequested) return;
        var module = await ModuleAsync();

        // Bounded, and cancelled by disposal: an image whose request never ends must neither keep the
        // grid hidden nor hold the lifecycle gate. A wait that times out or fails shows the grid as it is.
        bool ready;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_preparationLifetime.Token);
        cancellation.CancelAfter(PreparationTimeout);
        try
        {
            ready = await module.InvokeAsync<bool>("waitForReady", cancellation.Token, grid.Viewport);
        }
        catch (OperationCanceledException)
        {
            ready = true;
        }
        catch (JSException)
        {
            ready = true;
        }

        if (ready && !grid.DisposeRequested)
        {
            _renderReady = true;
            grid.Render();
        }
    }

    /// <summary>
    /// Wires the pointer gesture of the column resize handles once, and only when at least one
    /// column can actually be resized or auto-fitted, so a read-only grid still needs no script.
    /// </summary>
    private async Task EnsureResizeInteropAsync()
    {
        if (_resizeAttached || !grid.ColumnSet.VisibleColumns.Any(grid.ColumnLayout.HasEdgeHandle))
        {
            return;
        }

        var module = await ModuleAsync();
        await module.InvokeVoidAsync("attachResize", grid.Viewport, SelfReference, GridColumnLayout<TItem>.MinimumColumnWidth);
        _resizeAttached = true;
    }

    /// <summary>
    /// Watches the horizontal scroll once a column is frozen, so the detach control can follow the
    /// cycle documented in docs/data-components.md. The script reports only when the viewport leaves
    /// or reaches its start, never on every scrolled frame.
    /// </summary>
    private async Task EnsureFrozenScrollInteropAsync()
    {
        var frozen = grid.FrozenState;
        frozen.EndDetachmentWithoutFrozenColumns();
        if (_frozenScrollAttached || !grid.ColumnSet.HasFrozenColumns)
        {
            return;
        }

        var module = await ModuleAsync();
        var self = SelfReference;
        _frozenScrollAttached = true;
        var scrolled = await module.InvokeAsync<bool>("attachFrozenScroll", grid.Viewport, self);
        if (scrolled != frozen.HorizontallyScrolled)
        {
            frozen.ApplyHorizontalScroll(scrolled);
            grid.Render();
        }
    }

    /// <summary>
    /// Closes any open header filter popover after a suggestion was chosen in one of them, when the
    /// grid was told to hide the menu on select.
    /// </summary>
    internal async Task CloseFilterMenusAsync()
    {
        if (!grid.HideFilterMenuOnSelect || !_filterMenuAttached || _gridModule is null)
        {
            return;
        }

        await _gridModule.InvokeVoidAsync("closeFilterMenus", grid.Viewport);
    }

    /// <summary>
    /// Wires the popovers once: the header filter menus, the advanced filter panels and the folded
    /// checkable lists. The script closes them on an outside click or Escape and places their panel
    /// over the page, since the scrolling viewport would otherwise clip it; the text filter with
    /// suggestions gets the same placement for its list.
    /// </summary>
    private async Task EnsureFilterMenuInteropAsync()
    {
        if (_filterMenuAttached || !grid.FilterEditor.HasPopovers)
        {
            return;
        }

        var module = await ModuleAsync();
        await module.InvokeVoidAsync("attachFilterMenus", grid.Viewport, grid.HideFilterMenuOnSelect);
        _filterMenuAttached = true;
    }

    /// <summary>Follows the wheel scroll scope: attached once per selector, detached when cleared.</summary>
    private async Task EnsureWheelScopeInteropAsync()
    {
        var scope = string.IsNullOrWhiteSpace(grid.WheelScrollScope) ? null : grid.WheelScrollScope.Trim();
        if (string.Equals(scope, _wheelScopeAttached, StringComparison.Ordinal))
        {
            return;
        }

        var module = await ModuleAsync();
        if (scope is null)
        {
            await module.InvokeVoidAsync("detachWheelScope", grid.Viewport);
        }
        else
        {
            await module.InvokeVoidAsync("attachWheelScope", grid.Viewport, scope);
        }

        _wheelScopeAttached = scope;
    }

    /// <summary>
    /// In fill mode the script sizes the grid to what is left below it in its scrolling area, and
    /// follows that area as it resizes; leaving fill mode hands the height back to the stylesheet.
    /// </summary>
    private async Task EnsureFillInteropAsync()
    {
        if (grid.FillAvailableHeight == _fillAttached)
        {
            return;
        }

        var module = await ModuleAsync();
        await module.InvokeVoidAsync(grid.FillAvailableHeight ? "attachFill" : "detachFill", grid.Viewport);
        _fillAttached = grid.FillAvailableHeight;
    }

    private async Task DetachViewportAsync()
    {
        if (!_virtualAttached || _gridModule is null)
        {
            return;
        }

        _virtualAttached = false;
        try
        {
            await _gridModule.InvokeVoidAsync("detach", grid.Viewport);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Stops the wait for the images: disposal must not wait for it.</summary>
    internal Task CancelPreparationAsync() => _preparationLifetime.CancelAsync();

    internal void ReleasePreparation() => _preparationLifetime.Dispose();

    /// <summary>Detaches everything attached to the viewport, then releases the module and the grid's reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await DetachViewportAsync();
            // Every listener is attached once the module is loaded: without a module, nothing is attached.
            if (_gridModule is not null)
            {
                foreach (var function in TakeAttached())
                {
                    object?[] arguments = function == UninstallTooltips ? [] : [grid.Viewport];
                    await _gridModule.InvokeVoidAsync(function, arguments);
                }

                await _gridModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
        finally
        {
            // Released: a late call finds no module rather than a disposed one.
            _gridModule = null;
        }

        _selfReference?.Dispose();
    }

    private const string UninstallTooltips = "uninstallPackageTooltips";

    /// <summary>The script functions that undo what is attached, in order; each is forgotten once named.</summary>
    private List<string> TakeAttached()
    {
        var functions = new List<string>();
        if (_resizeAttached)
        {
            functions.Add("detachResize");
        }

        if (_filterMenuAttached)
        {
            functions.Add("detachFilterMenus");
        }

        if (_wheelScopeAttached is not null)
        {
            functions.Add("detachWheelScope");
        }

        if (_fillAttached)
        {
            functions.Add("detachFill");
        }

        if (_frozenScrollAttached)
        {
            functions.Add("detachFrozenScroll");
        }

        if (_columnHoverAttached)
        {
            functions.Add("detachColumnHover");
        }

        if (_cutTooltipsInstalled)
        {
            functions.Add(UninstallTooltips);
        }

        (_resizeAttached, _filterMenuAttached, _wheelScopeAttached, _fillAttached) = (false, false, null, false);
        (_frozenScrollAttached, _columnHoverAttached, _cutTooltipsInstalled) = (false, false, false);
        return functions;
    }
}
