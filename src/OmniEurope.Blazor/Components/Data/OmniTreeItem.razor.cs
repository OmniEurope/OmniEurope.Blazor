namespace OmniEurope.Blazor.Components;

/// <summary>An item of an <see cref="OmniTree{TValue}"/>, with its child items declared or loaded on demand.</summary>
/// <typeparam name="TValue">The type of the value the item stands for; the same as its tree's.</typeparam>
public partial class OmniTreeItem<TValue>
{
    private bool _expanded;
    private bool _loaded;
    private bool _loading;
    private bool _loadError;
    private CancellationTokenSource? _loadCancellation;
    private bool? _observedExpanded;
    private Func<CancellationToken, Task>? _observedLoader;

    [CascadingParameter]
    private OmniTreeContext<TValue>? Context { get; set; }

    // The item this one is nested in, so a drag never drops an item into its own branch.
    [CascadingParameter(Name = "OmniTreeParentItem")]
    private OmniTreeItem<TValue>? ParentItem { get; set; }

    /// <summary>The value this item stands for in the tree's selection.</summary>
    [Parameter]
    public TValue Value { get; set; } = default!;

    /// <summary>The item's text, also its accessible name.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// What the row shows in place of <see cref="Text"/> (an icon, a badge, a styled label), apart
    /// from the child items in <c>ChildContent</c>. It sits inside the row's button, so it holds no
    /// interactive element; <see cref="Text"/> then becomes the row's accessible name. Null shows
    /// <see cref="Text"/>, as before.
    /// </summary>
    [Parameter]
    public RenderFragment? TextContent { get; set; }

    /// <summary>Whether the child items are shown (<c>@bind-Expanded</c>).</summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Raised when the reader opens or closes the item.</summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>
    /// Loads the child items the first time the item opens (the host then renders them in
    /// <see cref="ChildContent"/>); a newer opening cancels a load still running.
    /// </summary>
    [Parameter]
    public Func<CancellationToken, Task>? LoadChildren { get; set; }

    /// <summary>
    /// Raised with the exception when <see cref="LoadChildren"/> fails; the item then shows a localized
    /// error message. A cancelled load is not a failure, nor is a load replaced by a newer one: the
    /// newer load alone decides what the item shows.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnLoadError { get; set; }

    /// <summary>Makes the item impossible to select.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The child items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool HasChildren => ChildContent is not null || LoadChildren is not null;
    private bool Selected => Context?.SelectedValues.Contains(Value, EqualityComparer<TValue>.Default) == true;

    /// <summary>
    /// Takes <see cref="Expanded"/> when it differs from the value last received, and a new
    /// <see cref="LoadChildren"/> (cancelling a load still running and forgetting the loaded children).
    /// A branch opened this way, or given another loader while open, starts loading its children
    /// without waiting for the load to finish.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var load = false;
        if (_observedExpanded is null || _observedExpanded.Value != Expanded)
        {
            // Rendered under a tree being expanded as a whole: opens too, and says so to its host.
            var openedByTree = _observedExpanded is null && !Expanded && Context?.ExpandAll == true && HasChildren;
            _expanded = Expanded || openedByTree;
            _observedExpanded = Expanded;
            load = _expanded;
            if (openedByTree)
            {
                _ = InvokeAsync(() => ExpandedChanged.InvokeAsync(true));
            }
        }
        if (!Equals(_observedLoader, LoadChildren))
        {
            _loadCancellation?.Cancel();
            _loaded = false;
            _loadError = false;
            _observedLoader = LoadChildren;
            load |= _expanded;
        }

