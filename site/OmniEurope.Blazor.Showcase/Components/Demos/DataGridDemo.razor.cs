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

    private IReadOnlyList<RunRow> SelectedRuns { get; set; } = [];

    protected override void OnInitialized()
    {
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
            Run("#2400", "atlas-candidate", 192, "RunsStatusDanger", "danger"),
            Run("#2399", "aetheus-deploy-prod", 108, "RunsStatusSuccess", "success"),
            Run("#2398", "portfolio-build", 52, "RunsStatusSuccess", "success"),
            Run("#2397", "atlas-candidate", 164, "RunsStatusInfo", "info"),
            Run("#2396", "aetheus-candidate", 121, "RunsStatusSuccess", "success"),
            Run("#2395", "aetheus-deploy-prod", 309, "DemoGridStatusPending", "warning"),
            Run("#2394", "aetheus-candidate", 125, "RunsStatusSuccess", "success"),
            Run("#2393", "portfolio-build", 47, "RunsStatusWarning", "warning")
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
        SelectedRuns = [.. Runs.Where(run => run.Run == "#2395")];
    }

    /// <summary>A run whose status reads in the current culture; the tone picks the dot's colour.</summary>
    private RunRow Run(string run, string pipeline, int seconds, string statusKey, string tone) =>
        new(run, pipeline, seconds, Text[statusKey], tone);

    /// <summary>The export of the first grid: every file, read page by page as a remote source would serve it.</summary>
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
}
