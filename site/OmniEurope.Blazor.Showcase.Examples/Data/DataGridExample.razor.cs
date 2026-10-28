using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>
/// The flights grid with every common setting as a parameter: data, paging, filtering, rows, columns and
/// presentation. It echoes the selection, the last row event and the state of the grid under the table.
/// </summary>
public partial class DataGridExample
{
    /// <summary>The fixed height of a row when the setting is on, in pixels.</summary>
    private const double FixedRowHeightPixels = 44;

    private static readonly IReadOnlyList<int> PagerPageSizes = [5, 10, 20, 50];

    private readonly IReadOnlyList<Flight> _flights = FlightData.Create();
    private readonly Dictionary<int, (FlightStatus Status, string Gate)> _editSnapshots = [];
    private int _seenPageSize;
    private int _pageSize = 10;

    // ---- data -------------------------------------------------------------------------------------

    /// <summary>Renders only the rows on screen; paging, grouping and detail rows step aside.</summary>
    [Parameter]
    public bool Virtualize { get; set; } = true;

    /// <summary>Gives the grid no flight, to show its empty text.</summary>
    [Parameter]
    public bool EmptyData { get; set; }

    /// <summary>Marks the grid busy, as while a request is in flight.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Whether a busy grid draws the loading bar over its header.</summary>
    [Parameter]
    public bool ShowLoadingBar { get; set; } = true;

    /// <summary>How the loading bar animates.</summary>
    [Parameter]
    public OmniLoadingBarMode LoadingBarMode { get; set; } = OmniLoadingBarMode.Sweep;

    // ---- paging -----------------------------------------------------------------------------------

    /// <summary>Pages the rows; off, the grid renders them all at once.</summary>
    [Parameter]
    public bool AllowPaging { get; set; } = true;

    /// <summary>The rows per page.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Raised when the visitor picks another page size in the pager.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Where the pager sits.</summary>
    [Parameter]
    public OmniDataGridPlacement PagerPlacement { get; set; } = OmniDataGridPlacement.Bottom;

    /// <summary>How the parts of the pager are spread along its row.</summary>
    [Parameter]
    public OmniJustification PagerJustify { get; set; } = OmniJustification.Between;

    /// <summary>How many numbered page buttons the pager shows; 0 hides them.</summary>
    [Parameter]
    public int NumericPageCount { get; set; } = 5;

    /// <summary>Whether the pager offers a choice of page size.</summary>
    [Parameter]
    public bool ShowPageSizeOptions { get; set; } = true;

    /// <summary>Whether the pager writes the first and last row shown out of the total.</summary>
    [Parameter]
    public bool ShowPagingSummary { get; set; } = true;

    /// <summary>Whether the pager stays when everything fits on one page.</summary>
    [Parameter]
    public bool AlwaysShowPager { get; set; } = true;

    // ---- sorting and filters ----------------------------------------------------------------------

    /// <summary>Sorts a column when its title is clicked.</summary>
    [Parameter]
    public bool AllowSorting { get; set; } = true;

    /// <summary>Shows a filter under each title.</summary>
    [Parameter]
    public bool Filterable { get; set; } = true;

    /// <summary>How the filters are offered: inline, with a menu, or with an advanced panel.</summary>
    [Parameter]
    public OmniDataGridFilterMode FilterMode { get; set; } = OmniDataGridFilterMode.Simple;

    /// <summary>Whether each title carries a filter menu button.</summary>
    [Parameter]
    public bool ShowHeaderFilterMenu { get; set; }

    /// <summary>Whether the filter menu closes once a value is picked.</summary>
    [Parameter]
    public bool HideFilterMenuOnSelect { get; set; } = true;

    /// <summary>Whether filters tell upper from lower case.</summary>
    [Parameter]
    public bool CaseSensitiveFilters { get; set; }

    /// <summary>Whether filters ignore accents: "geneve" finds Genève.</summary>
    [Parameter]
    public bool IgnoreDiacritics { get; set; } = true;

    // ---- rows -------------------------------------------------------------------------------------

    /// <summary>Whether rows are selected one at a time, several at a time, or not at all.</summary>
    [Parameter]
    public OmniDataGridSelectionMode SelectionMode { get; set; } = OmniDataGridSelectionMode.Multiple;

    /// <summary>Whether a click anywhere on a row selects it.</summary>
    [Parameter]
    public bool SelectOnRowClick { get; set; }

    /// <summary>Makes a cancelled flight neither selectable nor expandable, and reads it struck out.</summary>
    [Parameter]
    public bool LockCancelled { get; set; } = true;

    /// <summary>Adds the pencil column that opens a row for editing.</summary>
    [Parameter]
    public bool ShowEditColumn { get; set; } = true;

