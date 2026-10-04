using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A data table: paging, virtual scroll, sorting, column filters, grouping, selection, detail rows,
/// inline editing, resizable and frozen columns, from <see cref="Items"/> held in memory or a remote
/// <see cref="Load"/>. Columns are declared with <see cref="OmniDataGridColumn{TItem}"/> in <see cref="Columns"/>.
/// Its texts come from the library resources; a host rewords them through <c>AddOmniEuropeTextOverrides</c>.
/// </summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
public partial class OmniDataGrid<TItem>
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private OmniDataGridContext<TItem> _context = default!;
    private ElementReference _viewport;
    private bool _disposeRequested;

    /// <summary>Creates the grid and the internal collaborators that hold its state and behaviour.</summary>
    public OmniDataGrid()
    {
        ColumnSet = new(this);
        Query = new(this);
        FilterEditor = new(this);
        FilterControls = new(this);
        View = new(this);
        Virtual = new(this);
        Rows = new(this);
        Grouping = new(this);
        Selection = new(this);
        Expansion = new(this);
        Editing = new(this);
        Paging = new(this);
        ColumnLayout = new(this);
        LayoutInterop = new(this);
        Script = new(this);
        FrozenState = new(this);
        Highlight = new(this);
        Persistence = new(this);
        Classes = new(this);
        Export = new(this);
    }

    internal GridColumnSet<TItem> ColumnSet { get; }
    internal GridQueryState<TItem> Query { get; }
    internal GridFilterEditor<TItem> FilterEditor { get; }
    internal GridFilterControls<TItem> FilterControls { get; }
    internal GridDataView<TItem> View { get; }
    internal GridVirtualViewport<TItem> Virtual { get; }
    internal GridRowBuilder<TItem> Rows { get; }
    internal GridGrouping<TItem> Grouping { get; }
    internal GridSelection<TItem> Selection { get; }
    internal GridExpansion<TItem> Expansion { get; }
    internal GridRowEditing<TItem> Editing { get; }
    internal GridPaging<TItem> Paging { get; }
    internal GridColumnLayout<TItem> ColumnLayout { get; }
    internal GridLayoutInterop<TItem> LayoutInterop { get; }
    internal GridScriptBridge<TItem> Script { get; }
    internal GridFrozenState<TItem> FrozenState { get; }
    internal GridNewRowHighlight<TItem> Highlight { get; }
    internal GridStatePersistence<TItem> Persistence { get; }
    internal GridCssClasses<TItem> Classes { get; }
    internal GridExport<TItem> Export { get; }

    [Inject]
    internal IJSRuntime JavaScript { get; set; } = default!;

    [Inject]
    internal IOmniDataGridStateStore? InjectedStateStore { get; set; }

    [Inject]
    internal IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// The rows the grid holds and pages, sorts, filters and groups itself. Ignored when
    /// <see cref="Load"/> is set.
    /// </summary>
    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

    /// <summary>
    /// Remote data: called with the page, the sorts, the filters and a cancellation token each time the
    /// grid needs rows (first render, page change, sort, filter, or a block while virtualizing). A newer
    /// request cancels the previous one. A failure is shown in the grid (<see cref="ErrorContent"/>) with
    /// a retry action and reported through <see cref="OnLoadError"/>.
    /// </summary>
    [Parameter]
    public Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? Load { get; set; }

    /// <summary>The <see cref="OmniDataGridColumn{TItem}"/> declarations. Without any, a single column shows each item as text.</summary>
    [Parameter]
    public RenderFragment? Columns { get; set; }

    /// <summary>Visible caption of the table, which is also its accessible name.</summary>
    [Parameter]
    public string? Caption { get; set; }

    // ---- paging -----------------------------------------------------------------------------

    /// <summary>
    /// The page shown, from 1. The grid keeps its own page and reports every change through
    /// <see cref="PageChanged"/>; a new value from the host moves it.
    /// </summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>Raised with the new page when the reader, a sort, a filter or a shrinking row set changes it.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>
    /// Rows per page, 20 by default; also the block size of a remote virtualized grid unless
    /// <see cref="VirtualBlockSize"/> is set. The grid keeps its own value and reports a change picked in
    /// the pager through <see cref="PageSizeChanged"/>.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 20;

    /// <summary>Raised with the page size the reader picked in the pager.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Page sizes offered in the pager. Empty, the default, hides the selector.</summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

    /// <summary>
    /// How rows beyond the screen are reached: pages with a pager (<see cref="OmniDataGridScrollMode.Paged"/>,
    /// the default), a virtualized continuous scroll (<see cref="OmniDataGridScrollMode.Virtual"/>), or
    /// every row rendered at once (<see cref="OmniDataGridScrollMode.All"/>).
    /// </summary>
    [Parameter]
    public OmniDataGridScrollMode ScrollMode { get; set; } = OmniDataGridScrollMode.Paged;

    /// <summary>Where the pager goes: under the table (the default), above it, or both.</summary>
    [Parameter]
    public OmniDataGridPosition PagerPosition { get; set; } = OmniDataGridPosition.Bottom;

    /// <summary>How the pager's controls are aligned along its row; the start by default.</summary>
    [Parameter]
    public OmniJustification PagerHorizontalAlign { get; set; } = OmniJustification.Start;

    /// <summary>Shows the localized "first to last of total" line under the table, announced politely.</summary>
    [Parameter]
    public bool ShowPagingSummary { get; set; }

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

    /// <summary>Whether rows can be selected, one or several, with a checkbox column. None by default.</summary>
    [Parameter]
    public OmniDataGridSelectionMode SelectionMode { get; set; }

    /// <summary>
    /// A stable identity for a row, used by selection, expansion, editing and the new-row highlight. Null
    /// uses the item itself, which then must compare by value (a record) for the selection to survive a
    /// reload that hands new instances.
    /// </summary>
    [Parameter]
    public Func<TItem, object>? KeyOf { get; set; }

    /// <summary>
    /// How long a row brought in by <see cref="RefreshAsync"/> reads as new (bold), for live data;
    /// null, the default, marks nothing. Rows are told apart by <see cref="KeyOf"/>, so without it
    /// nothing is marked either.
    /// </summary>
    [Parameter]
    public TimeSpan? NewRowHighlight { get; set; }

    /// <summary>
    /// The selected rows (<c>@bind-Value</c>), matched to the rows on screen through <see cref="KeyOf"/>.
    /// The grid keeps its own selection and reports each change through <see cref="ValueChanged"/>; a new
    /// list from the host replaces it. A change keeps the selected rows of other pages.
    /// </summary>
    [Parameter]
    public IReadOnlyList<TItem> Value { get; set; } = Array.Empty<TItem>();

    /// <summary>Raised with the selected rows each time the selection changes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> ValueChanged { get; set; }

    /// <summary>A click (or Enter, Space) on a row also toggles its selection, when <see cref="SelectionMode"/> allows one.</summary>
    [Parameter]
    public bool AllowRowSelectOnRowClick { get; set; }

    /// <summary>
    /// Raised by a click on a data row, or Enter or Space on a focused one, with the row's item and index
    /// and the modifier keys held (Ctrl, Shift...), so a host can select with Ctrl or Shift and act on a
    /// plain click. Activated from the keyboard, a row reports no modifier and no pointer position. Set,
    /// rows become focusable and interactive.
    /// </summary>
    [Parameter]
    public EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowClick { get; set; }

    /// <summary>Raised by a double click on a data row, with the row's item, index and modifier keys.</summary>
    [Parameter]
    public EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; set; }

    /// <summary>
    /// Raised by a right-click on a data row; the browser menu is then not shown there. The event still
    /// bubbles, so an enclosing context menu opens at the pointer. Unset, rows keep the browser menu.
    /// </summary>
    [Parameter]
    public EventCallback<OmniDataGridRowMouseEventArgs<TItem>> OnRowContextMenu { get; set; }

    /// <summary>
    /// Raised by a click on a data cell, with the row's item and index, the cell's column (key, title,
    /// property, index), the value it reads, the modifier keys and the pointer position. The row's own
    /// <see cref="OnRowClick"/> is still raised after it. A pointer event only: the keyboard activates
    /// rows, not cells. Not raised in the control columns nor in a row being edited.
    /// </summary>
    [Parameter]
    public EventCallback<OmniDataGridCellMouseEventArgs<TItem>> OnCellClick { get; set; }

    /// <summary>
    /// Raised by a double click on a data cell, with the same details as <see cref="OnCellClick"/>, for a
    /// drill-down on the figure under the pointer; <see cref="OnRowDoubleClick"/> is still raised after it.
    /// A pointer event only: give keyboard readers another way to the same action (a row click, a button).
    /// </summary>
    [Parameter]
    public EventCallback<OmniDataGridCellMouseEventArgs<TItem>> OnCellDoubleClick { get; set; }

    /// <summary>Called for every rendered row so the host can add a class or veto its controls.</summary>
    [Parameter]
    public Action<OmniDataGridRowRenderArgs<TItem>>? RowRender { get; set; }

    // ---- editing ----------------------------------------------------------------------------

    /// <summary>Whether editing a row closes the row already being edited (the default) or several rows can be edited at once.</summary>
    [Parameter]
    public OmniDataGridRowMode EditMode { get; set; } = OmniDataGridRowMode.Single;

    /// <summary>Raised when a row enters edit mode, from its edit button or <see cref="EditRowAsync"/>.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowEdit { get; set; }

    /// <summary>Raised when a row in edit mode is saved, from its save button or <see cref="UpdateRowAsync"/>. The host persists the change.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowUpdate { get; set; }

    /// <summary>Raised when a row leaves edit mode without saving, from its cancel button or <see cref="CancelEditAsync"/>.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowEditCancel { get; set; }

    // ---- detail rows ------------------------------------------------------------------------

    /// <summary>Content of the detail row opened under an item; set, each row gets an expand button.</summary>
    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    /// <summary>Keys (see <see cref="KeyOf"/>) of the rows whose detail row is open.</summary>
    [Parameter]
    public IReadOnlyList<object> ExpandedKeys { get; set; } = Array.Empty<object>();

    /// <summary>Raised with the keys of the open rows each time a row opens or closes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<object>> ExpandedKeysChanged { get; set; }

    /// <summary>Whether several detail rows can be open at once (the default) or opening one closes the other.</summary>
    [Parameter]
    public OmniDataGridRowMode ExpandMode { get; set; } = OmniDataGridRowMode.Multiple;

    /// <summary>Shows the column of expand buttons when <see cref="DetailTemplate"/> is set. True by default.</summary>
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

    /// <summary>
    /// Puts a button in the expand column header that opens every expandable row on screen, or closes
    /// them. Only offered under <see cref="OmniDataGridRowMode.Multiple"/> <see cref="ExpandMode"/>.
    /// </summary>
    [Parameter]
    public bool ShowExpandAll { get; set; }

    /// <summary>Raised with the item whose detail row was opened.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowExpand { get; set; }

    /// <summary>Raised with the item whose detail row was closed.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowCollapse { get; set; }

    // ---- grouping ---------------------------------------------------------------------------

    /// <summary>Lets the reader group rows by a column from its header; <see cref="Groups"/> is applied only while this is on.</summary>
    [Parameter]
    public bool AllowGrouping { get; set; }

    /// <summary>Shows the band above the table that lists the active groups, each removable.</summary>
    [Parameter]
    public bool ShowGroupPanel { get; set; }

    /// <summary>The active groups, outermost first. Bind it (<c>@bind-Groups</c>) for the header toggles to take effect.</summary>
    [Parameter]
    public IReadOnlyList<OmniDataGridGroup> Groups { get; set; } = Array.Empty<OmniDataGridGroup>();

    /// <summary>Raised with the new groups when the reader adds or removes one.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniDataGridGroup>> GroupsChanged { get; set; }

    /// <summary>Whether groups start open (the default) or closed.</summary>
    [Parameter]
    public bool AllGroupsExpanded { get; set; } = true;

    /// <summary>
    /// Text of a group header from the group's value and its row count; null shows
    /// <c>Title : value (count)</c>.
    /// </summary>
    [Parameter]
    public Func<object?, int, string>? GroupLabel { get; set; }

    // ---- sorting and filtering ----------------------------------------------------------------

    /// <summary>Lets the reader sort by a column from its header (Shift adds a sort). True by default; a column opts out with its own <c>Sortable</c>.</summary>
    [Parameter]
    public bool AllowSorting { get; set; } = true;

    /// <summary>
    /// Gives a filter to every column that reads a value and does not set its own <c>Filterable</c>. True by
    /// default; false leaves a filter only on the columns that declare <c>Filterable="true"</c>.
    /// </summary>
    [Parameter]
    public bool Filterable { get; set; } = true;

    /// <summary>
    /// How a column filter is built: a value only (the default), an operator and a value, or two
    /// conditions joined by and/or applied with a button.
    /// </summary>
    [Parameter]
    public OmniDataGridFilterMode FilterMode { get; set; } = OmniDataGridFilterMode.Simple;

    /// <summary>
    /// Moves each column's filter into a popover anchored to its header, in place of the inline filter
    /// row, which is then not rendered: the header stays one row. The popover holds the same editor
    /// the row would, for the current <see cref="FilterMode"/> (two conditions with apply and clear in
    /// <see cref="OmniDataGridFilterMode.Advanced"/>). On by default (recette R-041): a grid that
    /// wants the inline filter row under its header sets it to false.
    /// </summary>
    [Parameter]
    public bool ShowHeaderFilterMenu { get; set; } = true;

    /// <summary>
    /// Closes a header filter menu as soon as a value is picked in it. A click outside the menu, or
    /// the Escape key, always closes it whatever this is set to.
    /// </summary>
    [Parameter]
    public bool HideFilterMenuOnSelect { get; set; }

    /// <summary>
    /// Makes text filters tell capitals from lower case: "Paris" then no longer matches "paris".
    /// Off by default, filters ignore case. <see cref="IgnoreDiacritics"/> is independent of it.
    /// </summary>
    [Parameter]
    public bool CaseSensitiveFilters { get; set; }

    /// <summary>
    /// Compares filters with accents stripped from both sides, so a search for "epee" matches the
    /// accented spelling of the same word. A host that filters its own rows behind <see cref="Load"/>
    /// applies the same rule through <see cref="OmniDataGridFilterText.Normalize"/>.
    /// </summary>
    [Parameter]
    public bool IgnoreDiacritics { get; set; }

    // ---- presentation -------------------------------------------------------------------------

    /// <summary>Gives each column a drag handle on its trailing edge (and arrow keys on it). True by default; a column opts out with <c>Resizable</c>.</summary>
    [Parameter]
    public bool AllowColumnResize { get; set; } = true;

    /// <summary>
    /// Excel's double click on a column's trailing edge: the column takes the width of its widest
    /// content, header included, measured over every loaded row, the virtualized rows off screen
    /// included. Off by default. A column's own <c>AutoFit</c> overrides this in either direction, and
    /// the drag governed by <see cref="AllowColumnResize"/> stays separate.
    /// </summary>
    [Parameter]
    public bool AllowColumnAutoFit { get; set; }

    /// <summary>
    /// Raised with the column key and its new CSS width once a resize ends: a drag, a fit to content or
    /// an arrow key on the handle.
    /// </summary>
    [Parameter]
    public EventCallback<OmniDataGridColumnWidthChange> OnColumnResize { get; set; }

    /// <summary>Tints every other row.</summary>
    [Parameter]
    public bool AllowAlternatingRows { get; set; }

    /// <summary>Tints a whole column, header included, while it carries a sort or a filter.</summary>
    [Parameter]
    public bool HighlightActiveColumn { get; set; }

    /// <summary>Tints the row under the pointer.</summary>
    [Parameter]
    public bool HighlightRowOnHover { get; set; }

    /// <summary>Which rules separate the cells; a rule under each row by default.</summary>
    [Parameter]
    public OmniDataGridLines GridLines { get; set; } = OmniDataGridLines.Horizontal;

    /// <summary>Row height and cell padding; comfortable by default.</summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>Stacks each row into a labelled card once the viewport is too narrow for a table.</summary>
    [Parameter]
    public bool Responsive { get; set; }

    /// <summary>
    /// What a grid without rows says when no filter is active; null shows the localized "no row". A grid
    /// emptied by its filters always says so instead.
    /// </summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>
    /// Rendered in the empty-state cell instead of <see cref="EmptyText"/>, for an empty state with an
    /// icon, a description or an action. Left null, the text is shown exactly as before.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Content displayed inside the table, under the column headers, while a grid with no row yet loads
    /// and while the images of the rows are awaited. With <see cref="ShowLoadingBar"/> off it replaces the
    /// rows during every load.
    /// </summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// Replaces the default failure message ("loading failed", followed by a retry button) when
    /// <see cref="Load"/> throws; receives the exception. The retry button stays.
    /// </summary>
    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>
    /// Raised with the exception when <see cref="Load"/> fails (a cancelled request is not a failure).
    /// The grid still shows its error state; this is where the host logs or reports it.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnLoadError { get; set; }

    /// <summary>
    /// The host is loading the grid's data itself (a grid fed through <see cref="Items"/>): the grid
    /// reads as busy and shows the same loading indicator as for its own requests, the bar of
    /// <see cref="ShowLoadingBar"/> by default.
    /// </summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>
    /// Whether every load of the grid, its own requests (first load, page, sort, filter, a block while
    /// virtualizing) as well as a host load signalled by <see cref="Busy"/>, shows a bar between the headers
    /// and the first row, on the table only: the rows already there stay in place, and a grid with no row
    /// yet keeps its body empty under the bar (<see cref="LoadingContent"/> when set). True by default;
    /// false replaces the rows with a loading row (<see cref="LoadingContent"/> or the localized
    /// "loading") during every load instead.
    /// </summary>
    [Parameter]
    public bool ShowLoadingBar { get; set; } = true;

    /// <summary>How that bar reports the request: a sweep by default, or a bar that keeps filling.</summary>
    [Parameter]
    public OmniLoadingBarMode LoadingBarMode { get; set; } = OmniLoadingBarMode.Sweep;

    /// <summary>
    /// Height of the scrolling table as a CSS length, for example <c>600px</c>, <c>50vh</c> or
    /// <c>100%</c>. Left unset the table grows with its content, or fills its parent while
    /// virtualizing.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

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
    /// Opt-in: a CSS selector naming an ancestor of the grid, for example <c>.page-content</c>. A vertical
    /// wheel turn anywhere over that ancestor scrolls the grid's rows, so a reader does not have to aim
    /// at the table first. A turn over the grid itself, over another area that can still scroll that
    /// way, with Shift held (horizontal intent) or with Ctrl held (zoom) keeps its native behavior, and
    /// once the rows reach their end the page is left alone rather than scrolled. Unset by default.
    /// </summary>
    [Parameter]
    public string? WheelScrollScope { get; set; }

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

    /// <summary>Rows rendered beyond each edge of the viewport while virtualizing, so a short scroll shows no gap. 3 by default.</summary>
    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 3;

    /// <summary>
    /// Starting height assumed for a row that has not been measured yet, in pixels; with
    /// <see cref="FixedRowHeight"/>, the exact height of every row.
    /// </summary>
    [Parameter]
    public double EstimatedRowHeight { get; set; } = 40d;

    /// <summary>
    /// Gives every row exactly <see cref="EstimatedRowHeight"/>: rows are never measured, the scroll
    /// math uses that height alone, and the rows are drawn at it (uniform rows, overflow clipped with
    /// an ellipsis). For homogeneous data sets. Off by default: rows are measured.
    /// </summary>
    [Parameter]
    public bool FixedRowHeight { get; set; }

    /// <summary>
    /// What a column title does when it is wider than its column: run onto a second line, growing
    /// the header band, or be cut with an ellipsis and keep it one line tall.
    /// </summary>
    [Parameter]
    public OmniDataGridHeaderWrap HeaderWrap { get; set; } = OmniDataGridHeaderWrap.Wrap;

    /// <summary>Rows fetched per remote request while virtualizing. Zero, the default, uses <see cref="PageSize"/>.</summary>
    [Parameter]
    public int VirtualBlockSize { get; set; }

    // ---- state persistence --------------------------------------------------------------------

    /// <summary>
    /// Opaque key this grid's filters, sorts and column widths are saved under. Persistence is
    /// inert until this is set: that is the option's activation switch. The default browser store
    /// cannot be read while the grid is prerendered; the state is then restored by the interactive
    /// render. A saved entry missing a part, or holding an incomplete filter or sort, restores what
    /// is valid and drops the rest.
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


    /// <summary>
    /// Where the footer row goes (the columns' <c>FooterContent</c>): in a real table footer after the
    /// rows (the default), directly under the header, or both.
    /// </summary>
    [Parameter]
    public OmniDataGridPosition FooterPosition { get; set; }

    // ---- export ---------------------------------------------------------------------------------

    /// <summary>
    /// The formats of the export bar: each button exports every row the filters in force select, in the
    /// current sort order, not only the page or window on screen. Empty, the default, shows no bar. A
    /// format nobody writes (no <see cref="IOmniTableExportRenderer"/> of the host supports it) has no
    /// button; Markdown and CSV are written by the package. The columns exported are the visible ones
    /// that read a value (see <see cref="OmniDataGridColumn{TItem}.ExportValue"/>).
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniTableExportFormat> ExportFormats { get; set; } = Array.Empty<OmniTableExportFormat>();

    /// <summary>Where the export bar goes: under the table (the default), above it, or both.</summary>
    [Parameter]
    public OmniDataGridPosition ExportPosition { get; set; }

    /// <summary>The title of the exported document; null uses <see cref="Caption"/>, then a localized "Table export".</summary>
    [Parameter]
    public string? ExportTitle { get; set; }

    /// <summary>
    /// Header lines of the exported document that say where the rows come from (the application, the
    /// period). The active column filters are added after them by the grid.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniTableExportField> ExportFields { get; set; } = Array.Empty<OmniTableExportField>();

    /// <summary>
    /// The exported file's name without extension, best the application then the content
    /// (<c>shop-logs</c>); the generation time (UTC) and the format's extension are appended, as in
    /// <c>shop-logs-2026-10-01-0840.md</c>. Null, the default, derives it from <see cref="ExportTitle"/>,
    /// then <see cref="Caption"/>, in lowercase without accents and with hyphens between words
    /// (<c>export</c> when neither is set).
    /// </summary>
    [Parameter]
    public string? ExportFileName { get; set; }

    /// <summary>
    /// The variant of each format's button in the export bar; a format absent from it is
    /// <see cref="OmniButtonVariant.Secondary"/> (neutral grey), an export being one of the other actions of its zone. Each button carries
    /// the file icon of its format (<see cref="OmniIconName.FileMd"/>, <see cref="OmniIconName.FileCsv"/>,
    /// <see cref="OmniIconName.FileXls"/>, <see cref="OmniIconName.FilePdf"/>).
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<OmniTableExportFormat, OmniButtonVariant>? ExportVariants { get; set; }

    /// <summary>Most rows an export reads, 5000 by default; beyond it the bar says the export is truncated.</summary>
    [Parameter]
    public int ExportRowLimit { get; set; } = 5000;

    /// <summary>
    /// Reads the rows of an export, in place of <see cref="Load"/>: asked page after page, 200 rows at a
    /// time, with the sorts and filters in force. Set it when <see cref="Load"/> does more than answer
    /// its request (it keeps the page it served, say), which an export must not disturb. Null, the
    /// default, exports through <see cref="Load"/>, or from <see cref="Items"/> when there is none.
    /// </summary>
    [Parameter]
    public Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? ExportLoad { get; set; }

    /// <summary>Raised with the exported document after its file was handed to the browser.</summary>
    [Parameter]
    public EventCallback<OmniTableExportDocument> OnExport { get; set; }

    /// <summary>
    /// Raised with the exception when an export fails (a page that could not be read, a renderer that
    /// threw); no file is produced and the bar says the export failed. A cancelled export is not a failure.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnExportError { get; set; }

    internal bool DisposeRequested => _disposeRequested;

    internal ElementReference Viewport => _viewport;

    /// <summary>The identity of a row: <see cref="KeyOf"/>, or the item itself.</summary>
    internal object ItemKey(TItem item) => KeyOf is not null ? KeyOf(item) : item!;

    internal void Render() => StateHasChanged();

    internal Task RenderLaterAsync() => InvokeAsync(StateHasChanged);

    internal Task DispatchAsync(Func<Task> work) => InvokeAsync(work);

    internal string Text(string name, params object[] arguments) => Localize(name, arguments);

    internal DateTimeOffset Now() => Clock.GetUtcNow();

    /// <summary>Narrowest the table may become, as a CSS length (see <see cref="GridColumnLayout{TItem}.TableMinimumWidth"/>).</summary>
    internal string TableMinimumWidth() => ColumnLayout.TableMinimumWidth();

    private sealed record FilterEditorRequest(OmniDataGridColumnDefinition<TItem> Column, string Id, bool InPanel);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _context = new OmniDataGridContext<TItem>
        {
            Register = ColumnSet.Register,
            Unregister = ColumnSet.Unregister,
            DelegatesAdopted = ColumnSet.RenderAdoptedDelegates
        };
        MirrorParameters();
    }

    /// <summary>
    /// Reads the saved state. A store that answers at once is read before the first render; a slower one
    /// lets the grid render first, so the child <see cref="OmniDataGridColumn{TItem}"/> content registers
    /// its defaults and the restored filters, sorts and column widths then replace them. The first
    /// request waits for both.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        if (StateKey is { } key)
        {
            await Persistence.LoadAsync(key);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (View.Virtualized && Load is not null && (DetailTemplate is not null || Grouping.ActiveGroups.Count > 0))
        {
            throw new InvalidOperationException(
                "OmniDataGrid cannot virtualize a remote (Load) grid that also declares Groups or DetailTemplate: "
                + "groups and detail rows need the whole row set, which a remote source only loads block by block. "
                + "Use Items for a grouped or detailed virtualized grid.");
        }

        MirrorParameters();
    }

    /// <summary>
    /// Takes the page, page size and selection the host passes when they are new values. Also run from
    /// <see cref="OnInitialized"/>: a state restored asynchronously renders the grid once before
    /// <see cref="OnParametersSet"/>, and that render must already use the host's values.
    /// </summary>
    private void MirrorParameters()
    {
        Paging.Mirror();
        Selection.Mirror();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ColumnSet.ResetImplicitColumn();
        View.InvalidateLocalProjection();
        View.ObserveItems();
        await View.ObserveLoaderAsync();
        RebuildRenderSnapshot();
        if (View.Virtualized)
        {
            await View.BootstrapVirtualizationAsync();
            return;
        }

        await Paging.ClampToLastPageAsync();
    }

    /// <summary>
    /// Recomputes what the next render reads: the visible columns, the selected and expanded keys, the
    /// slots and, while virtualizing, the window.
    /// </summary>
    internal void RebuildRenderSnapshot()
    {
        ColumnSet.Refresh();
        Selection.Reindex();
        Expansion.Reindex();
        Rows.ForgetSlots();
        if (View.Virtualized)
        {
            Virtual.Sync();
        }
    }

    /// <summary>
    /// Sets column filters from code (a summary shortcut, a link) exactly as the user would in the
    /// column headers, then reloads once. A null or empty value clears that column. With
    /// <paramref name="replace"/> every other filter is cleared too. A key whose column has not
    /// rendered yet is kept and applies when it does.
    /// </summary>
    /// <param name="values">Filter value per column key; the column's default operator applies (equality for a key without a column yet).</param>
    /// <param name="replace">True to clear every filter not in <paramref name="values"/> first.</param>
    /// <returns>A task that completes when the grid has reloaded and saved its state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    public async Task SetFiltersAsync(IReadOnlyDictionary<string, string?> values, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(values);
        Query.SetFilters(values, replace);
        await View.QueryChangedAsync();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The columns registered during this first render: the first request of a Load grid waits for
        // it, so it already carries their default filters and sorts.
        if (firstRender)
        {
            await View.ColumnsRenderedAsync();
        }

        await _lifecycleGate.WaitAsync();
        try
        {
            if (!_disposeRequested)
            {
                await Script.AfterRenderAsync();
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>
    /// Invoked by the grid script when the viewport leaves its horizontal start or comes back to it.
    /// Coming back ends a detachment: the frozen columns are frozen again and the control hides.
    /// </summary>
    /// <param name="scrolled">True when the viewport has left its horizontal start.</param>
    /// <returns>A completed task.</returns>
    [JSInvokable]
    public Task OnHorizontalScrollChangedAsync(bool scrolled) => FrozenState.HorizontalScrollChangedAsync(scrolled);

    /// <summary>
    /// Invoked by the grid script when the rendered rows changed size without any scroll (an image or a
    /// font arrived, a column was narrowed). Rows are only measured after a render, so one is asked for:
    /// the new heights move the spacers and the end of the list. A render that changes nothing leaves the
    /// table the same size, so the script does not ask again.
    /// </summary>
    [JSInvokable]
    public Task OnContentResizedAsync()
    {
        if (View.Virtualized)
        {
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by the grid script when the viewport is scrolled or resized. Loads and renders the rows of
    /// the new window only when the window moved.
    /// </summary>
    /// <param name="scrollTop">Vertical scroll offset of the viewport, in CSS pixels.</param>
    /// <param name="viewportHeight">Height of the viewport, in CSS pixels.</param>
    /// <returns>A task that completes when the rows of the new window are loaded.</returns>
    [JSInvokable]
    public Task OnViewportChangedAsync(double scrollTop, double viewportHeight) =>
        Virtual.ViewportChangedAsync(scrollTop, viewportHeight);

    /// <summary>
    /// Scrolls the virtualized viewport so that <paramref name="index"/> sits at its top edge. Does
    /// nothing when the grid is not virtualized or its script is not loaded yet.
    /// </summary>
    /// <param name="index">Zero-based index of the row in the virtualized rows.</param>
    /// <returns>A task that completes when the scroll was requested.</returns>
    public async Task ScrollToIndexAsync(int index)
    {
        if (!View.Virtualized || Script.Module is null)
        {
            return;
        }

        await Script.ScrollToOffsetAsync(Virtual.OffsetOf(index));
    }

    /// <summary>
    /// Puts a row in edit mode, honouring <see cref="EditMode"/> (in single mode the row previously
    /// edited leaves edit mode without any callback), then raises <see cref="OnRowEdit"/>.
    /// </summary>
    /// <param name="item">The row to edit, found by its key.</param>
    /// <returns>A task that completes when <see cref="OnRowEdit"/> has run.</returns>
    public Task EditRowAsync(TItem item) => Editing.EditAsync(item);

    /// <summary>Closes the edit state of a row and reports the update through <see cref="OnRowUpdate"/>.</summary>
    /// <param name="item">The edited row, found by its key.</param>
    /// <returns>A task that completes when <see cref="OnRowUpdate"/> has run.</returns>
    public Task UpdateRowAsync(TItem item) => Editing.UpdateAsync(item);

    /// <summary>Closes the edit state of a row without reporting an update, and raises <see cref="OnRowEditCancel"/>.</summary>
    /// <param name="item">The edited row, found by its key.</param>
    /// <returns>A task that completes when <see cref="OnRowEditCancel"/> has run.</returns>
    public Task CancelEditAsync(TItem item) => Editing.CancelAsync(item);

    /// <summary>
    /// Fetches the rows again without leaving them, for live data (a row created, changed or
    /// removed on the server): no loading state, the scroll position and the page stay, and the
    /// rows on screen are replaced only once the new ones are in, so a new row slides in and moves
    /// the others down. A sort or filter change still restarts from the top through
    /// <see cref="ReloadAsync"/>. With <see cref="NewRowHighlight"/>, rows that were not held
    /// before read as new for that long. For a grid fed through <c>Items</c>, call it before handing
    /// the new rows: they are compared to the ones held at the call.
    /// </summary>
    public Task RefreshAsync() => View.RefreshAsync();

    /// <summary>
    /// Fetches the rows of a <see cref="Load"/> grid again from the start of the current query, with the
    /// loading state (the bar by default); a virtualized grid starts again from its first block. Does
    /// nothing for a grid fed through <see cref="Items"/>.
    /// </summary>
    public Task ReloadAsync() => View.ReloadAsync();

    /// <summary>
    /// Text of the column for every loaded row, so omni-grid.js can size the column on rows the
    /// virtualization never put in the page. Null for a templated column: its rendering is not a
    /// text .NET can produce, so only its rendered rows are measured. Distinct values only, the
    /// width depends on the text and not on how many rows carry it.
    /// </summary>
    /// <param name="key">Key of the column being fitted.</param>
    /// <returns>
    /// The distinct non-empty cell texts, or <c>null</c> when the column is not visible, does not fit
    /// to content, or has a template.
    /// </returns>
    [JSInvokable]
    public string[]? GetColumnAutoFitTexts(string key) => ColumnLayout.AutoFitTexts(key);

    /// <summary>
    /// Width chosen by a fit to content, in CSS pixels measured by omni-grid.js. Reported once per
    /// gesture, then applied, persisted and announced like the end of a drag.
    /// </summary>
    /// <param name="key">Key of the fitted column; ignored when that column is not visible or does not fit to content.</param>
    /// <param name="width">Measured width in CSS pixels, raised to 48 when smaller.</param>
    /// <returns>A task that completes when the width is applied, announced and saved.</returns>
    [JSInvokable]
    public Task OnColumnAutoFitAsync(string key, double width) => ColumnLayout.AutoFitAsync(key, width);

    /// <summary>
    /// Final width of a pointer drag on a column's resize handle, in CSS pixels measured by
    /// omni-grid.js. The gesture itself never round-trips to .NET; only its outcome does.
    /// </summary>
    /// <param name="key">Key of the resized column; ignored when resizing is off or that column is not visible.</param>
    /// <param name="width">Final width in CSS pixels, raised to 48 when smaller.</param>
    /// <returns>A task that completes when the width is applied, announced and saved.</returns>
    [JSInvokable]
    public Task OnColumnResizedAsync(string key, double width) => ColumnLayout.ResizedAsync(key, width);

    /// <summary>Cancels the loads still running and detaches the grid script.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposeRequested = true;
        // Cancelled before waiting for the gate: a render holding it may be awaiting a load that only
        // this cancellation ends, and a loader that ignores its token is no longer awaited once cancelled.
        View.CancelLoads();
        await Script.CancelPreparationAsync();
        await _lifecycleGate.WaitAsync();
        try
        {
            await Script.DisposeAsync();
            await Highlight.DisposeAsync();
            await Export.DisposeAsync();
            Script.ReleasePreparation();
            await View.DisposeAsync();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }
}
