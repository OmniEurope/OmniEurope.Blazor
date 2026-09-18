using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniDataGrid<TItem>
{
    private const string GridModulePath = "./_content/OmniEurope.Blazor/omni-grid.js";

    private readonly List<OmniDataGridColumnDefinition<TItem>> _columns = [];
    private readonly Dictionary<string, GridColumnFilter> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GridColumnFilter> _draftFilters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _columnWidths = new(StringComparer.Ordinal);
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);
    private readonly HashSet<object> _editedKeys = [];
    private OmniDataGridContext<TItem> _context = default!;
    private readonly GridRemoteState<TItem> _remote = new();
    private readonly GridVirtualWindow _window = new();
    private readonly GridVirtualDataSource<TItem> _virtualSource = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly List<OmniDataGridSort> _sorts = [];
    private GridProjectionResult<TItem>? _localProjection;
    private IReadOnlyList<TItem>? _virtualLocalItems;
    private IReadOnlyList<GridRenderRow<TItem>>? _slots;
    private bool _slotsRebuilt;
    private int _slotShape;
    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> _visibleColumns = Array.Empty<OmniDataGridColumnDefinition<TItem>>();
    private OmniDataGridColumnDefinition<TItem>? _implicitColumn;
    private HashSet<object> _selectedKeyIndex = [];
    private HashSet<object> _expandedKeyIndex = [];
    private bool _hasEditing;
    private readonly HashSet<string> _initialSortKeys = new(StringComparer.Ordinal);
    private int _columnSpan;
    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? _observedLoader;
    private bool _externalRequested;
    private static readonly object NullGroupKey = new();

    private ElementReference _viewport;
    private IJSObjectReference? _gridModule;
    private DotNetObjectReference<OmniDataGrid<TItem>>? _selfReference;
    private GridVirtualRange _range;
    private double _scrollTop;
    private double _viewportHeight;
    private bool _virtualAttached;
    private bool _virtualBootstrapped;
    private bool _resizeAttached;
    private bool _filterMenuAttached;
    private bool _disposeRequested;
    private string? _appliedHeight;
    private string? _appliedMaxHeight;
    private string? _appliedColumnLayout;
    private double? _appliedRowHeight;
    private IOmniDataGridStateStore? _fallbackStateStore;

    private IOmniDataGridStateStore EffectiveStateStore =>
        StateStore ?? InjectedStateStore ?? (_fallbackStateStore ??= new OmniLocalStorageDataGridStateStore(JavaScript));

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

    [Parameter]
    public Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? Load { get; set; }

    /// <summary>Requests data from a parent that supplies <see cref="Items"/> and <see cref="Count"/> on its next render.</summary>
    [Parameter]
    public EventCallback<OmniDataGridLoadRequest> LoadRequested { get; set; }

    [Parameter]
    public RenderFragment? Columns { get; set; }

    [Parameter]
    public string? Caption { get; set; }

    // ---- paging -----------------------------------------------------------------------------

    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

    [Parameter]
    public string? PageSizeText { get; set; }

    [Parameter]
    public bool AllowPaging { get; set; } = true;

    [Parameter]
    public OmniDataGridPagerPosition PagerPosition { get; set; } = OmniDataGridPagerPosition.Bottom;

    [Parameter]
    public OmniJustification PagerHorizontalAlign { get; set; } = OmniJustification.Start;

    [Parameter]
    public bool ShowPagingSummary { get; set; }

    /// <summary>Composite format receiving the first row, the last row and the total row count.</summary>
    [Parameter]
    public string? PagingSummaryFormat { get; set; }

    [Parameter]
    public string? FirstPageAriaLabel { get; set; }

    [Parameter]
    public string? FirstPageTitle { get; set; }

    [Parameter]
    public string? LastPageAriaLabel { get; set; }

    [Parameter]
    public string? LastPageTitle { get; set; }

    [Parameter]
    public string? PrevPageAriaLabel { get; set; }

    [Parameter]
    public string? PrevPageTitle { get; set; }

    [Parameter]
    public string? NextPageAriaLabel { get; set; }

    [Parameter]
    public string? NextPageTitle { get; set; }

    /// <summary>Composite format receiving the page number, used on the numbered page buttons.</summary>
    [Parameter]
    public string? PageTitleFormat { get; set; }

    /// <summary>Composite format receiving the page number, used as the accessible name.</summary>
    [Parameter]
    public string? PageAriaLabelFormat { get; set; }

    /// <summary>Numbered page buttons rendered around the current page. Zero keeps the compact status.</summary>
    [Parameter]
    public int NumericPageCount { get; set; }

    /// <summary>
    /// Keep the pager on screen even when everything fits on one page. Without it the grid grows
    /// and shrinks by the height of the pager as rows are filtered, which moves the content under
    /// the pointer.
    /// </summary>
    [Parameter]
    public bool AlwaysShowPager { get; set; }

    /// <summary>Total row count when the host already knows it, for instance from a count query.</summary>
    [Parameter]
    public int? Count { get; set; }

    // ---- selection --------------------------------------------------------------------------

    [Parameter]
    public OmniDataGridSelectionMode SelectionMode { get; set; }

    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Property path identifying a row when no <see cref="KeySelector"/> is supplied.</summary>
    [Parameter]
    public string? KeyProperty { get; set; }

    [Parameter]
    public IReadOnlyList<object> SelectedKeys { get; set; } = Array.Empty<object>();

    [Parameter]
    public EventCallback<IReadOnlyList<object>> SelectedKeysChanged { get; set; }

    /// <summary>Currently selected rows. Kept in step with <see cref="SelectedKeys"/>.</summary>
    [Parameter]
    public IReadOnlyList<TItem> Value { get; set; } = Array.Empty<TItem>();

    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> ValueChanged { get; set; }

    [Parameter]
    public bool AllowRowSelectOnRowClick { get; set; }

    [Parameter]
    public EventCallback<TItem> RowSelect { get; set; }

    [Parameter]
    public EventCallback<TItem> RowClick { get; set; }

    [Parameter]
    public EventCallback<TItem> RowDoubleClick { get; set; }

    /// <summary>Called for every rendered row so the host can add a class or veto its controls.</summary>
    [Parameter]
    public Action<OmniDataGridRowRenderArgs<TItem>>? RowRender { get; set; }

    // ---- editing ----------------------------------------------------------------------------

    /// <summary>Overrides the grid's own edit tracking when the host owns the edit state.</summary>
    [Parameter]
    public Func<TItem, bool>? IsEditing { get; set; }

    [Parameter]
    public OmniDataGridEditMode EditMode { get; set; } = OmniDataGridEditMode.Single;

    [Parameter]
    public EventCallback<TItem> EditRequested { get; set; }

    [Parameter]
    public EventCallback<TItem> RowUpdated { get; set; }

    [Parameter]
    public EventCallback<TItem> EditCancelled { get; set; }

    // ---- detail rows ------------------------------------------------------------------------

    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    [Parameter]
    public IReadOnlyList<object> ExpandedKeys { get; set; } = Array.Empty<object>();

    [Parameter]
    public EventCallback<IReadOnlyList<object>> ExpandedKeysChanged { get; set; }

    [Parameter]
    public OmniDataGridExpandMode ExpandMode { get; set; } = OmniDataGridExpandMode.Multiple;

    [Parameter]
    public bool ShowExpandColumn { get; set; } = true;

    /// <summary>
    /// Whether the grid appends its own edit, save and cancel column as soon as a column carries an
    /// <see cref="OmniDataGridColumn{TItem}.EditTemplate"/>. Set to false when the consumer places
    /// those controls in a column of its own: otherwise every row shows a second, unlabelled set
    /// beside them, and there was no way to take it away.
    /// </summary>
    [Parameter]
    public bool ShowEditColumn { get; set; } = true;

    [Parameter]
    public bool ShowExpandAll { get; set; }

    [Parameter]
    public string? ExpandChildItemAriaLabel { get; set; }

    [Parameter]
    public EventCallback<TItem> RowExpand { get; set; }

    [Parameter]
    public EventCallback<TItem> RowCollapse { get; set; }

    // ---- grouping ---------------------------------------------------------------------------

    [Parameter]
    public bool AllowGrouping { get; set; }

    [Parameter]
    public bool ShowGroupPanel { get; set; }

    [Parameter]
    public IReadOnlyList<OmniDataGridGroup> Groups { get; set; } = Array.Empty<OmniDataGridGroup>();

    [Parameter]
    public EventCallback<IReadOnlyList<OmniDataGridGroup>> GroupsChanged { get; set; }

    [Parameter]
    public bool AllGroupsExpanded { get; set; } = true;

    /// <summary>Legacy single-level grouping by delegate. Ignored when <see cref="Groups"/> is used.</summary>
    [Parameter]
    public Func<TItem, object?>? GroupBy { get; set; }

    [Parameter]
    public Func<object?, int, string>? GroupLabel { get; set; }

    // ---- sorting and filtering ----------------------------------------------------------------

    [Parameter]
    public bool AllowSorting { get; set; } = true;

    [Parameter]
    public bool AllowFiltering { get; set; } = true;

    [Parameter]
    public OmniDataGridFilterMode FilterMode { get; set; } = OmniDataGridFilterMode.Simple;

    /// <summary>
    /// Adds a per-column filter popover anchored to its header, alongside the inline filter row.
    /// Useful when the inline row is kept for its "always visible" quick-filter role but a header
    /// entry point is also wanted; the popover offers only the value control (no dual-condition
    /// editing, which stays inline-only in <see cref="OmniDataGridFilterMode.Advanced"/>).
    /// </summary>
    [Parameter]
    public bool ShowHeaderFilterMenu { get; set; }

    /// <summary>
    /// Closes a header filter menu as soon as a value is picked in it. A click outside the menu, or
    /// the Escape key, always closes it whatever this is set to.
    /// </summary>
    [Parameter]
    public bool HideFilterMenuOnSelect { get; set; }

    [Parameter]
    public OmniDataGridFilterCaseSensitivity FilterCaseSensitivity { get; set; }

    /// <summary>
    /// Compares filters with accents stripped from both sides, so a search for "epee" matches the
    /// accented spelling of the same word. A host that filters its own rows behind <see cref="Load"/>
    /// applies the same rule through <see cref="OmniDataGridFilterText.Normalize"/>.
    /// </summary>
    [Parameter]
    public bool IgnoreDiacritics { get; set; }

    [Parameter]
    public string? FilterText { get; set; }

    [Parameter]
    public string? ApplyFilterText { get; set; }

    [Parameter]
    public string? ClearFilterText { get; set; }

    [Parameter]
    public string? ContainsText { get; set; }

    [Parameter]
    public string? DoesNotContainText { get; set; }

    [Parameter]
    public string? EqualsText { get; set; }

    [Parameter]
    public string? NotEqualsText { get; set; }

    [Parameter]
    public string? StartsWithText { get; set; }

    [Parameter]
    public string? EndsWithText { get; set; }

    [Parameter]
    public string? AndOperatorText { get; set; }

    [Parameter]
    public string? OrOperatorText { get; set; }

    // ---- presentation -------------------------------------------------------------------------

    [Parameter]
    public bool AllowColumnResize { get; set; } = true;

    [Parameter]
    public EventCallback<OmniDataGridColumnWidthChange> ColumnWidthChanged { get; set; }

    /// <summary>Observed alias of <see cref="ColumnWidthChanged"/>.</summary>
    [Parameter]
    public EventCallback<OmniDataGridColumnWidthChange> ColumnResized { get; set; }

    /// <summary>Default CSS width applied to columns that do not declare one.</summary>
    [Parameter]
    public string? ColumnWidth { get; set; }

    [Parameter]
    public bool AllowAlternatingRows { get; set; }

    /// <summary>Tints a whole column, header included, while it carries a sort or a filter.</summary>
    [Parameter]
    public bool HighlightActiveColumn { get; set; }

    /// <summary>Tints the row under the pointer.</summary>
    [Parameter]
    public bool HighlightRowOnHover { get; set; }

    [Parameter]
    public OmniDataGridLines GridLines { get; set; } = OmniDataGridLines.Default;

    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>Stacks each row into a labelled card once the viewport is too narrow for a table.</summary>
    [Parameter]
    public bool Responsive { get; set; }

    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>
    /// Rendered in the empty-state cell instead of <see cref="EmptyText"/>, for an empty state with an
    /// icon, a description or an action. Left null, the text is shown exactly as before.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Height of the scrolling table as a CSS length, for example <c>600px</c>, <c>50vh</c> or
    /// <c>100%</c>. Left unset the table grows with its content, or fills its parent while
    /// virtualizing.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>
    /// Shows a button that switches this grid, and only this grid, between the light and dark
    /// palettes. The surrounding page is untouched.
    /// </summary>
    [Parameter]
    public bool ShowThemeToggle { get; set; }

    /// <summary>Palette the grid starts on when <see cref="ShowThemeToggle"/> is used.</summary>
    [Parameter]
    public bool DarkTheme { get; set; }

    private bool? _darkThemeOverride;

    private bool IsDarkTheme => _darkThemeOverride ?? DarkTheme;

    private string? GridTheme => ShowThemeToggle || DarkTheme
        ? IsDarkTheme ? "dark" : "light"
        : null;

    private void ToggleTheme() => _darkThemeOverride = !IsDarkTheme;

    /// <summary>
    /// Opt-in: the grid stretches to whatever height its parent leaves free instead of using its
    /// own fixed viewport height, never dropping below <see cref="MinHeight"/>. The parent must be
    /// a sized flex or grid container for there to be a remainder to take. Off by default, so an
    /// existing grid keeps its current height.
    /// </summary>
    [Parameter]
    public bool FillAvailableHeight { get; set; }

    /// <summary>
    /// Floor of the scrolling area as a CSS length while <see cref="FillAvailableHeight"/> is on.
    /// </summary>
    [Parameter]
    public string MinHeight { get; set; } = "22.5rem";

    /// <summary>
    /// Ceiling of the scrolling area as a CSS length, for example <c>24rem</c> or <c>50vh</c>. Set,
    /// the table takes the height of its content up to this length, then scrolls: a virtualized grid
    /// of three rows is three rows tall instead of the fixed virtual height, and a long one still
    /// scrolls and renders only its window. Pushed as a CSS custom property by the grid script, never
    /// as a style attribute. Ignored when <see cref="Height"/> or <see cref="FillAvailableHeight"/>
    /// already sizes the table. Unset by default: the table is sized as before.
    /// </summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    // ---- virtualization -----------------------------------------------------------------------

    /// <summary>
    /// Renders only the rows the viewport can show and scrolls over the whole row count. Paging is
    /// replaced by a continuous scrollbar; grouping and detail rows are not supported in this mode.
    /// </summary>
    [Parameter]
    public bool AllowVirtualization { get; set; }

    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 4;

    /// <summary>Starting height assumed for a row that has not been measured yet, in pixels.</summary>
    [Parameter]
    public double EstimatedRowHeight { get; set; } = 40d;

    /// <summary>Fixed row height in pixels. When set, rows are never measured and every row uses it.</summary>
    [Parameter]
    public double? RowHeight { get; set; }

    /// <summary>
    /// Applies <see cref="RowHeight"/> as a real fixed CSS row height (uniform rows, overflow
    /// clipped) instead of only feeding the virtualization scroll math. Has no effect without a
    /// <see cref="RowHeight"/> value.
    /// </summary>
    [Parameter]
    public bool FixedRowHeight { get; set; }

    /// <summary>
    /// What a column title does when it is wider than its column: run onto a second line, growing
    /// the header band, or be cut with an ellipsis and keep it one line tall.
    /// </summary>
    [Parameter]
    public OmniDataGridHeaderWrap HeaderWrap { get; set; } = OmniDataGridHeaderWrap.Wrap;

    /// <summary>Rows fetched per remote request while virtualizing. Defaults to <see cref="PageSize"/>.</summary>
    [Parameter]
    public int VirtualBlockSize { get; set; }

    // ---- state persistence --------------------------------------------------------------------

    /// <summary>
    /// Opaque key this grid's filters, sorts and column widths are saved under. Persistence is
    /// inert until this is set: that is the option's activation switch.
    /// </summary>
    [Parameter]
    public string? StateKey { get; set; }

    /// <summary>
    /// Overrides the store this grid would otherwise resolve from DI (see
    /// <c>AddOmniEuropeBlazor</c>, which registers the built-in localStorage one) or, absent any
    /// registration, construct itself.
    /// </summary>
    [Parameter]
    public IOmniDataGridStateStore? StateStore { get; set; }

    [Inject]
    private IOmniDataGridStateStore? InjectedStateStore { get; set; }

    // ---- derived state ------------------------------------------------------------------------

    private bool Virtualized => AllowVirtualization;
    private bool ExternalData => LoadRequested.HasDelegate;

    /// <summary>
    /// A local virtualized grid whose body is not one row per item: group header rows and detail rows
    /// sit between the items. The virtual window then indexes "slots", an item row together with the
    /// group headers opening above it and its detail row, and measures each slot as a whole.
    /// </summary>
    private bool StructuredVirtual => Virtualized && Load is null
        && (GroupBy is not null || DetailTemplate is not null || ActiveGroups.Count > 0);

    private IReadOnlyList<GridRenderRow<TItem>> Slots => _slots ??= BuildSlots();

    // Rendered only for slotted rows: a null value leaves the attribute out of every other grid.
    private static string? SlotAttribute(GridRenderRow<TItem> row) =>
        row.Slot >= 0 ? row.Slot.ToString(CultureInfo.InvariantCulture) : null;
    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> VisibleColumns => _visibleColumns;

    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> EffectiveColumns =>
        _columns.Count == 0 ? [ImplicitColumn] : _columns;

    private OmniDataGridColumnDefinition<TItem> ImplicitColumn => _implicitColumn ??= new OmniDataGridColumnDefinition<TItem>
    {
        Key = "value",
        Title = Localize("GridValueColumn"),
        Value = item => item
    };

    private IReadOnlyList<TItem> VirtualLocalItems => _virtualLocalItems ??=
        GridProjection<TItem>.Create(Items, EffectiveColumns, _filters, _sorts, FilterCaseSensitivity, IgnoreDiacritics, 1, int.MaxValue).Items;

    private GridProjectionResult<TItem> LocalView => _localProjection ??= GridProjection<TItem>.Create(
        Items, EffectiveColumns, _filters, _sorts, FilterCaseSensitivity, IgnoreDiacritics, Page, AllowPaging ? PageSize : int.MaxValue);

    private IReadOnlyList<TItem> VisibleItems => Virtualized
        ? Array.Empty<TItem>()
        : ExternalData ? Items : Load is null ? LocalView.Items : _remote.Items;

    private int TotalCount => Count
        ?? (Virtualized
            ? Load is null ? VirtualLocalItems.Count : _virtualSource.TotalCount
            : ExternalData ? Items.Count : Load is null ? LocalView.TotalCount : _remote.TotalCount);

    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));

    /// <summary>
    /// The page actually shown. The local projection already clamps a page past the end to the last
    /// one; the pager and the summary read the same value, instead of announcing "page 5 of 1" over
    /// the rows of page 1 once the item list shrinks.
    /// </summary>
    private int EffectivePage => Math.Clamp(Page, 1, PageCount);
    private bool HasEditing => _hasEditing;
    private int ColumnSpan => _columnSpan;
    private bool Loading => IsLoading || (!ExternalData && _remote.Loading) || (Virtualized && _virtualSource.Loading && _virtualSource.CachedItemCount == 0);
    private Exception? Failure => ExternalData ? null : Virtualized ? _virtualSource.Error : _remote.Error;
    private bool ShowPager => AllowPaging && !Virtualized && (PageCount > 1 || AlwaysShowPager);
    private bool ShowPagerTop => ShowPager && PagerPosition is OmniDataGridPagerPosition.Top or OmniDataGridPagerPosition.TopAndBottom;
    private bool ShowPagerBottom => ShowPager && PagerPosition is OmniDataGridPagerPosition.Bottom or OmniDataGridPagerPosition.TopAndBottom;
    private int BlockSize => VirtualBlockSize > 0 ? VirtualBlockSize : Math.Max(1, PageSize);
    private string EmptyMessage => string.IsNullOrWhiteSpace(EmptyText) ? Localize("GridEmpty") : EmptyText;
    private bool RowsAreInteractive => AllowRowSelectOnRowClick || RowClick.HasDelegate || RowDoubleClick.HasDelegate;
    private bool ShowDetailColumn => DetailTemplate is not null && ShowExpandColumn;
    private bool ShowLoadingRow => Virtualized ? Loading && TotalCount == 0 : Loading;
    private bool IsEmpty => Virtualized ? TotalCount == 0 : VisibleItems.Count == 0;
    private GridVirtualRange Range => _range;
    private bool UsesAdvancedFilter => AllowFiltering && FilterMode == OmniDataGridFilterMode.Advanced;
    private bool ShowsOperatorSelector => AllowFiltering && FilterMode != OmniDataGridFilterMode.Simple;
    private IReadOnlyList<OmniDataGridGroup> ActiveGroups => AllowGrouping ? Groups : Array.Empty<OmniDataGridGroup>();
    /// <summary>Where the footer row goes. See <see cref="OmniDataGridFooterPosition"/>.</summary>
    [Parameter]
    public OmniDataGridFooterPosition FooterPosition { get; set; }

    private bool HasFooter => VisibleColumns.Any(column => column.FooterTemplate is not null);

    private bool HasTopFooter => HasFooter && FooterPosition == OmniDataGridFooterPosition.Top;

    private bool HasBottomFooter => HasFooter && FooterPosition == OmniDataGridFooterPosition.Bottom;

    protected override void OnInitialized()
    {
        _context = new OmniDataGridContext<TItem> { Register = RegisterColumn, Unregister = UnregisterColumn };
    }

    /// <summary>
    /// Runs, and is awaited, before the grid's first render, so any restored filters/sorts/column
    /// widths are already in <see cref="_filters"/>/<see cref="_sorts"/>/<see cref="_columnWidths"/>
    /// by the time the child <see cref="OmniDataGridColumn{TItem}"/> content first registers.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        if (StateKey is { } key)
        {
            await LoadPersistedStateAsync(key);
        }
    }

    private sealed record OmniDataGridPersistedState(
        Dictionary<string, GridColumnFilter> Filters,
        List<OmniDataGridSort> Sorts,
        Dictionary<string, string?> ColumnWidths);

    private async Task LoadPersistedStateAsync(string key)
    {
        string? json;
        try
        {
            json = await EffectiveStateStore.LoadAsync(key);
        }
        catch (JSDisconnectedException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        OmniDataGridPersistedState? state;
        try
        {
            state = JsonSerializer.Deserialize<OmniDataGridPersistedState>(json);
        }
        catch (JsonException)
        {
            // A stale or hand-edited entry from a previous grid shape must not block the page.
            return;
        }

        if (state is null)
        {
            return;
        }

        _filters.Clear();
        foreach (var (columnKey, filter) in state.Filters)
        {
            _filters[columnKey] = filter;
        }

        _sorts.Clear();
        _sorts.AddRange(state.Sorts);

        _columnWidths.Clear();
        foreach (var (columnKey, width) in state.ColumnWidths)
        {
            _columnWidths[columnKey] = width;
        }

        InvalidateLocalProjection();
    }

    private async Task PersistStateAsync()
    {
        if (StateKey is not { } key)
        {
            return;
        }

        var state = new OmniDataGridPersistedState(
            new Dictionary<string, GridColumnFilter>(_filters, StringComparer.Ordinal),
            [.. _sorts],
            new Dictionary<string, string?>(_columnWidths, StringComparer.Ordinal));
        var json = JsonSerializer.Serialize(state);
        try
        {
            await EffectiveStateStore.SaveAsync(key, json);
        }
        catch (JSDisconnectedException)
        {
            // Best-effort: a navigation racing the save must not surface as an error.
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        if (Load is not null && ExternalData)
        {
            throw new InvalidOperationException("OmniDataGrid accepts either Load or LoadRequested, not both.");
        }
        if (Virtualized && ExternalData)
        {
            throw new InvalidOperationException("LoadRequested is a paged external-data contract and cannot be virtualized. Use Load for remote virtualization.");
        }
        if (Virtualized && Load is not null && (GroupBy is not null || DetailTemplate is not null || ActiveGroups.Count > 0))
        {
            throw new InvalidOperationException(
                "OmniDataGrid cannot virtualize a remote (Load) grid that also declares GroupBy, Groups or DetailTemplate: "
                + "groups and detail rows need the whole row set, which a remote source only loads block by block. "
                + "Use Items for a grouped or detailed virtualized grid.");
        }

        _implicitColumn = null;
        InvalidateLocalProjection();

        if (Load is null)
        {
            if (_observedLoader is not null)
            {
                _remote.Reset();
                _virtualSource.Reset();
                _observedLoader = null;
                _virtualBootstrapped = false;
            }
        }
        else
        {
            var loaderChanged = !ReferenceEquals(_observedLoader, Load);
            _observedLoader = Load;
            if (Virtualized)
            {
                if (loaderChanged)
                {
                    _virtualSource.Reset();
                    _virtualBootstrapped = false;
                }
            }
            else if ((loaderChanged || !_remote.HasLoaded) && !_remote.Loading && _remote.Error is null)
            {
                await ReloadAsync();
            }
        }

        RebuildRenderSnapshot();
        if (Virtualized)
        {
            await BootstrapVirtualizationAsync();
            return;
        }

        // A local list that shrank under the current page (rows removed, another data set) is
        // shown from its last page; the host's bound page is told, so it never disagrees with it.
        if (Load is null && !ExternalData && AllowPaging && Page > PageCount)
        {
            Page = PageCount;
            InvalidateLocalProjection();
            await PageChanged.InvokeAsync(Page);
        }
    }

    /// <summary>
    /// Applies a column's declared <c>SortOrder</c> the first time that column registers. Columns
    /// register while the child content renders, after the grid's own parameters are set, so this
    /// runs per column rather than once for the grid.
    /// </summary>
    private void ApplyInitialSort(OmniDataGridColumnDefinition<TItem> column)
    {
        if (column.SortOrder is null || !_initialSortKeys.Add(column.Key))
        {
            return;
        }

        // A sort already present for this key (typically restored by LoadPersistedStateAsync, which
        // runs before any column registers) takes precedence over the column's own default.
        if (_sorts.Any(sort => sort.Key == column.Key))
        {
            return;
        }

        _sorts.Add(new OmniDataGridSort(column.Key, column.SortOrder == OmniDataGridSortOrder.Descending)
        {
            Property = column.SortProperty ?? column.Property
        });
        InvalidateLocalProjection();
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
        ApplyInitialSort(definition);
        InvalidateLocalProjection();
        RebuildRenderSnapshot();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UnregisterColumn(string key)
    {
        _columns.RemoveAll(column => column.Key == key);
        _filters.Remove(key);
        _draftFilters.Remove(key);
        _columnWidths.Remove(key);
        _sorts.RemoveAll(sort => sort.Key == key);
        InvalidateLocalProjection();
        RebuildRenderSnapshot();
        _ = InvokeAsync(async () =>
        {
            if (Load is not null || ExternalData)
            {
                await ReloadAsync();
            }

            StateHasChanged();
        });
    }

    private void InvalidateLocalProjection()
    {
        _localProjection = null;
        _virtualLocalItems = null;
        _slots = null;
    }

    private object ItemKey(TItem item)
    {
        if (KeySelector is not null)
        {
            return KeySelector(item);
        }

        var accessor = GridPropertyAccessor.Create<TItem>(KeyProperty);
        return accessor?.Invoke(item) ?? item!;
    }

    private bool IsSelected(object key) => _selectedKeyIndex.Contains(key);
    private bool IsExpanded(object key) => _expandedKeyIndex.Contains(key);
    private bool IsRowEditing(TItem item) => IsEditing?.Invoke(item) ?? _editedKeys.Contains(ItemKey(item));

    private void RebuildRenderSnapshot()
    {
        _visibleColumns = EffectiveColumns.Where(column => column.Visible).ToArray();
        _hasEditing = ShowEditColumn && _visibleColumns.Any(column => column.EditTemplate is not null);
        _columnSpan = _visibleColumns.Count
            + (SelectionMode == OmniDataGridSelectionMode.None ? 0 : 1)
            + (_hasEditing ? 1 : 0)
            + (ShowDetailColumn ? 1 : 0);
        _selectedKeyIndex = SelectedKeys.ToHashSet();
        _expandedKeyIndex = ExpandedKeys.ToHashSet();
        _slots = null;
        if (Virtualized)
        {
            SyncVirtualWindow();
        }
    }

    // ---- virtualization -----------------------------------------------------------------------

    private void SyncVirtualWindow()
    {
        var estimate = RowHeight ?? (EstimatedRowHeight > 0d ? EstimatedRowHeight : 40d);
        if (StructuredVirtual)
        {
            var count = Slots.Count;
            _window.Configure(count, estimate);
            if (_slotsRebuilt)
            {
                // A group opened or closed, a detail row appeared: the slots moved, so the heights
                // measured for the old ones no longer describe the new ones.
                _slotsRebuilt = false;
                _window.ResetMeasurements();
            }
        }
        else
        {
            _window.Configure(TotalCount, estimate);
        }

        _range = _window.Compute(_scrollTop, _viewportHeight, VirtualizationOverscanCount);
    }

    private IReadOnlyList<GridRenderRow<TItem>> BuildSlots()
    {
        var items = VirtualLocalItems;
        var rows = ActiveGroups.Count > 0 ? GroupedRows(ActiveGroups, items) : FlatRows(items);
        var slots = rows.Select((row, slot) => row with { Slot = slot }).ToArray();

        // The projection is rebuilt on every parameter pass; the measured heights only go when the
        // shape of the body really changed (another item at a slot, a header or a detail row more or less).
        var signature = new HashCode();
        signature.Add(slots.Length);
        foreach (var slot in slots)
        {
            signature.Add(slot.Index);
            signature.Add(slot.Headers.Count);
            signature.Add(slot.ShowDetail);
        }

        var shape = signature.ToHashCode();
        _slotsRebuilt |= shape != _slotShape;
        _slotShape = shape;
        return slots;
    }

    /// <summary>Forgets the slot layout after an expansion change and recomputes the window over the new one.</summary>
    private void RefreshSlots()
    {
        _slots = null;
        if (StructuredVirtual)
        {
            SyncVirtualWindow();
        }
    }

    private bool TryGetVirtualItem(int index, out TItem item)
    {
        if (Load is null)
        {
            var items = VirtualLocalItems;
            if (index >= 0 && index < items.Count)
            {
                item = items[index];
                return true;
            }

            item = default!;
            return false;
        }

        return _virtualSource.TryGet(index, out item);
    }

    private async Task BootstrapVirtualizationAsync()
    {
        if (Load is null || _virtualBootstrapped)
        {
            return;
        }

        _virtualBootstrapped = true;
        await EnsureVirtualDataAsync();
    }

    private async Task EnsureVirtualDataAsync()
    {
        if (Load is null)
        {
            return;
        }

        var count = Math.Max(1, _range.Count);
        var changed = await _virtualSource.EnsureRangeAsync(_range.StartIndex, count, BlockSize, LoadWindowAsync);
        if (changed)
        {
            SyncVirtualWindow();
            StateHasChanged();
        }
    }

    private Task<OmniDataGridResult<TItem>> LoadWindowAsync(int skip, int take, CancellationToken token)
    {
        var page = (skip / Math.Max(1, take)) + 1;
        return Load!(new OmniDataGridLoadRequest(page, take, CurrentSorts(), CurrentFilters(), token));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            if (_disposeRequested)
            {
                return;
            }

            if (!Virtualized)
            {
                await DetachViewportAsync();
                await ApplyLayoutAsync();
                await ApplyMaxHeightAsync();
                await EnsureResizeInteropAsync();
                await EnsureFilterMenuInteropAsync();
                if (ExternalData && !_externalRequested)
                {
                    await RequestExternalDataAsync();
                }
                return;
            }

            _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
            await EnsureResizeInteropAsync();
            await EnsureFilterMenuInteropAsync();
            if (!_virtualAttached)
            {
                _selfReference ??= DotNetObjectReference.Create(this);
                await _gridModule.InvokeVoidAsync("attach", _viewport, _selfReference);
                _virtualAttached = true;
            }

            // A slot holds group headers and a detail row besides its item row, so it is measured even
            // when the item rows themselves have a fixed height.
            var snapshot = await _gridModule.InvokeAsync<GridViewportSnapshot?>("sync", _viewport, RowHeight is null || StructuredVirtual);
            var moved = ApplySnapshot(snapshot);
            var previous = _range;
            SyncVirtualWindow();
            await _gridModule.InvokeVoidAsync("applyLayout", _viewport, _range.TopSpacer, _range.BottomSpacer, Height, EffectiveMinHeight);
            _appliedHeight = HeightSignature;
            await ApplyMaxHeightAsync();
            await ApplyColumnLayoutAsync();
            await ApplyRowHeightAsync(FixedRowHeight ? RowHeight : null);
            await EnsureVirtualDataAsync();
            if (moved || previous != _range)
            {
                StateHasChanged();
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>
    /// Wires the pointer gesture of the column resize handles once, and only when at least one
    /// column can actually be resized, so a read-only grid still needs no script.
    /// </summary>
    private async Task EnsureResizeInteropAsync()
    {
        if (_resizeAttached || !VisibleColumns.Any(IsResizable))
        {
            return;
        }

        _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        _selfReference ??= DotNetObjectReference.Create(this);
        await _gridModule.InvokeVoidAsync("attachResize", _viewport, _selfReference, MinimumColumnWidth);
        _resizeAttached = true;
    }

    /// <summary>
    /// Closes any open header filter popover after a suggestion was chosen in one of them, when the
    /// grid was told to hide the menu on select.
    /// </summary>
    private async Task CloseFilterMenusAsync()
    {
        if (!HideFilterMenuOnSelect || !_filterMenuAttached || _gridModule is null)
        {
            return;
        }

        await _gridModule.InvokeVoidAsync("closeFilterMenus", _viewport);
    }

    /// <summary>
    /// Wires the popovers once: the header filter menus, the advanced filter panels and the folded
    /// checkable lists. The script closes them on an outside click or Escape and places their panel
    /// over the page, since the scrolling viewport would otherwise clip it; the text filter with
    /// suggestions gets the same placement for its list.
    /// </summary>
    private async Task EnsureFilterMenuInteropAsync()
    {
        if (_filterMenuAttached || !HasPopovers)
        {
            return;
        }

        _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        await _gridModule.InvokeVoidAsync("attachFilterMenus", _viewport, HideFilterMenuOnSelect);
        _filterMenuAttached = true;
    }

    /// <summary>Whether any filter editor of this grid opens something over the rows.</summary>
    private bool HasPopovers => VisibleColumns.Any(column => IsFilterable(column)
        && (ShowHeaderFilterMenu
            || UsesAdvancedFilter
            || column.FilterTemplate is not null
            || column.FilterType is OmniDataGridColumnFilterType.MultiSelect or OmniDataGridColumnFilterType.Combo));

    /// <summary>Minimum viewport height pushed to CSS, only meaningful while filling.</summary>
    private string? EffectiveMinHeight => FillAvailableHeight && !string.IsNullOrWhiteSpace(MinHeight)
        ? MinHeight
        : null;

    /// <summary>Both height inputs in one value, so a change to either re-runs the layout interop.</summary>
    private string HeightSignature => $"{Height}|{EffectiveMinHeight}";

    /// <summary>The ceiling pushed to CSS: only when no other height input already sizes the table.</summary>
    private string? EffectiveMaxHeight => string.IsNullOrWhiteSpace(MaxHeight) || Height is not null || FillAvailableHeight
        ? null
        : MaxHeight.Trim();

    /// <summary>
    /// Pushes <see cref="MaxHeight"/> on its own call, only once a ceiling was asked for, so a grid
    /// without one runs exactly the interop it ran before the parameter existed.
    /// </summary>
    private async Task ApplyMaxHeightAsync()
    {
        var maxHeight = EffectiveMaxHeight;
        if (string.Equals(maxHeight, _appliedMaxHeight, StringComparison.Ordinal))
        {
            return;
        }

        _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        await _gridModule.InvokeVoidAsync("applyMaxHeight", _viewport, maxHeight);
        _appliedMaxHeight = maxHeight;
    }

    /// <summary>Applies the table height and the column widths outside the virtualized path.</summary>
    private async Task ApplyLayoutAsync()
    {
        var signature = ColumnLayoutSignature();
        if (!RequiresLayoutInterop && _appliedHeight is null && _appliedColumnLayout is null && _appliedRowHeight is null)
        {
            return;
        }

        var rowHeight = FixedRowHeight ? RowHeight : null;
        if (HeightSignature == _appliedHeight && signature == _appliedColumnLayout && rowHeight == _appliedRowHeight)
        {
            await ApplyFrozenOffsetsAsync();
            return;
        }

        _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        await _gridModule.InvokeVoidAsync("applyLayout", _viewport, 0d, 0d, Height, EffectiveMinHeight);
        _appliedHeight = HeightSignature;
        await ApplyColumnLayoutAsync();
        await ApplyRowHeightAsync(rowHeight);
    }

    /// <summary>
    /// Re-anchors the frozen cells after a render that kept the column layout: the rows of a new
    /// page, a sort or a filter are new cells, which the offsets set on the previous ones do not
    /// reach, and a second frozen column would otherwise slide over the first.
    /// </summary>
    private async Task ApplyFrozenOffsetsAsync()
    {
        if (!HasFrozenColumns || _gridModule is null)
        {
            return;
        }

        await _gridModule.InvokeVoidAsync("applyFrozen", _viewport);
    }

    private async Task ApplyRowHeightAsync(double? rowHeight)
    {
        if (rowHeight == _appliedRowHeight)
        {
            return;
        }

        _appliedRowHeight = rowHeight;
        _gridModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", GridModulePath);
        await _gridModule.InvokeVoidAsync("applyRowHeight", _viewport, rowHeight);
    }

    /// <summary>
    /// A plain grid with no explicit height and no column sizing needs no script at all, so the
    /// module is only imported once something actually has to be measured or positioned.
    /// </summary>
    private bool RequiresLayoutInterop => Height is not null
        || FillAvailableHeight
        || ColumnWidth is not null
        || _columnWidths.Count > 0
        || (FixedRowHeight && RowHeight is not null)
        || VisibleColumns.Any(column => column.Width is not null || column.MinWidth is not null || column.Frozen);

    private async Task ApplyColumnLayoutAsync()
    {
        var signature = ColumnLayoutSignature();
        if (_gridModule is null)
        {
            return;
        }

        if (signature == _appliedColumnLayout)
        {
            await ApplyFrozenOffsetsAsync();
            return;
        }

        _appliedColumnLayout = signature;
        var specs = VisibleColumns.Select(column => new
        {
            key = column.Key,
            width = _columnWidths.GetValueOrDefault(column.Key, column.Width ?? ColumnWidth),
            minWidth = column.MinWidth,
            frozen = column.Frozen
        }).ToArray();
        await _gridModule.InvokeVoidAsync("applyColumns", _viewport, specs, TableMinimumWidth());
    }

    /// <summary>
    /// Narrowest the table may become, as a CSS length: every column's declared width, or its
    /// minimum, or the auto-column floor, plus the grid's own control columns. Under the fixed table
    /// layout the columns without a width share whatever the declared ones leave, which is nothing
    /// once the viewport is narrower than their sum: they collapsed to zero, titles and filters
    /// piled on top of each other. Below this width the viewport scrolls sideways instead.
    /// </summary>
    internal string TableMinimumWidth()
    {
        var terms = VisibleColumns
            .Select(column => _columnWidths.GetValueOrDefault(column.Key, column.Width ?? ColumnWidth) ?? column.MinWidth)
            .Select(width => IsAbsoluteLength(width) ? width! : "var(--omni-data-grid-column-min-width)")
            .ToList();
        if (ShowDetailColumn)
        {
            terms.Add("var(--omni-data-grid-control-width)");
        }

        if (SelectionMode != OmniDataGridSelectionMode.None)
        {
            terms.Add("var(--omni-data-grid-control-width)");
        }

        if (HasEditing)
        {
            terms.Add("var(--omni-data-grid-edit-width)");
        }

        return terms.Count == 0 ? "0px" : $"calc({string.Join(" + ", terms)})";
    }

    /// <summary>
    /// A percentage, or any expression the table itself resolves, cannot be added to a minimum
    /// width without referring back to that width; such a column counts as an auto one.
    /// </summary>
    private static bool IsAbsoluteLength(string? width)
    {
        if (string.IsNullOrWhiteSpace(width) || width.Contains('%', StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = width.Trim();
        var digits = trimmed.AsSpan().TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        return digits.Length > 0
            && double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            && trimmed[digits.Length..].ToLowerInvariant() is "px" or "rem" or "em" or "ch" or "pt" or "vw";
    }

    private string ColumnLayoutSignature() => string.Join(
        '|',
        VisibleColumns.Select(column =>
            $"{column.Key}:{_columnWidths.GetValueOrDefault(column.Key, column.Width ?? ColumnWidth)}:{column.MinWidth}:{column.Frozen}"));

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

        if ((RowHeight is not null && !StructuredVirtual) || snapshot.Rows is null)
        {
            return moved;
        }

        foreach (var row in snapshot.Rows)
        {
            moved |= _window.Measure(row.Index, row.Height);
        }

        return moved;
    }

    /// <summary>Invoked by the grid script when the viewport is scrolled or resized.</summary>
    [JSInvokable]
    public async Task OnViewportChangedAsync(double scrollTop, double viewportHeight)
    {
        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;
        var previous = _range;
        SyncVirtualWindow();
        if (previous == _range)
        {
            return;
        }

        await EnsureVirtualDataAsync();
        StateHasChanged();
    }

    /// <summary>Scrolls the virtualized viewport so that <paramref name="index"/> sits at its top edge.</summary>
    public async Task ScrollToIndexAsync(int index)
    {
        if (!Virtualized || _gridModule is null)
        {
            return;
        }

        await _gridModule.InvokeVoidAsync("scrollToOffset", _viewport, _window.OffsetOf(index));
    }

    // ---- rows ---------------------------------------------------------------------------------

    /// <summary>Flattens the current view into the exact sequence of rows the markup emits.</summary>
    private IReadOnlyList<GridRenderRow<TItem>> RenderRows()
    {
        if (StructuredVirtual)
        {
            var slots = Slots;
            var end = Math.Min(_range.EndIndex, slots.Count);
            var windowRows = new List<GridRenderRow<TItem>>(Math.Max(0, end - _range.StartIndex));
            for (var slot = _range.StartIndex; slot < end; slot++)
            {
                windowRows.Add(slots[slot]);
            }

            return windowRows;
        }

        if (Virtualized)
        {
            var virtualRows = new List<GridRenderRow<TItem>>(Math.Max(0, _range.Count));
            for (var index = _range.StartIndex; index < _range.EndIndex; index++)
            {
                var found = TryGetVirtualItem(index, out var virtualItem);
                virtualRows.Add(found
                    ? Describe(virtualItem, index, [], false)
                    : new GridRenderRow<TItem>(index, default!, false, [], false, null, false, false));
            }

            return virtualRows;
        }

        var groups = ActiveGroups;
        return groups.Count > 0 ? GroupedRows(groups, VisibleItems) : FlatRows(VisibleItems);
    }

    private IReadOnlyList<GridRenderRow<TItem>> FlatRows(IReadOnlyList<TItem> items)
    {
        var rows = new List<GridRenderRow<TItem>>(items.Count);
        var first = true;
        object? previousGroup = null;
        var index = 0;
        foreach (var item in items)
        {
            var headers = new List<GridGroupHeader>();
            if (GroupBy is not null)
            {
                var currentGroup = GroupBy(item);
                if (first || !Equals(previousGroup, currentGroup))
                {
                    var count = items.Count(candidate => Equals(GroupBy(candidate), currentGroup));
                    var text = GroupLabel?.Invoke(currentGroup, count) ?? $"{currentGroup} ({count})";
                    headers.Add(new GridGroupHeader($"{currentGroup ?? NullGroupKey}", text, 0, count, true));
                }

                previousGroup = currentGroup;
            }

            first = false;
            rows.Add(Describe(item, index, headers, DetailTemplate is not null && IsExpanded(ItemKey(item))));
            index++;
        }

        return rows;
    }

    private IReadOnlyList<GridRenderRow<TItem>> GroupedRows(IReadOnlyList<OmniDataGridGroup> groups, IReadOnlyList<TItem> items)
    {
        var accessors = groups
            .Select(group => EffectiveColumns.FirstOrDefault(column => column.Key == group.Key))
            .Where(column => column is not null)
            .Select(column => column!)
            .ToArray();
        if (accessors.Length == 0)
        {
            return FlatRows(items);
        }

        var ordered = items
            .Select((item, index) => (Item: item, Index: index))
            .OrderBy(entry => 0);
        for (var level = 0; level < accessors.Length; level++)
        {
            var accessor = accessors[level];
            ordered = groups[level].Descending
                ? ordered.ThenByDescending(entry => accessor.Value(entry.Item), Comparer<object?>.Default)
                : ordered.ThenBy(entry => accessor.Value(entry.Item), Comparer<object?>.Default);
        }

        var sorted = ordered.ThenBy(entry => entry.Index).Select(entry => entry.Item).ToArray();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in sorted)
        {
            var path = string.Empty;
            for (var level = 0; level < accessors.Length; level++)
            {
                path = $"{path}/{accessors[level].Value(item) ?? NullGroupKey}";
                counts[path] = counts.GetValueOrDefault(path) + 1;
            }
        }

        var rows = new List<GridRenderRow<TItem>>(sorted.Length);
        var previousPaths = new string[accessors.Length];
        var index = 0;
        foreach (var item in sorted)
        {
            var headers = new List<GridGroupHeader>();
            var path = string.Empty;
            var reopened = false;
            var hidden = false;
            for (var level = 0; level < accessors.Length; level++)
            {
                var accessor = accessors[level];
                var value = accessor.Value(item);
                path = $"{path}/{value ?? NullGroupKey}";
                if (reopened || !string.Equals(previousPaths[level], path, StringComparison.Ordinal))
                {
                    reopened = true;
                    previousPaths[level] = path;
                    var count = counts.GetValueOrDefault(path);
                    var expanded = IsGroupExpanded(path);
                    var text = GroupLabel?.Invoke(value, count) ?? $"{accessor.Title} : {value} ({count})";
                    if (!hidden)
                    {
                        headers.Add(new GridGroupHeader(path, text, level, count, expanded));
                    }
                }

                hidden |= !IsGroupExpanded(path);
            }

            if (!hidden)
            {
                rows.Add(Describe(item, index, headers, DetailTemplate is not null && IsExpanded(ItemKey(item))));
                index++;
            }
            else if (headers.Count > 0)
            {
                rows.Add(new GridRenderRow<TItem>(-1, default!, false, headers, false, null, false, false));
            }
        }

        return rows;
    }

    private GridRenderRow<TItem> Describe(TItem item, int index, IReadOnlyList<GridGroupHeader> headers, bool showDetail)
    {
        if (RowRender is null)
        {
            return new GridRenderRow<TItem>(index, item, true, headers, showDetail, null, true, true);
        }

        var args = new OmniDataGridRowRenderArgs<TItem>(item, index);
        RowRender(args);
        return new GridRenderRow<TItem>(index, item, true, headers, showDetail, args.CssClass, args.Expandable, args.Selectable);
    }

    private bool IsGroupExpanded(string path) => AllGroupsExpanded
        ? !_collapsedGroups.Contains(path)
        : _collapsedGroups.Contains(path);

    private void ToggleGroup(string path)
    {
        if (!_collapsedGroups.Remove(path))
        {
            _collapsedGroups.Add(path);
        }

        RefreshSlots();
    }

    // ---- selection ----------------------------------------------------------------------------

    private async Task ToggleSelectionAsync(TItem item)
    {
        var key = ItemKey(item);
        var keys = SelectedKeys.ToList();
        var selecting = !keys.Remove(key);
        if (selecting)
        {
            if (SelectionMode == OmniDataGridSelectionMode.Single)
            {
                keys.Clear();
            }
            keys.Add(key);
        }

        _selectedKeyIndex = keys.ToHashSet();
        await SelectedKeysChanged.InvokeAsync(keys);
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(CurrentRows().Where(candidate => _selectedKeyIndex.Contains(ItemKey(candidate))).ToArray());
        }

        if (selecting)
        {
            await RowSelect.InvokeAsync(item);
        }
    }

    /// <summary>
    /// The rows the header checkbox acts on: the ones on screen that the host lets be selected.
    /// Rows of other pages are left as they are, which is what a reader expects from a box that sits
    /// above the rows it can see.
    /// </summary>
    private IReadOnlyList<TItem> SelectableVisibleRows => VisibleItems
        .Where(item => RowRender is null || Describe(item, 0, [], false).Selectable)
        .ToArray();

    private bool HasSelectableVisibleRows => SelectableVisibleRows.Count > 0;

    private bool AllVisibleSelected
    {
        get
        {
            var rows = SelectableVisibleRows;
            return rows.Count > 0 && rows.All(item => IsSelected(ItemKey(item)));
        }
    }

    /// <summary>Selects every selectable row on screen, or clears them when they all already are.</summary>
    private async Task ToggleAllSelectionAsync()
    {
        var rows = SelectableVisibleRows;
        var clearing = rows.Count > 0 && rows.All(item => IsSelected(ItemKey(item)));
        var keys = SelectedKeys.ToList();
        var added = new List<TItem>();
        foreach (var item in rows)
        {
            var key = ItemKey(item);
            if (clearing)
            {
                keys.Remove(key);
            }
            else if (!keys.Contains(key))
            {
                keys.Add(key);
                added.Add(item);
            }
        }

        _selectedKeyIndex = keys.ToHashSet();
        await SelectedKeysChanged.InvokeAsync(keys);
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(CurrentRows().Where(candidate => _selectedKeyIndex.Contains(ItemKey(candidate))).ToArray());
        }

        foreach (var item in added)
        {
            await RowSelect.InvokeAsync(item);
        }
    }

    private IEnumerable<TItem> CurrentRows() => StructuredVirtual
        ? RenderRows().Where(row => row.HasItem).Select(row => row.Item)
        : Virtualized
        ? Enumerable.Range(_range.StartIndex, Math.Max(0, _range.Count))
            .Select(index => TryGetVirtualItem(index, out var item) ? item : default!)
            .Where(item => item is not null)
        : VisibleItems;

    private async Task ActivateRowAsync(GridRenderRow<TItem> row)
    {
        if (!row.Selectable)
        {
            return;
        }

        if (AllowRowSelectOnRowClick && SelectionMode != OmniDataGridSelectionMode.None)
        {
            await ToggleSelectionAsync(row.Item);
        }

        await RowClick.InvokeAsync(row.Item);
    }

    private Task RowKeyDownAsync(KeyboardEventArgs args, GridRenderRow<TItem> row) =>
        args.Key is "Enter" or " " ? ActivateRowAsync(row) : Task.CompletedTask;

    private EventCallback RowClickCallback(GridRenderRow<TItem> row) => RowsAreInteractive
        ? EventCallback.Factory.Create(this, () => ActivateRowAsync(row))
        : default;

    private EventCallback RowDoubleClickCallback(GridRenderRow<TItem> row) => RowDoubleClick.HasDelegate
        ? EventCallback.Factory.Create(this, () => RowDoubleClick.InvokeAsync(row.Item))
        : default;

    private EventCallback<KeyboardEventArgs> RowKeyDownCallback(GridRenderRow<TItem> row) => RowsAreInteractive
        ? EventCallback.Factory.Create<KeyboardEventArgs>(this, args => RowKeyDownAsync(args, row))
        : default;

    private string? SelectionState(TItem item) => SelectionMode == OmniDataGridSelectionMode.None
        ? null
        : IsSelected(ItemKey(item)) ? "true" : "false";

    // ---- detail rows --------------------------------------------------------------------------

    private async Task ToggleExpandedAsync(TItem item)
    {
        var key = ItemKey(item);
        var keys = ExpandedKeys.ToList();
        var expanding = !keys.Remove(key);
        if (expanding)
        {
            if (ExpandMode == OmniDataGridExpandMode.Single)
            {
                keys.Clear();
            }
            keys.Add(key);
        }

        _expandedKeyIndex = keys.ToHashSet();
        RefreshSlots();
        await ExpandedKeysChanged.InvokeAsync(keys);
        await (expanding ? RowExpand.InvokeAsync(item) : RowCollapse.InvokeAsync(item));
    }

    /// <summary>The expandable rows on screen, the ones the header button acts on.</summary>
    private IReadOnlyList<object> ExpandableVisibleKeys => VisibleItems
        .Where(item => RowRender is null || Describe(item, 0, [], false).Expandable)
        .Select(ItemKey)
        .ToArray();

    private bool AllVisibleExpanded
    {
        get
        {
            var keys = ExpandableVisibleKeys;
            return keys.Count > 0 && keys.All(IsExpanded);
        }
    }

    /// <summary>
    /// Only offered when several rows may be open at once: under a single-row expand mode the
    /// button could only ever break the rule it sits above.
    /// </summary>
    private bool ShowsExpandAll => ShowExpandAll && ExpandMode == OmniDataGridExpandMode.Multiple;

    /// <summary>
    /// Opens every expandable row on screen, or closes them all when they already are open. Rows of
    /// other pages keep their state: the previous version compared the count of every expanded key
    /// with the rows on screen, so a row left open on another page turned the button into a no-op.
    /// </summary>
    private async Task ToggleAllExpandedAsync()
    {
        var visible = ExpandableVisibleKeys;
        var collapsing = visible.Count > 0 && visible.All(IsExpanded);
        var keys = ExpandedKeys.ToList();
        foreach (var key in visible)
        {
            if (collapsing)
            {
                keys.Remove(key);
            }
            else if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        _expandedKeyIndex = keys.ToHashSet();
        RefreshSlots();
        await ExpandedKeysChanged.InvokeAsync(keys);
    }

    // ---- editing ------------------------------------------------------------------------------

    /// <summary>Puts a row in edit mode, honouring <see cref="EditMode"/>.</summary>
    public async Task EditRowAsync(TItem item)
    {
        if (EditMode == OmniDataGridEditMode.Single)
        {
            _editedKeys.Clear();
        }

        _editedKeys.Add(ItemKey(item));
        await EditRequested.InvokeAsync(item);
        StateHasChanged();
    }

    /// <summary>Closes the edit state of a row and reports the update.</summary>
    public async Task UpdateRowAsync(TItem item)
    {
        _editedKeys.Remove(ItemKey(item));
        await RowUpdated.InvokeAsync(item);
        StateHasChanged();
    }

    /// <summary>Closes the edit state of a row without reporting an update.</summary>
    public async Task CancelEditAsync(TItem item)
    {
        _editedKeys.Remove(ItemKey(item));
        await EditCancelled.InvokeAsync(item);
        StateHasChanged();
    }

    // ---- sorting ------------------------------------------------------------------------------

    /// <summary>
    /// Cycles a column through the three sort states on successive clicks: ascending, descending,
    /// then unsorted. The third click removes the column from <see cref="_sorts"/> rather than
    /// looping back to ascending, so the grid can be returned to its natural order.
    /// </summary>
    private async Task SortAsync(string key, bool append)
    {
        var column = EffectiveColumns.FirstOrDefault(candidate => candidate.Key == key);
        // The handler now sits on the whole header cell, so a click on a column that does not sort
        // has to be turned away here rather than by not wiring the handler at all.
        if (column is null || !IsSortable(column))
        {
            return;
        }

        var existing = _sorts.FindIndex(sort => sort.Key == key);
        var wasDescending = existing >= 0 && _sorts[existing].Descending;
        var clear = existing >= 0 && wasDescending;
        if (!append) _sorts.Clear();
        else if (existing >= 0) _sorts.RemoveAt(existing);
        if (!clear)
        {
            _sorts.Add(new OmniDataGridSort(key, existing >= 0) { Property = column?.SortProperty ?? column?.Property });
        }

        await ResetToFirstPageAsync();
        InvalidateLocalProjection();
        await RefreshAfterQueryChangeAsync();
        await PersistStateAsync();
    }

    private string? AriaSort(OmniDataGridColumnDefinition<TItem> column)
    {
        var sort = _sorts.FirstOrDefault(candidate => candidate.Key == column.Key);
        return sort is null ? null : sort.Descending ? "descending" : "ascending";
    }

    /// <summary>
    /// Visual sort indicator next to a sortable header's title, mirroring aria-sort. Null on an
    /// unsorted column so no icon is rendered at all.
    /// </summary>
    private OmniIconName? SortIcon(OmniDataGridColumnDefinition<TItem> column) => AriaSort(column) switch
    {
        "ascending" => OmniIconName.SortAscending,
        "descending" => OmniIconName.SortDescending,
        _ => null
    };

    /// <summary>
    /// Secondary header affordances stay hidden until the header is hovered or focused, unless the
    /// affordance is currently active, in which case it remains visible so the column's state can be
    /// read without pointing at it.
    /// </summary>
    private static string HeaderActionClass(string baseClass, bool active) => CssClassBuilder.Combine([
        "omni-data-grid__icon-button",
        baseClass,
        "omni-data-grid__header-action",
        active ? "omni-data-grid__header-action--active" : null
    ]);

    /// <summary>
    /// The filter entry point is always visible: it is the column's primary control once the inline
    /// filter row has been replaced by the menu, so hiding it until hover would leave no sign that
    /// the column can be filtered at all.
    /// </summary>
    private string FilterToggleClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__filter-menu-toggle",
        HasActiveFilter(column) ? "omni-data-grid__filter-menu-toggle--active" : null
    ]);

    /// <summary>
    /// Whether the column is filtered right now. Read from the applied filter, not the draft: an
    /// advanced condition typed but not yet applied filters nothing, so it offers nothing to clear.
    /// </summary>
    private bool HasActiveFilter(OmniDataGridColumnDefinition<TItem> column) =>
        _filters.TryGetValue(column.Key, out var filter) && filter.IsActive;

    /// <summary>What the advanced filter trigger reads: the applied condition, or the placeholder.</summary>
    private string FilterSummary(OmniDataGridColumnDefinition<TItem> column)
    {
        if (!_filters.TryGetValue(column.Key, out var filter) || !filter.IsActive)
        {
            return Text(FilterText, "GridFilterPlaceholder");
        }

        var first = filter.HasFirst ? Condition(filter.Operator, filter.Value) : null;
        var second = filter.HasSecond ? Condition(filter.SecondOperator, filter.SecondValue) : null;
        return first is not null && second is not null
            ? $"{first} {LogicalLabel(filter.LogicalOperator).ToLower(CultureInfo.CurrentCulture)} {second}"
            : first ?? second ?? string.Empty;

        string Condition(OmniDataGridFilterOperator candidate, string value) => GridColumnFilter.IsValueless(candidate)
            ? OperatorLabel(candidate)
            : $"{OperatorLabel(candidate)} {DisplayFilterValue(value)}";
    }

    /// <summary>A multi-valued filter travels encoded; the summary shows its values, not the encoding.</summary>
    private static string DisplayFilterValue(string value) =>
        string.Join(", ", OmniDataGridFilterValues.Split(value));

    private sealed record FilterEditorRequest(OmniDataGridColumnDefinition<TItem> Column, string Id, bool InPanel);

    private static string FilterEditorClass(bool inPanel) => inPanel
        ? "omni-data-grid__filter-editor omni-data-grid__filter-editor--panel"
        : "omni-data-grid__filter-editor";

    private string FilterCellClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__filter-cell",
        column.Frozen ? "omni-data-grid__column--frozen" : null
    ]);

    /// <summary>
    /// The expand and selection columns carry the grid's own controls. They are frozen with the
    /// data columns as soon as one of those is, since a frozen column that slides over the controls
    /// of its own row hides them.
    /// </summary>
    private string ControlClass(string kind) => CssClassBuilder.Combine([
        "omni-data-grid__control",
        $"omni-data-grid__control--{kind}",
        HasFrozenColumns ? "omni-data-grid__column--frozen" : null
    ]);

    private static string ExpandButtonClass(bool expanded) => expanded
        ? "omni-data-grid__expand omni-data-grid__expand--open"
        : "omni-data-grid__expand";

    private static string GroupExpandClass(bool expanded) => expanded
        ? "omni-data-grid__icon-button omni-data-grid__group-expand omni-data-grid__expand--open"
        : "omni-data-grid__icon-button omni-data-grid__group-expand";

    private bool HasFrozenColumns => VisibleColumns.Any(column => column.Frozen);

    /// <summary>
    /// The sort indicator keeps its slot at all times so the header does not reflow when a column
    /// gains or loses its sort; only its visibility changes.
    /// </summary>
    private string SortIconClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__sort-icon",
        SortIcon(column) is null ? "omni-data-grid__sort-icon--idle" : null
    ]);

    // ---- filtering ----------------------------------------------------------------------------

    private GridColumnFilter FilterOf(OmniDataGridColumnDefinition<TItem> column) =>
        _filters.GetValueOrDefault(column.Key, DefaultFilter(column));

    private GridColumnFilter DraftOf(OmniDataGridColumnDefinition<TItem> column) =>
        _draftFilters.GetValueOrDefault(column.Key, FilterOf(column));

    private static GridColumnFilter DefaultFilter(OmniDataGridColumnDefinition<TItem> column) => new(
        DefaultOperator(column),
        string.Empty,
        column.LogicalFilterOperator,
        column.SecondFilterOperator,
        string.Empty);

    /// <summary>
    /// The operator a filter shape implies. A closed dropdown means equality and a checkable list
    /// means membership, whatever the column's text-oriented default says; only the shapes that
    /// really are free text keep it.
    /// </summary>
    private static OmniDataGridFilterOperator DefaultOperator(OmniDataGridColumnDefinition<TItem> column) => column.FilterType switch
    {
        OmniDataGridColumnFilterType.Select => OmniDataGridFilterOperator.Equals,
        OmniDataGridColumnFilterType.MultiSelect => OmniDataGridFilterOperator.In,
        _ => column.FilterOperator
    };

    /// <summary>
    /// Distinct string values for a Select/Combo filter, read from the locally held <see cref="Items"/>.
    /// A remote (<see cref="Load"/>-backed) grid only ever sees the current page, so Select/Combo on
    /// such a grid is a known limitation rather than a silent wrong answer.
    /// </summary>
    private IReadOnlyList<string> DistinctFilterValues(OmniDataGridColumnDefinition<TItem> column) =>
        column.FilterValues is { } declared
            ? declared.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : DerivedFilterValues(column);

    private IReadOnlyList<string> DerivedFilterValues(OmniDataGridColumnDefinition<TItem> column) => Items
        .Select(item => column.Value(item)?.ToString())
        .Where(value => !string.IsNullOrEmpty(value))
        .Select(value => value!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private string FilterValue(OmniDataGridColumnDefinition<TItem> column) => DraftOf(column).Value;
    private string SecondFilterValue(OmniDataGridColumnDefinition<TItem> column) => DraftOf(column).SecondValue;

    private Task FilterValueChangedAsync(OmniDataGridColumnDefinition<TItem> column, string value) =>
        StageAsync(column, DraftOf(column) with { Value = value });

    private Task SecondFilterValueChangedAsync(OmniDataGridColumnDefinition<TItem> column, string value) =>
        StageAsync(column, DraftOf(column) with { SecondValue = value });

    private Task FilterOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with { Operator = ParseOperator(value, column.FilterOperator) });

    private Task SecondFilterOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with { SecondOperator = ParseOperator(value, column.SecondFilterOperator) });

    private Task LogicalOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with
        {
            LogicalOperator = Enum.TryParse<OmniDataGridLogicalOperator>(value, out var parsed) ? parsed : OmniDataGridLogicalOperator.And
        });

    private static OmniDataGridFilterOperator ParseOperator(string? value, OmniDataGridFilterOperator fallback) =>
        Enum.TryParse<OmniDataGridFilterOperator>(value, out var parsed) ? parsed : fallback;

    /// <summary>
    /// Stores a pending filter change. In advanced mode it waits for the explicit apply action; in
    /// the other modes it applies immediately.
    /// </summary>
    private Task StageAsync(OmniDataGridColumnDefinition<TItem> column, GridColumnFilter filter)
    {
        _draftFilters[column.Key] = filter;
        return UsesAdvancedFilter ? Task.CompletedTask : ApplyFilterAsync(column);
    }

    private async Task ApplyFilterAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        var filter = DraftOf(column);
        if (filter.IsActive)
        {
            _filters[column.Key] = filter;
        }
        else
        {
            _filters.Remove(column.Key);
        }

        await ResetToFirstPageAsync();
        InvalidateLocalProjection();
        await RefreshAfterQueryChangeAsync();
        await PersistStateAsync();
    }

    private async Task ClearFilterAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        _filters.Remove(column.Key);
        _draftFilters.Remove(column.Key);
        await ResetToFirstPageAsync();
        InvalidateLocalProjection();
        await RefreshAfterQueryChangeAsync();
        await PersistStateAsync();
    }

    private IReadOnlyList<OmniDataGridSort> CurrentSorts()
    {
        var keys = EffectiveColumns.Select(column => column.Key).ToHashSet(StringComparer.Ordinal);
        return _sorts.Where(sort => keys.Contains(sort.Key)).ToArray();
    }

    private IReadOnlyList<OmniDataGridFilter> CurrentFilters()
    {
        var keys = EffectiveColumns.Select(column => column.Key).ToHashSet(StringComparer.Ordinal);
        return _filters
            .Where(pair => keys.Contains(pair.Key) && pair.Value.IsActive)
            .Select(pair => new OmniDataGridFilter(
                pair.Key,
                pair.Value.Operator,
                pair.Value.Value,
                pair.Value.LogicalOperator,
                pair.Value.HasSecond ? pair.Value.SecondOperator : null,
                pair.Value.HasSecond ? pair.Value.SecondValue : null))
            .ToArray();
    }

    /// <summary>Restarts the row set after a sort or filter changed, discarding measurements and cached rows.</summary>
    private async Task RefreshAfterQueryChangeAsync()
    {
        if (Virtualized)
        {
            _scrollTop = 0d;
            _window.ResetMeasurements();
            if (Load is not null)
            {
                _virtualSource.Reset();
                _virtualBootstrapped = false;
            }

            RebuildRenderSnapshot();
            if (_gridModule is not null)
            {
                await _gridModule.InvokeVoidAsync("scrollToOffset", _viewport, 0d);
            }

            await BootstrapVirtualizationAsync();
            return;
        }

        if (Load is not null || ExternalData)
        {
            await ReloadAsync();
        }

        RebuildRenderSnapshot();
    }

    // ---- grouping -----------------------------------------------------------------------------

    private bool IsGrouped(string key) => ActiveGroups.Any(group => group.Key == key);

    private async Task ToggleGroupingAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        var groups = ActiveGroups.ToList();
        var index = groups.FindIndex(group => group.Key == column.Key);
        if (index >= 0)
        {
            groups.RemoveAt(index);
        }
        else
        {
            groups.Add(new OmniDataGridGroup(column.Key));
        }

        await GroupsChanged.InvokeAsync(groups);
    }

    private string GroupTitle(OmniDataGridGroup group) =>
        EffectiveColumns.FirstOrDefault(column => column.Key == group.Key)?.Title ?? group.Key;

    // ---- paging -------------------------------------------------------------------------------

    private async Task ResetToFirstPageAsync()
    {
        if (Page == 1)
        {
            return;
        }

        Page = 1;
        await PageChanged.InvokeAsync(1);
    }

    private async Task ChangePageAsync(int page)
    {
        Page = page;
        InvalidateLocalProjection();
        await PageChanged.InvokeAsync(page);
        if ((Load is not null || ExternalData) && !Virtualized) await ReloadAsync();
        RebuildRenderSnapshot();
    }

    private async Task ChangePageSizeAsync(int pageSize)
    {
        PageSize = pageSize;
        InvalidateLocalProjection();
        await PageSizeChanged.InvokeAsync(pageSize);
        await ResetToFirstPageAsync();
        if ((Load is not null || ExternalData) && !Virtualized) await ReloadAsync();
        RebuildRenderSnapshot();
    }

    private string PagingSummary()
    {
        var total = TotalCount;
        var first = total == 0 ? 0 : ((EffectivePage - 1) * Math.Max(1, PageSize)) + 1;
        var last = total == 0 ? 0 : Math.Min(total, first + Math.Max(1, PageSize) - 1);
        return string.IsNullOrWhiteSpace(PagingSummaryFormat)
            ? Localize("GridPagingSummary", first, last, total)
            : string.Format(CultureInfo.CurrentCulture, PagingSummaryFormat, first, last, total);
    }

    public async Task ReloadAsync()
    {
        if (ExternalData)
        {
            await RequestExternalDataAsync();
            RebuildRenderSnapshot();
            return;
        }
        if (Load is null) return;
        if (Virtualized)
        {
            _virtualSource.Reset();
            _window.ResetMeasurements();
            _virtualBootstrapped = false;
            RebuildRenderSnapshot();
            await BootstrapVirtualizationAsync();
            return;
        }

        var sorts = CurrentSorts();
        var filters = CurrentFilters();
        await _remote.LoadAsync(token => Load(new OmniDataGridLoadRequest(Page, PageSize, sorts, filters, token)));
        RebuildRenderSnapshot();
    }

    private async Task RequestExternalDataAsync()
    {
        if (!ExternalData) return;
        _externalRequested = true;
        await LoadRequested.InvokeAsync(new OmniDataGridLoadRequest(
            Page,
            PageSize,
            CurrentSorts(),
            CurrentFilters(),
            CancellationToken.None));
    }

    // ---- presentation -------------------------------------------------------------------------

    private bool IsSortable(OmniDataGridColumnDefinition<TItem> column) => AllowSorting && column.Sortable;
    private bool IsFilterable(OmniDataGridColumnDefinition<TItem> column) => AllowFiltering && column.Filterable;
    private bool IsResizable(OmniDataGridColumnDefinition<TItem> column) => AllowColumnResize && column.Resizable != false;

    private int ResizeAriaValueNow(OmniDataGridColumnDefinition<TItem> column) => (int)Math.Round(Math.Clamp(
        ParseWidth(_columnWidths.GetValueOrDefault(column.Key, column.Width ?? ColumnWidth)),
        MinimumColumnWidth,
        2000d));
    private bool IsGroupable(OmniDataGridColumnDefinition<TItem> column) => AllowGrouping && column.Groupable;
    /// <summary>
    /// The inline filter row and the per-column header menu are two entry points to the same
    /// filter, so only one of them is ever rendered: turning <see cref="ShowHeaderFilterMenu"/> on
    /// moves the value control into the header and drops the row.
    /// </summary>
    private bool HasFilterRow => !ShowHeaderFilterMenu && VisibleColumns.Any(IsFilterable);

    private string FilterId(OmniDataGridColumnDefinition<TItem> column) => $"{Id ?? "omni-grid"}-filter-{column.Key}";
    private string HeaderFilterId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-menu";
    private string OperatorId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-operator";
    private string SecondFilterId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-second";
    private string SecondOperatorId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-second-operator";
    private string LogicalId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-logical";

    private string Text(string? candidate, string key) => string.IsNullOrWhiteSpace(candidate) ? Localize(key) : candidate;

    private string OperatorLabel(OmniDataGridFilterOperator candidate) => candidate switch
    {
        OmniDataGridFilterOperator.Contains => Text(ContainsText, "GridFilterContains"),
        OmniDataGridFilterOperator.DoesNotContain => Text(DoesNotContainText, "GridFilterDoesNotContain"),
        OmniDataGridFilterOperator.Equals => Text(EqualsText, "GridFilterEquals"),
        OmniDataGridFilterOperator.NotEquals => Text(NotEqualsText, "GridFilterNotEquals"),
        OmniDataGridFilterOperator.StartsWith => Text(StartsWithText, "GridFilterStartsWith"),
        OmniDataGridFilterOperator.EndsWith => Text(EndsWithText, "GridFilterEndsWith"),
        OmniDataGridFilterOperator.GreaterThan => Localize("GridFilterGreaterThan"),
        OmniDataGridFilterOperator.GreaterThanOrEquals => Localize("GridFilterGreaterThanOrEquals"),
        OmniDataGridFilterOperator.LessThan => Localize("GridFilterLessThan"),
        OmniDataGridFilterOperator.LessThanOrEquals => Localize("GridFilterLessThanOrEquals"),
        OmniDataGridFilterOperator.IsNull => Localize("GridFilterIsNull"),
        OmniDataGridFilterOperator.IsNotNull => Localize("GridFilterIsNotNull"),
        OmniDataGridFilterOperator.IsEmpty => Localize("GridFilterIsEmpty"),
        _ => Localize("GridFilterIsNotEmpty")
    };

    private string LogicalLabel(OmniDataGridLogicalOperator candidate) => candidate == OmniDataGridLogicalOperator.Or
        ? Text(OrOperatorText, "GridFilterOr")
        : Text(AndOperatorText, "GridFilterAnd");

    private static IReadOnlyList<OmniDataGridFilterOperator> FilterOperators { get; } =
        Enum.GetValues<OmniDataGridFilterOperator>();

    private string GridClass() => Css(
        "omni-data-grid",
        FillAvailableHeight ? "omni-data-grid--fill" : null,
        HighlightRowOnHover ? "omni-data-grid--row-hover" : null,
        AllowAlternatingRows ? "omni-data-grid--striped" : null,
        Virtualized ? "omni-data-grid--virtual" : null,
        Responsive ? "omni-data-grid--responsive" : null,
        FixedRowHeight && RowHeight is not null ? "omni-data-grid--fixed-row-height" : null,
        HeaderWrap == OmniDataGridHeaderWrap.Truncate ? "omni-data-grid--header-truncate" : null,
        GridLines == OmniDataGridLines.Default ? null : $"omni-data-grid--lines-{GridLines.ToString().ToLowerInvariant()}");

    private string ViewportClass() => CssClassBuilder.Combine([
        "omni-data-grid__viewport",
        Virtualized ? "omni-data-grid__viewport--virtual" : null,
        FillAvailableHeight ? "omni-data-grid__viewport--fill" : null,
        Height is null ? null : "omni-data-grid__viewport--sized",
        EffectiveMaxHeight is null ? null : "omni-data-grid__viewport--capped"
    ]);

    // A numeric column left at the default alignment takes the end, in figures of one width, so its
    // values compare at a glance; an alignment the consumer set (Center, End) is kept as is.
    private static bool IsNumericCell(OmniDataGridColumnDefinition<TItem> column) =>
        column.Numeric && column.TextAlign == OmniDataGridTextAlign.Start;

    private string ColumnClass(OmniDataGridColumnDefinition<TItem> column, bool header) => CssClassBuilder.Combine([
        IsNumericCell(column)
            ? "omni-data-grid__column--align-end omni-data-grid__cell--numeric"
            : $"omni-data-grid__column--align-{column.TextAlign.ToString().ToLowerInvariant()}",
        column.Frozen ? "omni-data-grid__column--frozen" : null,
        HighlightActiveColumn && IsColumnActive(column) ? "omni-data-grid__column--active" : null,
        header && IsSortable(column) ? "omni-data-grid__column--sortable" : null,
        header ? column.HeaderCssClass : column.CssClass
    ]);

    /// <summary>A column is active while it carries the sort or a filter value.</summary>
    private bool IsColumnActive(OmniDataGridColumnDefinition<TItem> column) =>
        _sorts.Any(sort => sort.Key == column.Key)
        || (_filters.TryGetValue(column.Key, out var filter) && filter.IsActive);

    private string RowClass(GridRenderRow<TItem> row) => CssClassBuilder.Combine([
        "omni-data-grid__row",
        row.HasItem && IsSelected(ItemKey(row.Item)) ? "omni-data-grid__row--selected" : null,
        AllowAlternatingRows && row.Index % 2 == 1 ? "omni-data-grid__row--alternate" : null,
        RowsAreInteractive && row.Selectable ? "omni-data-grid__row--interactive" : null,
        row.CssClass
    ]);

    private string? RowCountAttribute() => Virtualized
        ? TotalCount.ToString(CultureInfo.InvariantCulture)
        : null;

    private string? RowIndexAttribute(int rowIndex) => Virtualized
        ? (rowIndex + 1).ToString(CultureInfo.InvariantCulture)
        : null;

    private async Task ResizeColumnAsync(OmniDataGridColumnDefinition<TItem> column, int step)
    {
        var current = ParseWidth(_columnWidths.GetValueOrDefault(column.Key, column.Width ?? ColumnWidth));
        await ApplyColumnWidthAsync(column.Key, current + (step * 32d));
    }

    /// <summary>Keyboard equivalent of the drag handle, one step per arrow press.</summary>
    private Task ResizeKeyDownAsync(OmniDataGridColumnDefinition<TItem> column, string key) => key switch
    {
        "ArrowLeft" => ResizeColumnAsync(column, -1),
        "ArrowRight" => ResizeColumnAsync(column, 1),
        _ => Task.CompletedTask
    };

    /// <summary>
    /// Final width of a pointer drag on a column's resize handle, in CSS pixels measured by
    /// omni-grid.js. The gesture itself never round-trips to .NET; only its outcome does.
    /// </summary>
    [JSInvokable]
    public async Task OnColumnResizedAsync(string key, double width)
    {
        if (!AllowColumnResize || VisibleColumns.All(column => column.Key != key))
        {
            return;
        }

        await ApplyColumnWidthAsync(key, width);
        StateHasChanged();
    }

    private async Task ApplyColumnWidthAsync(string key, double width)
    {
        var clamped = Math.Max(MinimumColumnWidth, width);
        var value = $"{clamped.ToString(CultureInfo.InvariantCulture)}px";
        _columnWidths[key] = value;
        _appliedColumnLayout = null;
        var change = new OmniDataGridColumnWidthChange(key, value);
        await ColumnWidthChanged.InvokeAsync(change);
        await ColumnResized.InvokeAsync(change);
        await PersistStateAsync();
    }

    private const double MinimumColumnWidth = 48d;

    /// <summary>
    /// Reads a declared CSS width back into pixels for the keyboard resize steps. Only absolute
    /// units can be resolved without measuring the document, so a percentage or any other relative
    /// unit falls back to the default estimate rather than silently pretending to be pixels.
    /// </summary>
    private static double ParseWidth(string? width)
    {
        const double fallback = 160d;
        if (string.IsNullOrWhiteSpace(width))
        {
            return fallback;
        }

        var trimmed = width.Trim();
        var digits = trimmed.AsSpan().TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%");
        if (digits.Length == 0
            || !double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return fallback;
        }

        var unit = trimmed.AsSpan(digits.Length).Trim().ToString().ToLowerInvariant();
        return unit switch
        {
            "" or "px" => parsed,
            "rem" or "em" => parsed * RootFontSize,
            "pt" => parsed * 4d / 3d,
            _ => fallback
        };
    }

    /// <summary>Browser default root font size, used to turn rem/em widths into pixels.</summary>
    private const double RootFontSize = 16d;

    /// <summary>
    /// The single place that decides which editor a filterable column gets. A column's own
    /// <c>FilterTemplate</c> wins over every built-in shape, which is how a mode none of them covers
    /// is added without touching the grid. Shared by the inline filter row and the header filter
    /// menu, and by the primary and secondary condition of the advanced filter mode.
    /// </summary>
    private RenderFragment FilterValueControl(OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged, bool inPanel) => builder =>
    {
        if (column.FilterTemplate is not null)
        {
            // The template's callbacks belong to whichever component declared the column. When that
            // is a component of its own inside Columns, the event re-renders it and not the grid, so
            // the filter was taken and nothing was filtered: the grid renders itself here.
            builder.AddContent(0, column.FilterTemplate(new OmniDataGridFilterContext(
                id,
                value,
                DistinctFilterValues(column),
                Text(FilterText, "GridFilterPlaceholder"),
                async changed =>
                {
                    await onChanged(changed);
                    StateHasChanged();
                })
            {
                InPopover = inPanel
            }));
            return;
        }

        var onChange = EventCallback.Factory.Create<ChangeEventArgs>(this, args => onChanged(args.Value?.ToString() ?? string.Empty));
        switch (column.FilterType)
        {
            case OmniDataGridColumnFilterType.MultiSelect:
                builder.OpenComponent<OmniDataGridFilterMultiSelect>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridFilterMultiSelect.Id), id);
                builder.AddComponentParameter(2, nameof(OmniDataGridFilterMultiSelect.Value), value);
                builder.AddComponentParameter(3, nameof(OmniDataGridFilterMultiSelect.Suggestions), DistinctFilterValues(column));
                builder.AddComponentParameter(4, nameof(OmniDataGridFilterMultiSelect.Placeholder), Text(FilterText, "GridFilterPlaceholder"));
                builder.AddComponentParameter(5, nameof(OmniDataGridFilterMultiSelect.Searchable), column.FilterSearchable);
                builder.AddComponentParameter(
                    6,
                    nameof(OmniDataGridFilterMultiSelect.ValueChanged),
                    EventCallback.Factory.Create<string>(this, encoded => onChanged(encoded)));
                // Inside a popover the list is already on demand; in a header row it has to fold
                // into one line, or it stretches the header to the height of the whole list.
                builder.AddComponentParameter(
                    7,
                    nameof(OmniDataGridFilterMultiSelect.Presentation),
                    inPanel ? OmniMultiSelectPresentation.List : OmniMultiSelectPresentation.Compact);
                builder.CloseComponent();
                break;

            case OmniDataGridColumnFilterType.Select:
                builder.OpenElement(0, "select");
                builder.AddAttribute(1, "id", id);
                builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
                builder.AddAttribute(3, "value", value);
                builder.AddAttribute(4, "onchange", onChange);
                builder.OpenElement(5, "option");
                builder.AddAttribute(6, "value", string.Empty);
                builder.AddContent(7, Text(FilterText, "GridFilterPlaceholder"));
                builder.CloseElement();
                var selectSeq = 8;
                foreach (var candidate in DistinctFilterValues(column))
                {
                    builder.OpenElement(selectSeq++, "option");
                    builder.AddAttribute(selectSeq++, "value", candidate);
                    builder.AddAttribute(selectSeq++, "selected", string.Equals(candidate, value, StringComparison.Ordinal));
                    builder.AddContent(selectSeq++, candidate);
                    builder.CloseElement();
                }
                builder.CloseElement();
                break;

            case OmniDataGridColumnFilterType.Combo:
                builder.OpenComponent<OmniDataGridFilterCombo>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridFilterCombo.Id), id);
                builder.AddComponentParameter(2, nameof(OmniDataGridFilterCombo.Value), value);
                builder.AddComponentParameter(3, nameof(OmniDataGridFilterCombo.Suggestions), DistinctFilterValues(column));
                builder.AddComponentParameter(4, nameof(OmniDataGridFilterCombo.Placeholder), Text(FilterText, "GridFilterPlaceholder"));
                builder.AddComponentParameter(
                    5,
                    nameof(OmniDataGridFilterCombo.ValueChanged),
                    EventCallback.Factory.Create<string>(this, typed => onChanged(typed)));
                builder.AddComponentParameter(
                    6,
                    nameof(OmniDataGridFilterCombo.Picked),
                    EventCallback.Factory.Create(this, CloseFilterMenusAsync));
                builder.CloseComponent();
                break;

            default:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", id);
                builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
                builder.AddAttribute(3, "placeholder", Text(FilterText, "GridFilterPlaceholder"));
                builder.AddAttribute(4, "value", value);
                // Filters as the user types rather than on blur, so the table follows the keystrokes.
                builder.AddAttribute(5, "oninput", onChange);
                builder.CloseElement();
                break;
        }
    };

    private RenderFragment Cell(OmniDataGridColumnDefinition<TItem> column, TItem item, bool editing) => builder =>
    {
        if (editing && column.EditTemplate is not null)
        {
            builder.AddContent(0, column.EditTemplate(item));
            return;
        }

        if (column.Template is not null)
        {
            builder.AddContent(1, column.Template(item));
            return;
        }

        var value = column.Value(item);
        if (column.Format is not null)
        {
            builder.AddContent(2, column.Format(value));
        }
        else if (!string.IsNullOrWhiteSpace(column.FormatString))
        {
            builder.AddContent(3, string.Format(CultureInfo.CurrentCulture, column.FormatString, value));
        }
        else
        {
            builder.AddContent(4, value?.ToString());
        }
    };

    // ---- lifecycle ----------------------------------------------------------------------------

    private async Task DetachViewportAsync()
    {
        if (!_virtualAttached || _gridModule is null)
        {
            return;
        }

        _virtualAttached = false;
        try
        {
            await _gridModule.InvokeVoidAsync("detach", _viewport);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposeRequested = true;
        await _lifecycleGate.WaitAsync();
        try
        {
            try
            {
                await DetachViewportAsync();
                if (_resizeAttached && _gridModule is not null)
                {
                    _resizeAttached = false;
                    await _gridModule.InvokeVoidAsync("detachResize", _viewport);
                }

                if (_filterMenuAttached && _gridModule is not null)
                {
                    _filterMenuAttached = false;
                    await _gridModule.InvokeVoidAsync("detachFilterMenus", _viewport);
                }

                if (_gridModule is not null)
                {
                    await _gridModule.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
            }

            _selfReference?.Dispose();
            await _virtualSource.DisposeAsync();
            await _remote.DisposeAsync();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }
}
