using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataGridDemo
{
    // An organisation's name, sample data kept as written: a long value to show the ellipsis.
    private const string LongApplicant = "Établissement public de coopération intercommunale du Grand Est";

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<GridRow> Rows { get; set; } = [];

    private IReadOnlyList<RunRow> Runs { get; set; } = [];

    private IReadOnlyList<GridRow> FewRows { get; set; } = [];

    private IReadOnlyList<GridRow> NarrowRows { get; set; } = [];

    private IReadOnlyList<GridRow> ManyRows { get; set; } = [];

    // Packages with long names, and a description far longer than its column: the first column sizes
    // itself to the names, the second (Truncate) is cut.
    private IReadOnlyList<PackageRow> Packages { get; set; } = [];

    // Sites with names and projects of very different lengths, a status shown as a badge, a number and a
    // date: the grid whose columns are sized to their content (sample data kept as written).
    private IReadOnlyList<SiteRow> Sites { get; } =
    [
        new("Boutique en ligne", true, 128, "Vitrine commerciale", new DateOnly(2024, 3, 12)),
        new("Documentation technique de la plateforme", true, 37, "Intégration continue", new DateOnly(2023, 9, 4)),
        new("Intranet", false, 0, "Ressources humaines", new DateOnly(2022, 1, 17)),
        new("Extranet partenaires", true, 9, "Relations partenaires", new DateOnly(2025, 6, 30)),
        new("Blog", true, 54, "Communication", new DateOnly(2021, 11, 8))
    ];

    // Two thousand files grouped by country; one amount, far from the first window, is much wider than
    // the others, so the fit to content of Amount has to rank every text, rendered or not.
    private IReadOnlyList<GridRow> FillRows { get; set; } = [];

    // Four rows with a picture, the grid veiled until the pictures are there.
    private IReadOnlyList<GridRow> PictureRows { get; set; } = [];

    private IReadOnlyList<OmniDataGridGroup> FillGrouping { get; set; } = [new(nameof(GridRow.Country))];

    // The preview box: « none » scrolls the grid virtually, a position pages it with the pager there.
    private IReadOnlyList<OmniOption<string>> FillBoxPagerOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridLines>> FillBoxLineOptions { get; set; } = [];

    private string FillBoxPager { get; set; } = "none";

    private OmniDataGridLines FillBoxLines { get; set; } = OmniDataGridLines.Horizontal;

    private bool FillBoxPaged => FillBoxPager != "none";

    private OmniDataGridPlacement FillBoxPagerPlacement => FillBoxPager switch
    {
        "top" => OmniDataGridPlacement.Top,
        "both" => OmniDataGridPlacement.TopAndBottom,
        _ => OmniDataGridPlacement.Bottom
    };

    private IReadOnlyList<RunRow> SelectedRuns { get; set; } = [];

    private IReadOnlyList<StatementLine> Statement { get; set; } = [];

    private OmniDataGrid<EditableFile>? EditGrid { get; set; }

    // Kept by the page while it is shown: an edit changes the file itself, as a host's model would.
    private List<EditableFile> EditableFiles { get; } =
    [
        new() { Reference = "D-1001", Applicant = "Marie Lambert", Country = "Belgique" },
        new() { Reference = "D-1002", Applicant = "Jonas Weber", Country = "Allemagne" },
        new() { Reference = "D-1003", Applicant = "Inês Costa", Country = "Portugal" },
    ];

    // The grid that edits on a click of a cell: the Country of the second file is empty on purpose, a cell
    // with nothing to read is still there to be clicked.
    private List<EditableFile> CellEditFiles { get; } =
    [
        new() { Reference = "D-2001", Applicant = "Camille Durand", Country = "Belgique" },
        new() { Reference = "D-2002", Applicant = "Sofia Rossi", Country = string.Empty },
        new() { Reference = "D-2003", Applicant = "Lars Jansen", Country = "Pays-Bas" },
    ];

    // The clicks the Details button of a Country cell received: a click on it must not open the row.
    private int CellEditDetails { get; set; }

    private void CountCellEditDetails() => CellEditDetails++;

    // The values a row had when it entered edit mode: the editors write into the file itself, so a
    // cancelled edit is undone by the page from this copy (the grid only reports the cancellation).
    private readonly Dictionary<string, (string Applicant, string Country)> _originals = [];

    private void KeepOriginal(EditableFile file) => _originals[file.Reference] = (file.Applicant, file.Country);

    private void RestoreOriginal(EditableFile file)
    {
        if (_originals.Remove(file.Reference, out var original))
        {
            (file.Applicant, file.Country) = original;
        }
    }

    /// <summary>A file of the editable grid: its applicant and country change in place.</summary>
    private sealed class EditableFile
    {
        public required string Reference { get; init; }

        public string Applicant { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;
    }

    private OmniDataGrid<StatementLine>? StatementGrid { get; set; }

    private Task ExpandStatementAsync() => StatementGrid?.ExpandAllTreeRowsAsync() ?? Task.CompletedTask;

    private Task CollapseStatementAsync() => StatementGrid?.CollapseAllTreeRowsAsync() ?? Task.CompletedTask;

    [Inject]
    private OmniTableExportDownloader Downloader { get; set; } = default!;

    // A table the page builds itself, every line at its depth: the label indented by two spaces a level.
    private Task ExportStatementAsync()
    {
        IEnumerable<StatementLine> Flatten(IEnumerable<StatementLine> lines) =>
            lines.SelectMany(line => Flatten(line.Children ?? []).Prepend(line));
        OmniTableExportCell Amount(decimal value) => new(value.ToString("C0", CultureInfo.CurrentCulture)) { Number = value };
        var document = new OmniTableExportDocument
        {
            Title = Text["DemoGridTreeCaption"],
            Columns =
            [
                new(Text["DemoGridTreeColumnItem"], OmniTableExportValueKind.Text),
                new("2025", OmniTableExportValueKind.Number),
                new("2026", OmniTableExportValueKind.Number)
            ],
            Rows = Flatten(Statement)
                .Select(line => (IReadOnlyList<OmniTableExportCell>)[new(new string(' ', line.Level * 2) + line.Label), Amount(line.Previous), Amount(line.Current)])
                .ToArray()
        };
        return Downloader.DownloadAsync(document, OmniTableExportFormat.Excel, Text["DemoGridTreeCaption"]);
    }

    protected override void OnInitialized()
    {
        Statement =
        [
            new("P", Text["DemoGridTreeIncome"], 0, 412000, 455500,
            [
                new("P70", Text["DemoGridTreeSales"], 1, 398000, 441000,
                [
                    new("706", "706 " + Text["DemoGridTreeServices"], 2, 310000, 352000),
                    new("707", "707 " + Text["DemoGridTreeGoods"], 2, 88000, 89000)
                ]),
                new("758", "758 " + Text["DemoGridTreeIncome"], 1, 14000, 14500)
            ]),
            new("C", Text["DemoGridTreeExpenses"], 0, 351000, 372800,
            [
                new("C60", Text["DemoGridTreePurchases"], 1, 121000, 118300,
                [
                    new("607", "607 " + Text["DemoGridTreeGoods"], 2, 121000, 118300)
                ]),
                new("C64", Text["DemoGridTreeStaff"], 1, 230000, 254500,
                [
                    new("641", "641 " + Text["DemoGridTreeStaff"], 2, 230000, 254500)
                ])
            ])
        ];
        Rows =
        [
            new("D-2401", "Camille Durand", Text["DemoGridCountryBelgium"], 12400),
            new("D-2402", "Jonas Meyer", Text["DemoGridCountryLuxembourg"], 8600),
            new("D-2403", "Sofia Rossi", Text["DemoGridCountryFrance"], 21500),
            new("D-2404", "Lars Jansen", Text["DemoGridCountryNetherlands"], 4300),
            new("D-2405", "Ana Silva", Text["DemoGridCountryFrance"], 17800),
            new("D-2406", "Piet de Vries", Text["DemoGridCountryNetherlands"], 9100),
            new("D-2407", "Marie Lambert", Text["DemoGridCountryBelgium"], 15200)
        ];
        Runs =
        [
            Run("#2400", "billing-candidate", 192, "RunsStatusDanger", "danger"),
            Run("#2399", "orders-deploy-prod", 108, "RunsStatusSuccess", "success"),
            Run("#2398", "catalog-build", 52, "RunsStatusSuccess", "success"),
            Run("#2397", "billing-candidate", 164, "RunsStatusInfo", "info"),
            Run("#2396", "orders-candidate", 121, "RunsStatusSuccess", "success"),
            Run("#2395", "orders-deploy-prod", 309, "DemoGridStatusPending", "warning"),
            Run("#2394", "orders-candidate", 125, "RunsStatusSuccess", "success"),
            Run("#2393", "catalog-build", 47, "RunsStatusWarning", "warning")
        ];
        Packages =
        [
            new("@example-org/web-analytics", Text["DemoGridFitDescription"]),
            new("@example-org/design-tokens", Text["DemoGridFitDescription"]),
            new("@example-org/ui", Text["DemoGridFitDescription"])
        ];
        FewRows = [.. Rows.Take(3)];
        NarrowRows = [Rows[0] with { Applicant = LongApplicant }, .. Rows.Skip(1).Take(2)];
        ManyRows = [.. Enumerable.Range(1, 10_000)
            .Select(index => Rows[index % Rows.Count] with
            {
                Reference = $"D-{index:00000}",
                // Far outside the first window: the fit to content still has to see it.
                Applicant = index == 7_777 ? LongApplicant : Rows[index % Rows.Count].Applicant
            })];
        FillRows = [.. Enumerable.Range(1, 2_000)
            .Select(index => Rows[index % Rows.Count] with
            {
                Reference = $"F-{index:0000}",
                Amount = index == 1_999 ? 123_456_789_012_345_678m : 1_000m + (index * 37m)
            })];
        PictureRows = [.. FillRows.Take(4)];
        SelectedRuns = [.. Runs.Where(run => run.Run == "#2395")];
        FillBoxPagerOptions =
        [
            new("none", Text["DemoGridAdvancedLinesNone"]),
            new("bottom", Text["DemoGridAdvancedPagerBottom"]),
            new("top", Text["DemoGridAdvancedPagerTop"]),
            new("both", Text["DemoGridAdvancedPagerBoth"])
        ];
        FillBoxLineOptions =
        [
            new(OmniDataGridLines.None, Text["DemoGridAdvancedLinesNone"]),
            new(OmniDataGridLines.Horizontal, Text["DemoGridAdvancedLinesHorizontal"]),
            new(OmniDataGridLines.Vertical, Text["DemoGridAdvancedLinesVertical"]),
            new(OmniDataGridLines.Both, Text["DemoGridAdvancedLinesBoth"])
        ];
    }

    /// <summary>A run whose status reads in the current culture; the tone picks the dot's colour.</summary>
    private RunRow Run(string run, string pipeline, int seconds, string statusKey, string tone) =>
        new(run, pipeline, seconds, Text[statusKey], tone);

}