        // Opened by the parent (initially or later), or given another loader while open: the branch
        // loads as it would when opened from its toggle.
        if (load)
        {
            _ = LoadOpenedBranchAsync();
        }
    }

    /// <summary>
    /// Loads the children of a branch opened through its parameters, then renders them. A failure of
    /// the <see cref="OnLoadError"/> handler reaches the renderer as it would from a lifecycle method.
    /// </summary>
    private async Task LoadOpenedBranchAsync()
    {
        try
        {
            await EnsureChildrenLoadedAsync();
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
            return;
        }

        StateHasChanged();
    }

    private async Task ToggleExpandedAsync()
    {
        _expanded = !_expanded;
        await ExpandedChanged.InvokeAsync(_expanded);
        await EnsureChildrenLoadedAsync();
    }

    private async Task EnsureChildrenLoadedAsync()
    {
        if (!_expanded || _loaded || LoadChildren is null)
        {
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        // Each load keeps its own source: a load replaced by a newer one is judged on its own token and
        // leaves the loading state, the error and the loaded flag to the load that replaced it.
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        _loading = true;
        _loadError = false;
        try
        {
            await LoadChildren(cancellation.Token);
            if (ReferenceEquals(_loadCancellation, cancellation))
            {
                _loaded = true;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception) when (ReferenceEquals(_loadCancellation, cancellation))
        {
            _loadError = true;
            await OnLoadError.InvokeAsync(exception);
        }
        catch (Exception) { /* A replaced load failed after the fact: its outcome is no longer shown. */ }
        finally
        {
            if (ReferenceEquals(_loadCancellation, cancellation))
            {
                _loading = false;
            }
        }
    }
    /// <summary>Opens or closes the item from its tree (expand or collapse all), loading its children when it opens.</summary>
    internal async Task SetExpandedAsync(bool expanded)
    {
        if (!HasChildren || _expanded == expanded)
        {
            return;
        }

        await ToggleExpandedAsync();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Context?.Items.Add(this);

    private OmniTree<TValue>? Tree => Context?.Tree;

    private bool CanDragThis => Tree is { AllowDragDrop: true } tree && !Disabled && (tree.CanDrag?.Invoke(Value) ?? true);

    /// <summary>Whether the item being dragged may land here: not itself, not into its own branch, and the host agrees.</summary>
    private bool AcceptsDrop => Context?.Dragged is { } dragged && Tree is { AllowDragDrop: true } tree
        && !ReferenceEquals(dragged, this) && !IsInside(dragged)
        && (tree.CanDrop?.Invoke(dragged.Value, Value) ?? true);

    private bool IsInside(OmniTreeItem<TValue> ancestor)
    {
        for (var parent = ParentItem; parent is not null; parent = parent.ParentItem)
        {
            if (ReferenceEquals(parent, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private string RowClass => Internal.CssClassBuilder.Combine([
        "omni-tree__row",
        Context?.Dragged == this ? "omni-tree__row--dragging" : null,
        Context?.DropTarget == this ? "omni-tree__row--drop-target" : null]);

    private void DragStart()
    {
        if (Context is null || !CanDragThis)
        {
            return;
        }

        Context.Dragged = this;
        Context.DropTarget = null;
        Context.Redraw();
    }

    private void DragEnd()
    {
        if (Context?.Dragged is null)
        {
            return;
        }

        Context.Dragged = null;
        Context.DropTarget = null;
        Context.Redraw();
    }

    private void DragEnter()
    {
        if (Context is not null && AcceptsDrop && Context.DropTarget != this)
        {
            Context.DropTarget = this;
            Context.Redraw();
        }
    }

    private void DragLeave()
    {
        if (Context?.DropTarget == this)
        {
            Context.DropTarget = null;
            Context.Redraw();
        }
    }

    private async Task DropAsync()
    {
        if (Context?.Dragged is not { } dragged)
        {
            return;
        }

        var accepted = AcceptsDrop;
        Context.Dragged = null;
        Context.DropTarget = null;
        Context.Redraw();
        if (accepted && Tree is { } tree)
        {
            await tree.OnItemDropped.InvokeAsync(new OmniTreeDropEventArgs<TValue>(dragged.Value, Value));
        }
    }

    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.ToggleSelectionAsync(Value);

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "ArrowRight" && HasChildren)
        {
            if (!_expanded) return ToggleExpandedAsync();
        }
        else if (args.Key == "ArrowLeft" && HasChildren)
        {
            if (_expanded) return ToggleExpandedAsync();
        }
        else if (args.Key is "Enter" or " ")
        {
            return SelectAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>Cancels a child load still running.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync()
    {
        Context?.Items.Remove(this);
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