    /// <summary>Whether one row or several can be open for editing at once.</summary>
    [Parameter]
    public OmniDataGridRowMode EditMode { get; set; } = OmniDataGridRowMode.Single;

    /// <summary>Adds a detail panel under each flight, opened by a chevron.</summary>
    [Parameter]
    public bool ShowDetails { get; set; } = true;

    /// <summary>Whether one detail or several can be open at once.</summary>
    [Parameter]
    public OmniDataGridRowMode ExpandMode { get; set; } = OmniDataGridRowMode.Multiple;

    /// <summary>Adds a header button that opens or closes every detail of the page.</summary>
    [Parameter]
    public bool ShowExpandAll { get; set; } = true;

    /// <summary>Lets the visitor group the rows by a column.</summary>
    [Parameter]
    public bool AllowGrouping { get; set; } = true;

    /// <summary>Shows the panel the grouping columns are dropped in.</summary>
    [Parameter]
    public bool ShowGroupPanel { get; set; } = true;

    /// <summary>Whether groups start open.</summary>
    [Parameter]
    public bool AllGroupsExpanded { get; set; } = true;

    // ---- columns ----------------------------------------------------------------------------------

    /// <summary>Lets the visitor drag the edge of a title to resize its column.</summary>
    [Parameter]
    public bool AllowColumnResize { get; set; } = true;

    /// <summary>Lets the visitor drag a title to move its column.</summary>
    [Parameter]
    public bool AllowColumnReorder { get; set; } = true;

    /// <summary>Lets the visitor hide columns from the chooser of the header bar.</summary>
    [Parameter]
    public bool AllowColumnHide { get; set; } = true;

    /// <summary>Offers to reset the layout of the columns.</summary>
    [Parameter]
    public bool ShowResetLayout { get; set; }

    /// <summary>Writes the name of each export format beside its icon.</summary>
    [Parameter]
    public bool ShowExportButtonText { get; set; } = true;

    /// <summary>Freezes the flight number: it stays in place while the table scrolls sideways.</summary>
    [Parameter]
    public bool FreezeNumber { get; set; } = true;

    /// <summary>Whether a long title wraps or is cut.</summary>
    [Parameter]
    public OmniDataGridHeaderWrap HeaderWrap { get; set; } = OmniDataGridHeaderWrap.Wrap;

    /// <summary>Tints the sorted or filtered column, header included.</summary>
    [Parameter]
    public bool HighlightActiveColumn { get; set; } = true;

    // ---- look -------------------------------------------------------------------------------------

    /// <summary>The height of the rows.</summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Compact;

    /// <summary>Which rules separate the cells.</summary>
    [Parameter]
    public OmniDataGridLines GridLines { get; set; } = OmniDataGridLines.Horizontal;

    /// <summary>Tints every other row.</summary>
    [Parameter]
    public bool AlternatingRows { get; set; } = true;

    /// <summary>Tints the row under the pointer.</summary>
    [Parameter]
    public bool HighlightRowOnHover { get; set; } = true;

    /// <summary>Gives every row the same fixed height.</summary>
    [Parameter]
    public bool FixedRowHeight { get; set; }

    /// <summary>Where the total row sits: under the table or right below the titles.</summary>
    [Parameter]
    public OmniDataGridPlacement FooterPlacement { get; set; } = OmniDataGridPlacement.Bottom;

    /// <summary>Lets the grid reshape itself on a narrow screen.</summary>
    [Parameter]
    public bool Responsive { get; set; }

    // ---- state of the grid ------------------------------------------------------------------------

    private int CurrentPage { get; set; } = 1;

    private IReadOnlyList<Flight> SelectedFlights { get; set; } = [];

    private IReadOnlyList<OmniDataGridGroup> Groups { get; set; } = [];

    private IReadOnlyList<object> ExpandedKeys { get; set; } = [];

    private string? LastActivity { get; set; }

    private IReadOnlyList<Flight> GridItems => EmptyData ? Array.Empty<Flight>() : _flights;

    private IReadOnlyList<int> PageSizeChoices => ShowPageSizeOptions ? PagerPageSizes : Array.Empty<int>();

    /// <summary>The paged grid pages its rows, or renders them all once paging is switched off.</summary>
    private OmniDataGridScrollMode PagedScrollMode => AllowPaging ? OmniDataGridScrollMode.Paged : OmniDataGridScrollMode.All;

    /// <summary>The detail panel of a flight, or nothing: without a template the grid draws neither the expand column nor its header button.</summary>
    private RenderFragment<Flight>? DetailTemplate => ShowDetails ? FlightDetail : null;

    private string FlightCountText => $"{GridItems.Count} flights";

    private string SelectedCountText => $"{SelectedFlights.Count} selected";

