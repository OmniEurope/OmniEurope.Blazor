namespace OmniEurope.Blazor.Components;

public partial class OmniDataList<TItem>
{
    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private IReadOnlyList<TItem> _items = Array.Empty<TItem>();
    private Exception? _error;
    private bool _loading;
    private bool _hasLoaded;
    private Func<CancellationToken, Task<IReadOnlyList<TItem>>>? _observedLoader;

    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

    [Parameter]
    public Func<CancellationToken, Task<IReadOnlyList<TItem>>>? Load { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    [Parameter]
    public bool Virtualize { get; set; }

    private RenderFragment DefaultLoading => builder => builder.AddContent(0, Localize("Loading"));
    private RenderFragment DefaultEmpty => builder => builder.AddContent(0, Localize("DataListEmpty"));

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        if (Load is null)
        {
            _loadCancellation?.Cancel();
            _loadGeneration++;
            _items = Items;
            _error = null;
            _loading = false;
            _hasLoaded = false;
            _observedLoader = null;
            return;
        }

        if ((!_hasLoaded || !ReferenceEquals(_observedLoader, Load)) && !_loading && _error is null)
        {
            _observedLoader = Load;
            await LoadAsync();
        }
    }

    public Task ReloadAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (Load is null)
        {
            _items = Items;
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        var generation = ++_loadGeneration;
        _loading = true;
        _error = null;

        try
        {
            var items = await Load(token);
            if (generation == _loadGeneration)
            {
                _items = items;
                _hasLoaded = true;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (generation == _loadGeneration)
            {
                _error = exception;
                _items = Array.Empty<TItem>();
            }
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                _loading = false;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _loadCancellation?.Cancel();
        _loadGeneration++;
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
