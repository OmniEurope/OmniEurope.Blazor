using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataListDemo
{
    private static readonly IReadOnlyList<int> Sizes = [2, 4, 8];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<OmniKanbanColumn> WorkflowColumns { get; set; } = [];

    private IReadOnlyList<GridRow> Rows { get; set; } = [];

    private IReadOnlyList<GridRow> KanbanRows { get; set; } = [];

    private IReadOnlyList<GridRow> ManyRows { get; set; } = [];

    private IReadOnlyList<OmniLogLine> LogLines { get; set; } = [];

    private Dictionary<string, string> WorkflowPositions { get; } = new()
    {
        ["D-2401"] = "new", ["D-2402"] = "review", ["D-2403"] = "done"
    };

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = 2;

    private int PageCount => (int)Math.Ceiling(Rows.Count / (double)PageSize);

    private IReadOnlyList<GridRow> Page => [.. Rows.Skip((PageNumber - 1) * PageSize).Take(PageSize)];

    protected override void OnInitialized()
    {
        WorkflowColumns =
        [
            new("new", Text["DemoDataListColumnNew"]),
            new("review", Text["DemoDataListColumnReview"]),
            new("done", Text["DemoDataListColumnDone"])
        ];
        Rows =
        [
            new("D-2401", "Camille Durand", Text["DemoGridCountryBelgium"], 12400),
            new("D-2402", "Jonas Meyer", Text["DemoGridCountryLuxembourg"], 8600),
            new("D-2403", "Sofia Rossi", Text["DemoGridCountryFrance"], 21500),
            new("D-2404", "Lars Jansen", Text["DemoGridCountryNetherlands"], 4300),
            new("D-2405", "Ana Silva", Text["DemoGridCountryFrance"], 17800),
            new("D-2406", "Piet de Vries", Text["DemoGridCountryNetherlands"], 9100)
        ];
        KanbanRows = [.. Rows.Take(3)];
        ManyRows = [.. Enumerable.Range(1, 2_000).Select(index => Rows[index % Rows.Count] with { Reference = $"L-{index:0000}" })];
        LogLines =
        [
            new(Text["DemoDataListLogSearch"], OmniLogLevel.Trace),
            new(Text["DemoDataListLogConnected"], OmniLogLevel.Debug),
            new(Text["DemoDataListLogStarted"], OmniLogLevel.Information),
            new(Text["DemoDataListLogMissing"], OmniLogLevel.Warning),
            new(Text["DemoDataListLogRejected"], OmniLogLevel.Error),
            new(Text["DemoDataListLogAborted"], OmniLogLevel.Critical)
        ];
    }

    private string WorkflowColumnOf(GridRow row) => WorkflowPositions[row.Reference];

    private static object WorkflowKeyOf(GridRow row) => row.Reference;

    private static string WorkflowLabelOf(GridRow row) => row.Reference;

    private void MoveWorkflowRow(OmniKanbanMove<GridRow> move) =>
        WorkflowPositions[move.Item.Reference] = move.ToColumn;

    /// <summary>The export of the card list: every file, read page by page as a remote source would serve it.</summary>
    private OmniMarkdownTableExport<GridRow> CreateExport() => new()
    {
        Title = Text["DemoGridOpenCases"],
        Columns =
        [
            new(Text["DemoGridColumnReference"], row => row.Reference),
            new(Text["DemoGridColumnApplicant"], row => row.Applicant),
            new(Text["DemoGridColumnCountry"], row => row.Country),
            new(Text["DemoGridColumnAmount"], row => row.Amount.ToString("C0", CultureInfo.CurrentCulture))
        ],
        Fields = [new(Text["DemoGridExportSort"], Text["DemoGridExportSortValue"])],
        PageSize = 3,
        LoadPage = request => Task.FromResult(new OmniDataGridResult<GridRow>(
            [.. Rows.OrderBy(row => row.Reference, StringComparer.Ordinal).Skip(request.Skip).Take(request.PageSize)], Rows.Count))
    };

    private void ChangePageSize(int size)
    {
        PageSize = size;
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
    }
}