    private string CancelledCountText => $"{GridItems.Count(flight => flight.Status == FlightStatus.Cancelled)} cancelled";

    private string SelectionEcho
    {
        get
        {
            var numbers = SelectedFlights.Select(flight => flight.Number).ToList();
            return numbers.Count == 0
                ? "No flight selected."
                : string.Join(", ", numbers.Take(12)) + (numbers.Count > 12 ? ", ..." : string.Empty);
        }
    }

    private string ActivityEcho => LastActivity ?? "No row event yet.";

    private string StateEcho => $"Page {CurrentPage}, {_pageSize} rows per page, {Groups.Count} grouping(s), {ExpandedKeys.Count} open detail(s).";

    protected override void OnParametersSet()
    {
        // A new page size from the settings brings the grid back to its first page: the old one may no longer exist.
        if (PageSize != _seenPageSize)
        {
            _seenPageSize = PageSize;
            _pageSize = PageSize;
            CurrentPage = 1;
        }
    }

    private static RenderFragment FlightDetail(Flight flight) => builder =>
    {
        builder.OpenComponent<DataGridFlightDetail>(0);
        builder.AddComponentParameter(1, nameof(DataGridFlightDetail.Flight), flight);
        builder.CloseComponent();
    };

    private static object FlightKey(Flight flight) => flight.Id;

    private async Task ChangePageSize(int size)
    {
        _pageSize = size;
        _seenPageSize = size;
        CurrentPage = 1;
        await PageSizeChanged.InvokeAsync(size);
    }

    /// <summary>A cancelled flight can be neither selected nor opened, and reads struck out.</summary>
    private void RenderRow(OmniDataGridRowRenderArgs<Flight> args)
    {
        if (!LockCancelled || args.Item.Status != FlightStatus.Cancelled)
        {
            return;
        }

        args.Selectable = false;
        args.Expandable = false;
        args.Class = "flight-cancelled";
    }

    /// <summary>The Markdown export reads every row of the grid page by page, not the window shown.</summary>
    private OmniTableExport<Flight> CreateExport() => new()
    {
        Title = "Flights export",
        Columns =
        [
            new("Flight", flight => flight.Number),
            new("Airline", flight => flight.Airline),
            new("Origin", flight => flight.Origin),
            new("Destination", flight => flight.Destination),
            new("Departure", flight => flight.Departure.ToString("g", CultureInfo.CurrentCulture)),
            new("Gate", flight => flight.Gate)
        ],
        Fields = [new("Source", "Departures")],
        RowKey = flight => flight.Id,
        LoadPage = request =>
        {
            var items = GridItems;
            return Task.FromResult(new OmniDataGridResult<Flight>(items.Skip(request.Skip).Take(request.PageSize).ToList(), items.Count));
        }
    };

    private void OnExported(OmniTableExportDocument document) =>
        LastActivity = $"{document.RowCount} rows exported to Markdown.";

    private void OnRowClick(OmniDataGridRowMouseEventArgs<Flight> args) => LastActivity = $"Click on flight {args.Item.Number}.";

    // Listening to the right click removes the browser menu on rows: the host opens its own.
    private void OnRowContextMenu(OmniDataGridRowMouseEventArgs<Flight> args) => LastActivity = $"Right click on flight {args.Item.Number}.";

    private void OnRowDoubleClick(OmniDataGridRowMouseEventArgs<Flight> args) => LastActivity = $"Double click on flight {args.Item.Number}.";

    private void OnRowExpanded(Flight flight) => LastActivity = $"Details of flight {flight.Number} expanded.";

    private void OnRowCollapsed(Flight flight) => LastActivity = $"Details of flight {flight.Number} collapsed.";

    private void OnColumnResized(OmniDataGridColumnWidthChange change) =>
        LastActivity = string.Create(CultureInfo.CurrentCulture, $"Column {change.Key} resized to {change.Width}.");

    // The grid binds the editors straight to the row: a cancelled edit puts back the values taken when
    // it opened, or the row would keep what was typed.
    private void OnEditRequested(Flight flight)
    {
        _editSnapshots[flight.Id] = (flight.Status, flight.Gate);
        LastActivity = $"Flight {flight.Number} opened for editing.";
    }

    private void OnRowUpdated(Flight flight)
    {
        _editSnapshots.Remove(flight.Id);
        LastActivity = $"Flight {flight.Number} saved.";
    }

    private void OnEditCancelled(Flight flight)
    {
        if (_editSnapshots.Remove(flight.Id, out var snapshot))
        {
            flight.Status = snapshot.Status;
            flight.Gate = snapshot.Gate;
        }

        LastActivity = $"Editing of flight {flight.Number} cancelled, values restored.";
    }
}
