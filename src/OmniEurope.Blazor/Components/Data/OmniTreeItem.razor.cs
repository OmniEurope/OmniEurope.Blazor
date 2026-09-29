namespace OmniEurope.Blazor.Components;

/// <summary>An item of an <see cref="OmniTree{TValue}"/>, with its child items declared or loaded on demand.</summary>
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
    /// Raised with the exception when <see cref="LoadChildren"/> fails (a cancelled load is not a
    /// failure); the item then shows a localized error message.
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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var load = false;
        if (_observedExpanded is null || _observedExpanded.Value != Expanded)
        {
            _expanded = Expanded;
            _observedExpanded = Expanded;
            load = _expanded;
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
    /// the <see cref="LoadFailed"/> handler reaches the renderer as it would from a lifecycle method.
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
        _loadCancellation = new CancellationTokenSource();
        _loading = true;
        _loadError = false;
        try
        {
            await LoadChildren(_loadCancellation.Token);
            _loaded = true;
        }
        catch (OperationCanceledException) when (_loadCancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _loadError = true;
            await OnLoadError.InvokeAsync(exception);
        }
        finally { _loading = false; }
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

    public ValueTask DisposeAsync()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
