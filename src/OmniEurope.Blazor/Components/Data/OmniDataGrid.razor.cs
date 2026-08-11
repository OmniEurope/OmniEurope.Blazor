namespace OmniEurope.Blazor.Components;

public partial class OmniDataGrid<TItem>
{
    private readonly List<OmniDataGridColumnDefinition<TItem>> _columns = [];
    private readonly Dictionary<string, string> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OmniDataGridColumnWidth> _columnWidths = new(StringComparer.Ordinal);
    private OmniDataGridContext<TItem> _context = default!;
    private readonly GridRemoteState<TItem> _remote = new();
    private readonly List<OmniDataGridSort> _sorts = [];
    private GridProjectionResult<TItem>? _localProjection;
    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> _visibleColumns = Array.Empty<OmniDataGridColumnDefinition<TItem>>();
    private HashSet<object> _selectedKeyIndex = [];
    private HashSet<object> _expandedKeyIndex = [];
    private Dictionary<object, int> _groupCountIndex = [];
    private bool _hasEditing;
    private int _columnSpan;
    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? _observedLoader;
    private static readonly object NullGroupKey = new();

    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

    [Parameter]
    public Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? Load { get; set; }

    [Parameter]
    public RenderFragment? Columns { get; set; }

    [Parameter]
    public string? Caption { get; set; }

    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public OmniDataGridSelectionMode SelectionMode { get; set; }

    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    [Parameter]
    public IReadOnlyList<object> SelectedKeys { get; set; } = Array.Empty<object>();

    [Parameter]
    public EventCallback<IReadOnlyList<object>> SelectedKeysChanged { get; set; }

    [Parameter]
    public Func<TItem, bool>? IsEditing { get; set; }

    [Parameter]
    public EventCallback<TItem> EditRequested { get; set; }

    [Parameter]
    public EventCallback<TItem> RowUpdated { get; set; }

    [Parameter]
    public EventCallback<TItem> EditCancelled { get; set; }

    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    [Parameter]
    public IReadOnlyList<object> ExpandedKeys { get; set; } = Array.Empty<object>();

    [Parameter]
    public EventCallback<IReadOnlyList<object>> ExpandedKeysChanged { get; set; }

    [Parameter]
    public Func<TItem, object?>? GroupBy { get; set; }

    [Parameter]
    public Func<object?, int, string>? GroupLabel { get; set; }

    [Parameter]
    public EventCallback<OmniDataGridColumnWidthChange> ColumnWidthChanged { get; set; }

    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> VisibleColumns => _visibleColumns;
    private IReadOnlyList<TItem> VisibleItems => Load is null ? LocalView.Items : _remote.Items;
    private int TotalCount => Load is null ? LocalView.TotalCount : _remote.TotalCount;
    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
    private bool HasEditing => _hasEditing;
    private int ColumnSpan => _columnSpan;

    protected override void OnInitialized()
    {
        _context = new OmniDataGridContext<TItem> { Register = RegisterColumn, Unregister = UnregisterColumn };
    }

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        InvalidateLocalProjection();
        if (Load is null)
        {
            if (_observedLoader is not null)
            {
                _remote.Reset();
                _observedLoader = null;
            }
        }
        else if ((!_remote.HasLoaded || !ReferenceEquals(_observedLoader, Load)) && !_remote.Loading && _remote.Error is null)
        {
            _observedLoader = Load;
            await ReloadAsync();
        }

