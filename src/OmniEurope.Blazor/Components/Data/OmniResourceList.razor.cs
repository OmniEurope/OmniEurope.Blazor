namespace OmniEurope.Blazor.Components;

/// <summary>
/// The body of a list page: a toolbar with a search box, the host's filters and actions, a refresh
/// and a create button; the grid of <see cref="Columns"/> over <see cref="Items"/> or
/// <see cref="Load"/>; an empty state that offers to create the first item, or to clear a search that
/// found nothing; and a retry state in place of the grid when loading failed.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <remarks>
/// <para>
/// With <see cref="Items"/>, the search filters the rows locally through <see cref="SearchFilter"/>;
/// without one, the host filters the list it passes. With <see cref="Load"/>, the host's loader reads
/// <see cref="SearchText"/> (bound), and a search that settles for <see cref="SearchDelay"/> reloads the
/// grid from its first page.
/// </para>
/// <para>
/// A loader that throws is caught: the list shows <see cref="ErrorMessage"/> (or a localized default)
/// with a retry button instead of the grid. A host that loads by itself reports its failure by setting
/// <see cref="ErrorMessage"/>, and hears the retry through <see cref="OnRetry"/>.
/// </para>
/// </remarks>
public partial class OmniResourceList<TItem> : IDisposable
{
    private OmniDataGrid<TItem>? _grid;
    private string _searchText = string.Empty;
    private string? _observedSearchText;
    private IReadOnlyList<TItem>? _filteredSource;
    private string? _filteredText;
    private Func<TItem, string, bool>? _filteredBy;
    private IReadOnlyList<TItem> _visibleItems = Array.Empty<TItem>();
    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? _observedLoad;
    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? _gridLoad;
    private CancellationTokenSource? _searchDelay;
    private Exception? _loadError;
    private int? _loadedTotal;
    private int _page = 1;
    private bool _reloadPending;

    /// <summary>The grid columns, passed as they are to <see cref="OmniDataGrid{TItem}"/>.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? Columns { get; set; }

    /// <summary>The rows, when the host holds them all. Ignored when <see cref="Load"/> is set.</summary>
    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Loads a page of rows; the host reads <see cref="SearchText"/> in it.</summary>
    [Parameter]
    public Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? Load { get; set; }

    /// <summary>Accessible name of the list.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether the toolbar shows the search box.</summary>
    [Parameter]
    public bool ShowSearch { get; set; } = true;

    /// <summary>The searched text.</summary>
    [Parameter]
    public string? SearchText { get; set; }

    /// <summary>Raised on every keystroke in the search box.</summary>
    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    /// <summary>Placeholder and accessible name of the search box; the localized "search" when empty.</summary>
    [Parameter]
    public string? SearchPlaceholder { get; set; }

    /// <summary>Whether a row matches the searched text, for a local search over <see cref="Items"/>.</summary>
    [Parameter]
    public Func<TItem, string, bool>? SearchFilter { get; set; }

    /// <summary>How long the search must settle before a <see cref="Load"/> grid reloads.</summary>
    [Parameter]
    public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>The host's filters, after the search box: a status list, a switch.</summary>
    [Parameter]
    public RenderFragment? Filters { get; set; }

    /// <summary>The host's actions, before the create button.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>The text of the create button, shown in the toolbar and in the empty state. No button without it.</summary>
    [Parameter]
    public string? CreateText { get; set; }

    /// <summary>The icon of the create button.</summary>
    [Parameter]
    public OmniIconName CreateIcon { get; set; } = OmniIconName.Add;

    /// <summary>Raised by the create button.</summary>
    [Parameter]
    public EventCallback OnCreate { get; set; }

    /// <summary>Whether the user may create; the button stays visible but disabled when not.</summary>
    [Parameter]
    public bool CanCreate { get; set; } = true;

    /// <summary>Why the create button is disabled, shown on hover when <see cref="CanCreate"/> is false.</summary>
    [Parameter]
    public string? CreateDisabledReason { get; set; }

    /// <summary>Whether the toolbar shows a refresh button.</summary>
    [Parameter]
    public bool ShowRefresh { get; set; }

    /// <summary>Raised by the refresh button, after a <see cref="Load"/> grid has reloaded.</summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>Whether the toolbar shows how many rows there are.</summary>
    [Parameter]
    public bool ShowCount { get; set; }

    /// <summary>The title of the empty state; the localized "nothing here yet" when empty.</summary>
    [Parameter]
    public string? EmptyTitle { get; set; }

    /// <summary>The explanation of the empty state.</summary>
    [Parameter]
    public string? EmptyDescription { get; set; }

    /// <summary>The icon of the empty state.</summary>
    [Parameter]
    public RenderFragment? EmptyIcon { get; set; }

    /// <summary>Replaces the whole empty state shown when there is no row and no search.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Shows the retry state with this message instead of the grid.</summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>Raised by the retry button, before a failed <see cref="Load"/> is tried again.</summary>
    [Parameter]
    public EventCallback OnRetry { get; set; }

