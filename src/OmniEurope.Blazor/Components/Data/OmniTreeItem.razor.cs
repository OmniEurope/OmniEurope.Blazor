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
    public int TabIndex { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool HasChildren => ChildContent is not null || LoadChildren is not null;
    private bool Selected => Context?.SelectedValues.Contains(Value, EqualityComparer<TValue>.Default) == true;

    protected override void OnParametersSet()
    {
        if (_observedExpanded is null || _observedExpanded.Value != Expanded)
        {
            _expanded = Expanded;
            _observedExpanded = Expanded;
        }
        if (!Equals(_observedLoader, LoadChildren))
        {
            _loadCancellation?.Cancel();
            _loaded = false;
            _loadError = false;
            _observedLoader = LoadChildren;
        }
    }

    private async Task ToggleExpandedAsync()
    {
        _expanded = !_expanded;
        await ExpandedChanged.InvokeAsync(_expanded);
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
