using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the internal collaborators of an <see cref="OmniDataGrid{TItem}"/> need from the grid: its parameters,
/// its services and render requests, the callbacks of its script, and the other collaborators, reached through
/// their role interfaces only. The grid creates the collaborators and hands itself to them as this host, so no
/// collaborator depends on the component nor on another collaborator's class.
/// </summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridHost<TItem>
{
    // ---- collaborators ----------------------------------------------------------------------------

    IGridColumnSet<TItem> ColumnSet { get; }

    IGridColumnArrangement<TItem> Arrangement { get; }

    IGridQueryState<TItem> Query { get; }

    IGridFilterEditor<TItem> FilterEditor { get; }

    IGridDataView<TItem> View { get; }

    IGridVirtualViewport Virtual { get; }

    IGridRowBuilder<TItem> Rows { get; }

    IGridGrouping<TItem> Grouping { get; }

    IGridSelection Selection { get; }

    IGridExpansion Expansion { get; }

    IGridRowEditing<TItem> Editing { get; }

    IGridPaging Paging { get; }

    IGridColumnLayout<TItem> ColumnLayout { get; }

    IGridLayoutInterop LayoutInterop { get; }

    IGridScriptBridge Script { get; }

    IGridFrozenState FrozenState { get; }

    IGridNewRowHighlight<TItem> Highlight { get; }

    IGridStatePersistence Persistence { get; }

    IGridCssClasses Classes { get; }

    IGridTree<TItem> Tree { get; }

    IGridColumnChooser Chooser { get; }

    // ---- services and rendering -------------------------------------------------------------------

    IJSRuntime JavaScript { get; }

    OmniPreferences Preferences { get; }

    IServiceProvider Services { get; }

    /// <summary>The logger of the grid's category (<c>ILogger&lt;OmniDataGrid&lt;TItem&gt;&gt;</c>), or a null logger without logging.</summary>
    ILogger Logger { get; }

    TimeProvider TimeSource { get; }

    bool DisposeRequested { get; }

    string BaseId { get; }

    ElementReference Viewport { get; }

    bool SizesToContent { get; }

    string? Class { get; }

    object ItemKey(TItem item);

    void Render();

    Task RenderLaterAsync();

    Task DispatchAsync(Func<Task> work);

    Task DispatchDetachedAsync(Func<Task> work);

    string Text(string name, params object[] arguments);

    DateTimeOffset Now();

    void RebuildRenderSnapshot();

    Task ReloadAsync();

    Task ResetLayoutAsync();

    // ---- script callbacks (forwarded by GridInteropBridge) ------------------------------------------

    Task OnHorizontalScrollChangedAsync(bool scrolled);

    Task OnContentResizedAsync();

    Task OnViewportChangedAsync(double scrollTop, double viewportHeight);

    string[]? GetColumnAutoFitTexts(string key);

    Task OnColumnAutoFitAsync(string key, double width);

    Task OnColumnResizedAsync(string key, double width);

    Task OnColumnDroppedAsync(string key, string targetKey, bool after);

    // ---- data and paging parameters -----------------------------------------------------------------

    IReadOnlyList<TItem> Items { get; }

    Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? Load { get; }

    RenderFragment? Columns { get; }

    string? Caption { get; }

    int Page { get; }

    EventCallback<int> PageChanged { get; }

    int PageSize { get; }

    EventCallback<int> PageSizeChanged { get; }

    IReadOnlyList<int> PageSizeOptions { get; }

    OmniDataGridScrollMode ScrollMode { get; }

    OmniDataGridPlacement PagerPlacement { get; }

    bool AlwaysShowPager { get; }

    int? Count { get; }

    // ---- selection and row events -------------------------------------------------------------------

    OmniDataGridSelectionMode SelectionMode { get; }

    Func<TItem, object>? KeyOf { get; }

    TimeSpan? NewRowHighlight { get; }

    IReadOnlyList<TItem> Value { get; }

    EventCallback<IReadOnlyList<TItem>> ValueChanged { get; }

    bool AllowRowSelectOnRowClick { get; }

    EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowClick { get; }

    EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; }

    EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowContextMenu { get; }

    EventCallback<OmniDataGridCellMouseEventArgs<TItem>> OnCellClick { get; }

    EventCallback<OmniDataGridCellMouseEventArgs<TItem>> OnCellDoubleClick { get; }

    Action<OmniDataGridRowRenderArgs<TItem>>? RowRender { get; }

    // ---- editing and detail rows ----------------------------------------------------------------------

    OmniDataGridRowMode EditMode { get; }

    OmniDataGridEditTrigger EditTrigger { get; }

    EventCallback<TItem> OnRowEdit { get; }

    EventCallback<TItem> OnRowUpdate { get; }

    EventCallback<TItem> OnRowEditCancel { get; }

    RenderFragment<TItem>? DetailTemplate { get; }

    IReadOnlyList<object> ExpandedKeys { get; }

    EventCallback<IReadOnlyList<object>> ExpandedKeysChanged { get; }

    OmniDataGridRowMode ExpandMode { get; }

    bool ShowExpandColumn { get; }

    bool ShowEditColumn { get; }

    bool ShowExpandAll { get; }

    EventCallback<TItem> OnRowExpand { get; }

    EventCallback<TItem> OnRowCollapse { get; }

    // ---- tree rows --------------------------------------------------------------------------------------

    Func<TItem, IReadOnlyList<TItem>?>? ChildrenOf { get; }

    string? TreeColumnKey { get; }

    IReadOnlyList<object> TreeExpandedKeys { get; }

    EventCallback<IReadOnlyList<object>> TreeExpandedKeysChanged { get; }

    Func<TItem, bool>? InitiallyExpanded { get; }

    Func<TItem, bool>? CanToggleTreeRow { get; }

    bool ToggleTreeOnRowClick { get; }

    // ---- grouping ---------------------------------------------------------------------------------------

    bool AllowGrouping { get; }

    bool ShowGroupPanel { get; }

    IReadOnlyList<OmniDataGridGroup> Groups { get; }

    EventCallback<IReadOnlyList<OmniDataGridGroup>> GroupsChanged { get; }

    bool AllGroupsExpanded { get; }

    bool ShowExpandAllGroups { get; }

    Func<object?, int, string>? GroupLabel { get; }

    // ---- sorting and filtering --------------------------------------------------------------------------

    bool AllowSorting { get; }

    bool Filterable { get; }

    OmniDataGridFilterMode FilterMode { get; }

    OmniDataGridFilterPlacement FilterPlacement { get; }

    OmniDataGridDateFilterEditor HeaderMenuDateFilter { get; }

    bool HideFilterMenuOnSelect { get; }

    bool CaseSensitiveFilters { get; }

    bool IgnoreDiacritics { get; }

    // ---- presentation -----------------------------------------------------------------------------------

    bool AllowColumnResize { get; }

    bool AllowColumnAutoFit { get; }

    OmniDataGridColumnSizing ColumnSizing { get; }

    EventCallback<OmniDataGridColumnWidthChange> OnColumnResize { get; }

    bool AllowColumnReorder { get; }

    bool AllowColumnHide { get; }

    bool ShowResetLayout { get; }

    bool AllowColumnFreeze { get; }

    EventCallback<OmniDataGridColumnOrderChange> OnColumnReorder { get; }

    bool AllowAlternatingRows { get; }

    bool HighlightActiveColumn { get; }

    bool HighlightRowOnHover { get; }

    bool HighlightColumnOnHover { get; }

    OmniDataGridLines GridLines { get; }

    bool Responsive { get; }

    string? EmptyText { get; }

    RenderFragment? LoadingContent { get; }

    EventCallback<Exception> OnLoadError { get; }

    bool Busy { get; }

    bool ShowLoadingBar { get; }

    OmniLoadingBarMode LoadingBarMode { get; }

    string? Height { get; }

    bool FillAvailableHeight { get; }

    string MinHeight { get; }

    string? WheelScrollScope { get; }

    string? MaxHeight { get; }

    // ---- virtualization and state -----------------------------------------------------------------------

    int VirtualizationOverscanCount { get; }

    double? EstimatedRowHeight { get; }

    bool FixedRowHeight { get; }

    OmniDataGridHeaderWrap HeaderWrap { get; }

    int VirtualBlockSize { get; }

    string? StateKey { get; }

    bool? PersistState { get; }

    /// <summary>The id the host gave the grid (<c>OmniComponentBase.Id</c>), or null.</summary>
    string? Id { get; }

    OmniDataGridPlacement FooterPlacement { get; }

    // ---- bars and export --------------------------------------------------------------------------------

    bool ShowHeaderBar { get; }

    bool ShowFooterBar { get; }

    OmniDataGridBarExport HeaderBarExport { get; }

    OmniDataGridBarExport FooterBarExport { get; }

    bool ExportMarkdown { get; }

    bool ExportCsv { get; }

    bool ExportExcel { get; }

    bool ExportPdf { get; }

    string? ExportTitle { get; }

    IReadOnlyList<OmniTableExportField> ExportFields { get; }

    string? ExportFileName { get; }

    IReadOnlyDictionary<OmniTableExportFormat, OmniButtonVariant>? ExportVariants { get; }

    bool ShowExportButtonText { get; }

    OmniControlSize? BarButtonSize { get; }

    int ExportRowLimit { get; }

    int CopyMaxRows { get; }

    Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? ExportLoad { get; }

    EventCallback<OmniTableExportDocument> OnExport { get; }

    EventCallback<Exception> OnExportError { get; }
}