    /// <summary>Raised when <see cref="Load"/> throws.</summary>
    [Parameter]
    public EventCallback<Exception> OnLoadError { get; set; }

    /// <summary>Forces the busy state of the grid, for a host that loads by itself.</summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>Identifies a row, as the grid's <see cref="OmniDataGrid{TItem}.KeySelector"/>.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Raised when a row is clicked, or activated with Enter or Space.</summary>
    [Parameter]
    public EventCallback<TItem> RowClick { get; set; }

    /// <summary>Rows per page.</summary>
    [Parameter]
    public int PageSize { get; set; } = 25;

    /// <summary>Whether the grid pages its rows.</summary>
    [Parameter]
    public bool AllowPaging { get; set; } = true;

    /// <summary>Whether the columns sort.</summary>
    [Parameter]
    public bool AllowSorting { get; set; } = true;

    /// <summary>The height of the grid, as the grid's <see cref="OmniDataGrid{TItem}.Height"/>.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Whether the grid fills the height left to it.</summary>
    [Parameter]
    public bool FillAvailableHeight { get; set; }

    /// <summary>The row density of the grid.</summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>The grid inside the list, for what the list does not expose.</summary>
    public OmniDataGrid<TItem>? Grid => _grid;

    private bool HasSearch => !string.IsNullOrWhiteSpace(_searchText);

    private bool HasCreate => !string.IsNullOrWhiteSpace(CreateText) && OnCreate.HasDelegate;