        RebuildRenderSnapshot();
    }

    private void RegisterColumn(OmniDataGridColumnDefinition<TItem> definition)
    {
        var index = _columns.FindIndex(column => column.Key == definition.Key);
        if (index >= 0)
        {
            _columns[index] = definition;
        }
        else
        {
            _columns.Add(definition);
        }
        InvalidateLocalProjection();
        RebuildRenderSnapshot();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UnregisterColumn(string key)
    {
        _columns.RemoveAll(column => column.Key == key);
        _filters.Remove(key);
        _columnWidths.Remove(key);
        _sorts.RemoveAll(sort => sort.Key == key);
        InvalidateLocalProjection();
        RebuildRenderSnapshot();
        _ = InvokeAsync(async () =>
        {
            if (Load is not null)
            {
                await ReloadAsync();
            }

            StateHasChanged();
        });
    }

    private GridProjectionResult<TItem> LocalView => _localProjection ??=
        GridProjection<TItem>.Create(Items, _columns, _filters, _sorts, Page, PageSize);
    private void InvalidateLocalProjection() => _localProjection = null;
    private object ItemKey(TItem item) => KeySelector?.Invoke(item) ?? item!;
    private bool IsSelected(object key) => _selectedKeyIndex.Contains(key);
    private bool IsExpanded(object key) => _expandedKeyIndex.Contains(key);
    private string GroupText(object? key, int count) => GroupLabel?.Invoke(key, count) ?? $"{key} ({count})";
    private int GroupCount(object? key) => _groupCountIndex.GetValueOrDefault(key ?? NullGroupKey);

    private void RebuildRenderSnapshot()
    {
        _visibleColumns = _columns.Where(column => column.Visible).ToArray();
        _hasEditing = _visibleColumns.Any(column => column.EditTemplate is not null);
        _columnSpan = _visibleColumns.Count + (SelectionMode == OmniDataGridSelectionMode.None ? 0 : 1) + (_hasEditing ? 1 : 0) + (DetailTemplate is null ? 0 : 1);
        _selectedKeyIndex = SelectedKeys.ToHashSet();
        _expandedKeyIndex = ExpandedKeys.ToHashSet();
        _groupCountIndex = [];
        if (GroupBy is null)
        {
            return;
        }

        foreach (var item in VisibleItems)
        {
            var key = GroupBy(item) ?? NullGroupKey;
            _groupCountIndex[key] = _groupCountIndex.GetValueOrDefault(key) + 1;
        }
    }

    private async Task ToggleSelectionAsync(object key)
    {
        var keys = SelectedKeys.ToList();
        if (keys.Contains(key))
        {
            keys.Remove(key);
        }
        else
        {
            if (SelectionMode == OmniDataGridSelectionMode.Single)
            {
                keys.Clear();
            }
            keys.Add(key);
        }
        await SelectedKeysChanged.InvokeAsync(keys);
        _selectedKeyIndex = keys.ToHashSet();
    }

    private Task ToggleExpandedAsync(object key)
    {
        var keys = ExpandedKeys.ToList();
        if (!keys.Remove(key)) keys.Add(key);
        _expandedKeyIndex = keys.ToHashSet();
        return ExpandedKeysChanged.InvokeAsync(keys);
    }

    private async Task SortAsync(string key, bool append)
    {
        var existing = _sorts.FindIndex(sort => sort.Key == key);
        var descending = existing >= 0 && !_sorts[existing].Descending;
        if (!append) _sorts.Clear();
        else if (existing >= 0) _sorts.RemoveAt(existing);
        _sorts.Add(new OmniDataGridSort(key, descending));
        if (Page != 1)
        {
            Page = 1;
            await PageChanged.InvokeAsync(1);
        }
        InvalidateLocalProjection();
        if (Load is not null) await ReloadAsync();
        RebuildRenderSnapshot();
    }

    private async Task FilterAsync(string key, string value)
    {
        _filters[key] = value;
        if (Page != 1)
        {
            Page = 1;
            await PageChanged.InvokeAsync(1);
        }
        InvalidateLocalProjection();
        if (Load is not null) await ReloadAsync();
        RebuildRenderSnapshot();
    }

    private async Task ChangePageAsync(int page)
    {
        Page = page;
        InvalidateLocalProjection();
        await PageChanged.InvokeAsync(page);
        if (Load is not null) await ReloadAsync();
        RebuildRenderSnapshot();
    }

    public async Task ReloadAsync()
    {
        if (Load is null) return;
        var columnIndex = _columns.ToDictionary(column => column.Key, StringComparer.Ordinal);
        var filters = _filters.Where(pair => !string.IsNullOrWhiteSpace(pair.Value) && columnIndex.ContainsKey(pair.Key))
            .Select(pair => new OmniDataGridFilter(pair.Key, columnIndex[pair.Key].FilterOperator, pair.Value))
            .ToArray();
        var sorts = _sorts.Where(sort => columnIndex.ContainsKey(sort.Key)).ToArray();
        await _remote.LoadAsync(token => Load(new OmniDataGridLoadRequest(Page, PageSize, sorts, filters, token)));
        RebuildRenderSnapshot();
    }

    private string? AriaSort(OmniDataGridColumnDefinition<TItem> column)
    {
        var sort = _sorts.FirstOrDefault(candidate => candidate.Key == column.Key);
        return sort is null ? null : sort.Descending ? "descending" : "ascending";
    }
    private string FilterId(OmniDataGridColumnDefinition<TItem> column) => $"{Id ?? "omni-grid"}-filter-{column.Key}";
    private string FilterValue(string key) => _filters.GetValueOrDefault(key, string.Empty);
    private string ColumnClass(OmniDataGridColumnDefinition<TItem> column)
    {
        var width = _columnWidths.GetValueOrDefault(column.Key, column.Width);
        return $"omni-data-grid__column--{width.ToString().ToLowerInvariant()}";
    }

    private Task ResizeColumnAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        var current = _columnWidths.GetValueOrDefault(column.Key, column.Width);
        var next = current == OmniDataGridColumnWidth.Wide ? OmniDataGridColumnWidth.Auto : (OmniDataGridColumnWidth)((int)current + 1);
        _columnWidths[column.Key] = next;
        return ColumnWidthChanged.InvokeAsync(new OmniDataGridColumnWidthChange(column.Key, next));
    }
    private RenderFragment Cell(OmniDataGridColumnDefinition<TItem> column, TItem item, bool editing) => builder =>
    {
        if (editing && column.EditTemplate is not null) builder.AddContent(0, column.EditTemplate(item));
        else if (column.Template is not null) builder.AddContent(1, column.Template(item));
        else builder.AddContent(2, column.Format?.Invoke(column.Value(item)) ?? column.Value(item)?.ToString());
    };

    public ValueTask DisposeAsync() => _remote.DisposeAsync();
}
