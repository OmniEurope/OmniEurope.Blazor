namespace OmniEurope.Blazor.Components;

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

    [Parameter]
    public TValue Value { get; set; } = default!;

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

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    [Parameter]
    public Func<CancellationToken, Task>? LoadChildren { get; set; }

    [Parameter]
    public EventCallback<Exception> LoadFailed { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

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
            await LoadFailed.InvokeAsync(exception);
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