    private string EffectiveSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder) ? Localize("ResourceListSearch") : SearchPlaceholder;

    private string? FailureMessage => !string.IsNullOrWhiteSpace(ErrorMessage)
        ? ErrorMessage
        : _loadError is not null ? Localize("ResourceListLoadFailed") : null;

    private IReadOnlyList<TItem> VisibleItems => Load is null ? _visibleItems : Array.Empty<TItem>();

    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? GridLoad => Load is null ? null : _gridLoad;

    private int? CountValue => Load is null ? _visibleItems.Count : _loadedTotal;

    private RenderFragment EmptyTemplate => HasSearch ? NoMatchState : EmptyContent ?? EmptyState;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.Equals(SearchText, _observedSearchText, StringComparison.Ordinal))
        {
            _observedSearchText = SearchText;
            _searchText = SearchText ?? string.Empty;
        }

        // The loader handed to the grid is ours, stable across renders so the grid does not reload on
        // each of them; it is rebuilt, and the grid reloads, only when the host's loader really changes.
        if (!Equals(Load, _observedLoad))
        {
            _observedLoad = Load;
            _gridLoad = Load is null ? null : new Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>(LoadThroughAsync);
            _loadError = null;
        }

        FilterItems();
    }

    private void FilterItems()
    {
        var source = Items ?? Array.Empty<TItem>();
        if (ReferenceEquals(source, _filteredSource) && string.Equals(_filteredText, _searchText, StringComparison.Ordinal) && Equals(_filteredBy, SearchFilter))
        {
            return;
        }

        _filteredSource = source;
        _filteredText = _searchText;
        _filteredBy = SearchFilter;
        var needle = _searchText.Trim();
        _visibleItems = SearchFilter is null || needle.Length == 0
            ? source
            : [.. source.Where(item => SearchFilter(item, needle))];
    }

    private async Task<OmniDataGridResult<TItem>> LoadThroughAsync(OmniDataGridLoadRequest request)
    {
        try
        {
            var result = await Load!(request);
            _loadError = null;
            _loadedTotal = result.TotalCount;
            await InvokeAsync(StateHasChanged);
            return result;
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _loadError = exception;
            await OnLoadError.InvokeAsync(exception);
            _ = InvokeAsync(StateHasChanged);
            throw;
        }
    }

    private async Task SearchChangedAsync(string? text)
    {
        _searchText = text ?? string.Empty;
        _observedSearchText = text;
        await SearchTextChanged.InvokeAsync(text);
        if (Load is null)
        {
            _page = 1;
            FilterItems();
            return;
        }

        await ReloadAfterDelayAsync();
    }

    private async Task ClearSearchAsync()
    {
        _searchText = string.Empty;
        _observedSearchText = string.Empty;
        await SearchTextChanged.InvokeAsync(string.Empty);
        _page = 1;
        FilterItems();
        if (Load is not null)
        {
            _searchDelay?.Cancel();
            _reloadPending = true;
        }
    }

    /// <summary>Waits for the search to settle, then asks for a reload from the first page after the next render.</summary>
    private async Task ReloadAfterDelayAsync()
    {
        _searchDelay?.Cancel();
        _searchDelay?.Dispose();
        _searchDelay = new CancellationTokenSource();
        var token = _searchDelay.Token;
        try
        {
            if (SearchDelay > TimeSpan.Zero)
            {
                await Task.Delay(SearchDelay, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _page = 1;
        _reloadPending = true;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The grid has its first page by now, bound through Page: reloading reads the new search.
        if (_reloadPending && _grid is not null)
        {
            _reloadPending = false;
            await _grid.ReloadAsync();
            StateHasChanged();
        }
    }

    private Task PageChangedAsync(int page)
    {
        _page = page;
        return Task.CompletedTask;
    }

    /// <summary>Loads the rows again: the grid's loader with <see cref="Load"/>, nothing on its own with <see cref="Items"/>.</summary>
    public async Task ReloadAsync()
    {
        _loadError = null;
        if (_grid is not null && Load is not null)
        {
            await _grid.ReloadAsync();
        }

        StateHasChanged();
    }

    private async Task RefreshAsync()
    {
        await ReloadAsync();
        await OnRefresh.InvokeAsync();
    }

    private async Task RetryAsync()
    {
        await OnRetry.InvokeAsync();

        // Without the error the grid is rendered anew, and a new grid loads its first page by itself.
        _loadError = null;
    }

    private string CountText(int count) => Localize(count == 1 ? "ResourceListOneItem" : "ResourceListManyItems", count);

    private RenderFragment CreateButton(string cssClass) => builder =>
    {
        builder.OpenComponent<OmniButton>(0);
        builder.AddComponentParameter(1, nameof(OmniButton.Class), cssClass);
        builder.AddComponentParameter(2, nameof(OmniButton.Variant), OmniButtonVariant.Primary);
        builder.AddComponentParameter(3, nameof(OmniButton.Size), OmniControlSize.Small);
        builder.AddComponentParameter(4, nameof(OmniButton.Disabled), !CanCreate);
        if (!CanCreate && !string.IsNullOrWhiteSpace(CreateDisabledReason))
        {
            builder.AddAttribute(5, "title", CreateDisabledReason);
        }

        builder.AddComponentParameter(6, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, CreateAsync));
        builder.AddComponentParameter(7, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniIcon>(0);
            content.AddComponentParameter(1, nameof(OmniIcon.Name), CreateIcon);
            content.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Small);
            content.CloseComponent();
            content.OpenElement(3, "span");
            content.AddContent(4, CreateText);
            content.CloseElement();
        }));
        builder.CloseComponent();
    };

    private Task CreateAsync() => CanCreate ? OnCreate.InvokeAsync() : Task.CompletedTask;

    private RenderFragment EmptyState => builder =>
    {
        builder.OpenComponent<OmniEmptyState>(0);
        builder.AddComponentParameter(1, nameof(OmniEmptyState.Class), "omni-resource-list__empty");
        builder.AddComponentParameter(2, nameof(OmniEmptyState.Title), string.IsNullOrWhiteSpace(EmptyTitle) ? Localize("ResourceListEmpty") : EmptyTitle);
        builder.AddComponentParameter(3, nameof(OmniEmptyState.Description), EmptyDescription);
        if (EmptyIcon is not null)
        {
            builder.AddComponentParameter(4, nameof(OmniEmptyState.Icon), EmptyIcon);
        }

        if (HasCreate && CanCreate)
        {
            builder.AddComponentParameter(5, nameof(OmniEmptyState.Actions), CreateButton("omni-resource-list__empty-create"));
        }

        builder.CloseComponent();
    };

    private RenderFragment NoMatchState => builder =>
    {
        builder.OpenComponent<OmniEmptyState>(0);
        builder.AddComponentParameter(1, nameof(OmniEmptyState.Class), "omni-resource-list__no-match");
        builder.AddComponentParameter(2, nameof(OmniEmptyState.Title), Localize("ResourceListNoMatch"));
        builder.AddComponentParameter(3, nameof(OmniEmptyState.Description), Localize("ResourceListNoMatchDescription", _searchText.Trim()));
        builder.AddComponentParameter(4, nameof(OmniEmptyState.Icon), (RenderFragment)(icon =>
        {
            icon.OpenComponent<OmniIcon>(0);
            icon.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Search);
            icon.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Large);
            icon.CloseComponent();
        }));
        builder.AddComponentParameter(5, nameof(OmniEmptyState.Actions), (RenderFragment)(actions =>
        {
            actions.OpenComponent<OmniButton>(0);
            actions.AddComponentParameter(1, nameof(OmniButton.Class), "omni-resource-list__empty-clear");
            actions.AddComponentParameter(2, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
            actions.AddComponentParameter(3, nameof(OmniButton.Size), OmniControlSize.Small);
            actions.AddComponentParameter(4, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, ClearSearchAsync));
            actions.AddComponentParameter(5, nameof(OmniButton.ChildContent), (RenderFragment)(content => content.AddContent(0, Localize("ResourceListClearSearch"))));
            actions.CloseComponent();
        }));
        builder.CloseComponent();
    };

    public void Dispose()
    {
        _searchDelay?.Cancel();
        _searchDelay?.Dispose();
        GC.SuppressFinalize(this);
    }
}
