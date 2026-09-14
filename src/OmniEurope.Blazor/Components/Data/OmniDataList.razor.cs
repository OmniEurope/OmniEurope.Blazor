using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniDataList<TItem>
{
    private const string GridModulePath = "./_content/OmniEurope.Blazor/omni-grid.js";

    // Starting estimate for an item that has not been measured yet; every rendered item is then
    // measured, so the estimate only shapes the scroll range of the part not seen so far.
    private const double EstimatedItemHeight = 32d;
    private const int OverscanCount = 4;

    private readonly GridVirtualWindow _window = new();
    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private IReadOnlyList<TItem> _items = Array.Empty<TItem>();
    private IReadOnlyList<TItem>? _windowItems;
    private Exception? _error;
    private bool _loading;
    private bool _hasLoaded;
    private Func<CancellationToken, Task<IReadOnlyList<TItem>>>? _observedLoader;
    private ElementReference _root;
    private IJSObjectReference? _module;
    private DotNetObjectReference<OmniDataList<TItem>>? _selfReference;
    private bool _attached;
    private double _scrollTop;
    private double _viewportHeight;
    private GridVirtualRange _range;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

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

    /// <summary>
    /// Renders only the items around the visible part of the list, inside whichever ancestor scrolls
    /// (or the page). Spacers stand in for the rest; their heights are set by <c>omni-grid.js</c> through
    /// a CSS custom property, never a <c>style</c> attribute, so the strict CSP of the library holds.
    /// </summary>
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
            SyncWindow();
            return;
        }

        if ((!_hasLoaded || !ReferenceEquals(_observedLoader, Load)) && !_loading && _error is null)
        {
            _observedLoader = Load;
            await LoadAsync();
        }

        SyncWindow();
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
                SyncWindow();
            }
        }
    }

    private void SyncWindow()
    {
        if (!ReferenceEquals(_windowItems, _items))
        {
            // A new collection invalidates the measured heights of the previous one.
            _windowItems = _items;
            _window.Configure(_items.Count, EstimatedItemHeight);
            _window.ResetMeasurements();
        }

        _range = _window.Compute(_scrollTop, _viewportHeight, OverscanCount);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!Virtualize || _items.Count == 0)
        {
            await DetachAsync();
            return;
        }

        _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        if (!_attached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("attachList", _root, _selfReference);
            _attached = true;
        }

        var snapshot = await _module.InvokeAsync<GridViewportSnapshot?>("syncList", _root);
        var moved = ApplySnapshot(snapshot);
        var previous = _range;
        SyncWindow();
        await _module.InvokeVoidAsync("applyListLayout", _root, _range.TopSpacer, _range.BottomSpacer);
        if (moved || previous != _range)
        {
            StateHasChanged();
        }
    }

    private bool ApplySnapshot(GridViewportSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return false;
        }

        var moved = false;
        if (Math.Abs(snapshot.ViewportHeight - _viewportHeight) > 0.5d)
        {
            _viewportHeight = snapshot.ViewportHeight;
            moved = true;
        }

        if (Math.Abs(snapshot.ScrollTop - _scrollTop) > 0.5d)
        {
            _scrollTop = snapshot.ScrollTop;
            moved = true;
        }

        foreach (var row in snapshot.Rows ?? [])
        {
            moved |= _window.Measure(row.Index, row.Height);
        }

        return moved;
    }

    /// <summary>
    /// Invoked by the list script when the scrolling ancestor moves or resizes. <paramref name="scrollTop"/>
    /// is the offset of the visible area inside the list, not the scroll position of the ancestor.
    /// </summary>
    [JSInvokable]
    public Task OnViewportChangedAsync(double scrollTop, double viewportHeight)
    {
        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;
        var previous = _range;
        SyncWindow();
        if (previous != _range)
        {
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private async Task DetachAsync()
    {
        if (!_attached || _module is null)
        {
            return;
        }

        _attached = false;
        try
        {
            await _module.InvokeVoidAsync("detachList", _root);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _loadCancellation?.Cancel();
        _loadGeneration++;
        _loadCancellation?.Dispose();
        try
        {
            await DetachAsync();
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfReference?.Dispose();
    }
}
