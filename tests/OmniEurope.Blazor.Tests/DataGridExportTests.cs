using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridExportTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The export bar of the grid: every row the filters and sorts select, not the page on screen; the
/// columns that read a value; Markdown and CSV written by the package, the other formats by the host's
/// renderer; and a bar that says when the file does not hold every row.
/// </summary>
public sealed class DataGridExportTests : OmniBunitContext
{
    private const string DownloadModulePath = Internal.OmniModules.DocumentEditor;
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 8, 15, 0, TimeSpan.Zero);

    private static readonly Row[] Rows =
    [
        new(1, "Delta", 1234.5m, new DateOnly(2026, 9, 1), true),
        new(2, "Alpha", 20m, new DateOnly(2026, 9, 2), false),
        new(3, "=SUM(A1)", -3m, new DateOnly(2026, 9, 3), false),
        new(4, "Bravo; \"cité\"", 0.5m, new DateOnly(2026, 9, 4), true),
        new(5, "Charlie", 7m, new DateOnly(2026, 9, 5), true)
    ];

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class RecordingRenderer : IOmniTableExportRenderer
    {
        public List<(OmniTableExportDocument Document, OmniTableExportFormat Format)> Calls { get; } = [];

        public Exception? Failure { get; set; }

        public bool Supports(OmniTableExportFormat format) => format is OmniTableExportFormat.Excel or OmniTableExportFormat.Pdf;

        public Task<OmniTableExportFile> RenderAsync(OmniTableExportDocument document, OmniTableExportFormat format, CancellationToken cancellationToken)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Calls.Add((document, format));
            return Task.FromResult(new OmniTableExportFile(new byte[] { 1, 2, 3 },
                format == OmniTableExportFormat.Pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                format == OmniTableExportFormat.Pdf ? "pdf" : "xlsx"));
        }
    }

    private readonly BunitJSModuleInterop _download;

    public DataGridExportTests()
    {
        Services.AddSingleton<TimeProvider>(new FixedClock());
        _download = JSInterop.SetupModule(DownloadModulePath);
        _download.SetupVoid("download", _ => true).SetVoidResult();
    }

    private static string CsvOf(JSRuntimeInvocation download) => Encoding.UTF8.GetString((byte[])download.Arguments[2]!);

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<DataGridExportTestHost> host, string text) =>
        host.FindAll(".omni-data-grid__export-button").Single(button => button.TextContent.Trim() == text);

    [Fact]
    public void WithoutFormats_TheGridHasNoExportBar()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters.Add(component => component.Items, Rows));

        Assert.Empty(host.FindAll(".omni-data-grid__export"));
    }

    [Fact]
    public void AFormatNobodyWrites_HasNoButton()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown, OmniTableExportFormat.Csv, OmniTableExportFormat.Excel, OmniTableExportFormat.Pdf]));

        Assert.Equal(["Markdown", "CSV"], host.FindAll(".omni-data-grid__export-button").Select(button => button.TextContent.Trim()));
    }

    [Theory]
    [InlineData(OmniDataGridPosition.Bottom, false)]
    [InlineData(OmniDataGridPosition.Top, true)]
    public void TheBar_GoesUnderOrAboveTheTable(OmniDataGridPosition position, bool above)
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Position, position)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        var grid = host.Find(".omni-data-grid");
        var children = grid.Children.ToList();
        var bar = Assert.Single(children, child => child.ClassList.Contains("omni-data-grid__export"));
        var viewport = children.Single(child => child.ClassList.Contains("omni-data-grid__viewport"));
        Assert.Equal(above, children.IndexOf(bar) < children.IndexOf(viewport));
        Assert.Equal("group", bar.GetAttribute("role"));
        Assert.Equal("Export du tableau", bar.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task AnEmptyGrid_DisablesItsButtons_UntilRowsArrive()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown, OmniTableExportFormat.Csv]));

        Assert.All(host.FindAll(".omni-data-grid__export-button"), button => Assert.True(button.HasAttribute("disabled")));
        // A call that bypasses the disabled button writes nothing either.
        var grid = host.FindComponent<OmniDataGrid<Row>>().Instance;
        await host.InvokeAsync(() => grid.Export.RunAsync(OmniTableExportFormat.Csv));
        Assert.Empty(_download.Invocations["download"]);
        Assert.Null(host.Instance.Exported);

        host.Render(parameters => parameters.Add(component => component.Items, Rows));

        Assert.All(host.FindAll(".omni-data-grid__export-button"), button => Assert.False(button.HasAttribute("disabled")));
    }

    [Fact]
    public void AGridWhoseFiltersLeaveNoRow_DisablesItsButtons()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.NameFilter, "introuvable")
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        Assert.True(Button(host, "CSV").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Csv_OfALocalGrid_HoldsEveryFilteredRowInTheSortOrder_NotThePage()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.NameSort, OmniDataGridSortOrder.Ascending)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        // The grid shows a page of two rows.
        Assert.Equal(2, host.FindAll("tbody tr").Count);

        await Button(host, "CSV").ClickAsync(new());

        var download = Assert.Single(_download.Invocations["download"]);
        Assert.Equal("commandes-2026-09-30-0815.csv", download.Arguments[0]);
        Assert.Equal("text/csv;charset=utf-8", download.Arguments[1]);
        var bytes = (byte[])download.Arguments[2]!;
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes.Take(3));
        var lines = CsvOf(download).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        // French culture: decimal comma, so the separator is a semicolon and numbers are raw values.
        Assert.Equal("N°;Nom;Montant;Jour;Statut", lines[0]);
        Assert.Equal(
        [
            "3;'=SUM(A1);-3;03/09/2026;due",
            "2;Alpha;20;02/09/2026;due",
            "4;\"Bravo; \"\"cité\"\"\";0,5;04/09/2026;payée",
            "5;Charlie;7;05/09/2026;payée",
            "1;Delta;1234,5;01/09/2026;payée"
        ], lines.Skip(1));
        // Exporting changes nothing of what the grid shows.
        Assert.Equal(2, host.FindAll("tbody tr").Count);
        Assert.Empty(host.FindAll(".omni-data-grid__export-message"));
    }

    [Fact]
    public async Task TheDocument_KeepsTheValuesBehindTheTexts_AndSaysWhichRowsItAnswersTo()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.NameFilter, "l")
            .Add(component => component.Caption, "Commandes")
            .Add(component => component.Fields, [new OmniTableExportField("Application", "Boutique")])
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));

        await Button(host, "Markdown").ClickAsync(new());

        var document = host.Instance.Exported;
        Assert.NotNull(document);
        Assert.Equal("Commandes", document.Title);
        Assert.Equal("fr-FR", document.Culture);
        Assert.Equal(Now, document.GeneratedAt);
        Assert.Equal(
            [("N°", OmniTableExportValueKind.Number), ("Nom", OmniTableExportValueKind.Text), ("Montant", OmniTableExportValueKind.Number),
             ("Jour", OmniTableExportValueKind.Date), ("Statut", OmniTableExportValueKind.Text)],
            document.Columns.Select(column => (column.Title, column.Kind)));
        Assert.Equal(["Application", "Filtrer Nom"], document.Fields.Select(field => field.Label));
        Assert.EndsWith("l", document.Fields[1].Value, StringComparison.Ordinal);
        // "l" matches Delta, Alpha and Charlie.
        Assert.Equal(3, document.RowCount);
        Assert.True(document.IsComplete);
        var delta = document.Rows.Single(row => row[1].Text == "Delta");
        Assert.Equal(1234.5m, delta[2].Number);
        Assert.Equal("1 234,50 EUR", delta[2].Text.Replace(' ', ' ').Replace(' ', ' '));
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), delta[3].Date);
        Assert.Equal("payée", delta[4].Text);

        var download = Assert.Single(_download.Invocations["download"]);
        Assert.Equal("commandes-2026-09-30-0815.md", download.Arguments[0]);
        var markdown = Encoding.UTF8.GetString((byte[])download.Arguments[2]!);
        Assert.StartsWith("# Commandes", markdown, StringComparison.Ordinal);
        Assert.Contains("- Application : Boutique", markdown, StringComparison.Ordinal);
        Assert.Contains("| N° | Nom | Montant | Jour | Statut |", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARemoteGrid_IsReadPageByPage_WithTheSortsAndFiltersInForce()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var all = Enumerable.Range(1, 450).Select(id => new Row(id, $"ligne {id}", id, new DateOnly(2026, 1, 1), true)).ToArray();
        Task<OmniDataGridResult<Row>> Load(OmniDataGridLoadRequest request)
        {
            requests.Add(request);
            return Task.FromResult(new OmniDataGridResult<Row>(all.Skip(request.Skip).Take(request.Top).ToArray(), all.Length));
        }

        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, Load)
            .Add(component => component.NameFilter, "ligne")
            .Add(component => component.NameSort, OmniDataGridSortOrder.Descending)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        requests.Clear();

        await Button(host, "CSV").ClickAsync(new());

        Assert.Equal([1, 2, 3], requests.Select(request => request.Page));
        Assert.All(requests, request =>
        {
            Assert.Equal(200, request.PageSize);
            Assert.Equal("Name", Assert.Single(request.Sorts).Key);
            Assert.True(request.SortDescending);
            Assert.Equal("ligne", Assert.Single(request.Filters).Value);
        });
        Assert.Equal(450, host.Instance.Exported!.RowCount);
        Assert.Equal(2, host.FindAll("tbody tr").Count);
    }

    [Fact]
    public async Task ExportLoad_ReadsTheRows_InPlaceOfLoad()
    {
        var loads = 0;
        var exports = 0;
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, _ =>
            {
                loads++;
                return Task.FromResult(new OmniDataGridResult<Row>(Rows.Take(2).ToArray(), Rows.Length));
            })
            .Add(component => component.ExportLoad, request =>
            {
                exports++;
                return Task.FromResult(new OmniDataGridResult<Row>(Rows.Skip(request.Skip).Take(request.Top).ToArray(), Rows.Length));
            })
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        var loadsBefore = loads;

        await Button(host, "CSV").ClickAsync(new());

        Assert.Equal(1, exports);
        Assert.Equal(loadsBefore, loads);
        Assert.Equal(5, host.Instance.Exported!.RowCount);
    }

    [Fact]
    public async Task AnExportCutByTheRowLimit_SaysSoInTheBar()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.RowLimit, 3)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        await Button(host, "CSV").ClickAsync(new());

        var document = host.Instance.Exported!;
        Assert.Equal(3, document.RowCount);
        Assert.Equal(5, document.TotalCount);
        Assert.True(document.Truncated);
        var message = host.Find(".omni-data-grid__export-message");
        Assert.Equal("status", message.GetAttribute("role"));
        Assert.Contains("tronqué", message.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ASourceWithoutTotal_StoppedOnAFullPageByTheLimit_SaysMoreRowsMayExist()
    {
        // Pages are always full and the source announces no total (0): the reading stops at the limit.
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(
                Enumerable.Range(1, request.PageSize).Select(id => new Row(((request.Page - 1) * request.PageSize) + id, $"ligne {id}", id, new DateOnly(2026, 9, 1), false)).ToArray(), 0)))
            .Add(component => component.RowLimit, 3)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));

        await Button(host, "CSV").ClickAsync(new());

        var document = host.Instance.Exported!;
        Assert.Equal(3, document.RowCount);
        Assert.True(document.TotalIsLowerBound);
        Assert.Contains("la source n'annonce pas de total", host.Find(".omni-data-grid__export-message").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AGridWithNoColumnToExport_FailsWithoutAFile_AndReportsWhy()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.OnlyTemplates, true)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        await Button(host, "CSV").ClickAsync(new());

        Assert.Empty(_download.Invocations["download"]);
        Assert.IsType<InvalidOperationException>(host.Instance.Failure);
        Assert.Equal("L'export a échoué.", host.Find(".omni-data-grid__export-message--error").TextContent);
    }

    [Fact]
    public async Task TheRowLimit_DefaultsTo5000_AndAFormatNobodyWrites_IsRefused()
    {
        Assert.Equal(5000, new OmniDataGrid<Row>().ExportRowLimit);
        var exporter = Services.GetRequiredService<OmniTableExporter>();
        var document = new OmniTableExportDocument { Title = "Commandes", Columns = [new("Nom", OmniTableExportValueKind.Text)], Rows = [] };

        Assert.False(exporter.Supports(OmniTableExportFormat.Excel));
        await Assert.ThrowsAsync<NotSupportedException>(() => exporter.RenderAsync(document, OmniTableExportFormat.Excel, Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheHostRenderer_WritesTheFormatsThePackageDoesNot()
    {
        var renderer = new RecordingRenderer();
        Services.AddSingleton<IOmniTableExportRenderer>(renderer);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Title, "Commandes du mois")
            .Add(component => component.Formats, [OmniTableExportFormat.Excel, OmniTableExportFormat.Pdf]));

        await Button(host, "PDF").ClickAsync(new());

        var (document, format) = Assert.Single(renderer.Calls);
        Assert.Equal(OmniTableExportFormat.Pdf, format);
        Assert.Equal("Commandes du mois", document.Title);
        Assert.Equal(5, document.RowCount);
        var download = Assert.Single(_download.Invocations["download"]);
        Assert.Equal("commandes-2026-09-30-0815.pdf", download.Arguments[0]);
        Assert.Equal("application/pdf", download.Arguments[1]);
        Assert.Equal([1, 2, 3], (byte[])download.Arguments[2]!);
    }

    [Fact]
    public async Task AFailedExport_ProducesNoFile_SaysSo_AndReportsTheException()
    {
        var renderer = new RecordingRenderer { Failure = new InvalidOperationException("rendu indisponible") };
        Services.AddSingleton<IOmniTableExportRenderer>(renderer);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Excel]));

        await Button(host, "Excel").ClickAsync(new());

        Assert.Empty(_download.Invocations["download"]);
        Assert.Same(renderer.Failure, host.Instance.Failure);
        var message = host.Find(".omni-data-grid__export-message--error");
        Assert.Equal("alert", message.GetAttribute("role"));
        Assert.Equal("L'export a échoué.", message.TextContent);
        // The next export clears the failure.
        renderer.Failure = null;
        await Button(host, "Excel").ClickAsync(new());
        Assert.Empty(host.FindAll(".omni-data-grid__export-message--error"));
    }

    [Fact]
    public async Task WhileAnExportRuns_ItsButtonIsBusy_TheOthersWait_AndCancelStopsIt()
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var first = true;
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, _ =>
            {
                if (first)
                {
                    first = false;
                    return Task.FromResult(new OmniDataGridResult<Row>(Rows.Take(2).ToArray(), Rows.Length));
                }

                return pending.Task;
            })
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown, OmniTableExportFormat.Csv]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));

        var running = Button(host, "CSV").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal("true", Button(host, "CSV").GetAttribute("aria-busy")));
        Assert.True(Button(host, "Markdown").HasAttribute("disabled"));
        await host.Find(".omni-data-grid__export-cancel").ClickAsync(new());
        await running;

        Assert.Empty(_download.Invocations["download"]);
        Assert.Null(host.Instance.Failure);
        Assert.Empty(host.FindAll(".omni-data-grid__export-cancel"));
        Assert.Empty(host.FindAll(".omni-data-grid__export-message"));
    }

    [Fact]
    public void Csv_InACultureWithADecimalPoint_IsSeparatedByCommas()
    {
        var document = new OmniTableExportDocument
        {
            Title = "Orders",
            Culture = "en-GB",
            Columns = [new("Name", OmniTableExportValueKind.Text), new("Amount", OmniTableExportValueKind.Number)],
            Rows = [[new("Smith, John"), new("1,234.50 EUR") { Number = 1234.5m }], [new("-dash"), new(string.Empty)]]
        };

        Assert.Equal("Name,Amount\r\n\"Smith, John\",1234.5\r\n'-dash,\r\n", OmniTableExporter.ToCsv(document));
    }

    [Theory]
    [InlineData("+1+2", "'+1+2")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tcmd", "'\tcmd")]
    [InlineData("\rcmd", "\"'\rcmd\"")]
    public void Csv_PrefixesEveryFormulaTrigger_InCellsAndInColumnTitles(string text, string written)
    {
        var document = new OmniTableExportDocument
        {
            Title = "Orders",
            Culture = "en-GB",
            Columns = [new("=cmd", OmniTableExportValueKind.Text), new("Amount", OmniTableExportValueKind.Number)],
            Rows = [[new(text), new("-2") { Number = -2m }]]
        };

        // A spreadsheet runs a cell that starts with = + - @, a tab or a carriage return: the title and
        // the text are prefixed, a typed number is written as is.
        Assert.Equal($"'=cmd,Amount\r\n{written},-2\r\n", OmniTableExporter.ToCsv(document));
    }

    [Fact]
    public void EachFormat_HasItsFileIcon_AndTheVariantTheHostChose()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown, OmniTableExportFormat.Csv])
            .Add(component => component.Variants, new Dictionary<OmniTableExportFormat, OmniButtonVariant> { [OmniTableExportFormat.Markdown] = OmniButtonVariant.Primary }));

        // Markdown in the host's blue, CSV left Ghost: an export is a secondary action by default.
        Assert.Contains("omni-button--primary", Button(host, "Markdown").ClassList);
        Assert.Contains("omni-button--ghost", Button(host, "CSV").ClassList);
        var markdownIcon = Button(host, "Markdown").QuerySelector("path")!.GetAttribute("d");
        var csvIcon = Button(host, "CSV").QuerySelector("path")!.GetAttribute("d");
        Assert.NotEqual(markdownIcon, csvIcon);
        Assert.Equal(Render<OmniIcon>(parameters => parameters.Add(icon => icon.Name, OmniIconName.FileMd)).Find("path").GetAttribute("d"), markdownIcon);
    }

    [Fact]
    public async Task WithoutAFileName_TheFileIsNamedAfterTheExportTitle_ThenTheDate()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.FileName, null)
            .Add(component => component.Title, "Journaux d'Aetheus : étape 3")
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        await Button(host, "CSV").ClickAsync(new());

        Assert.Equal("journaux-d-aetheus-etape-3-2026-09-30-0815.csv", Assert.Single(_download.Invocations["download"]).Arguments[0]);
    }

    [Fact]
    public async Task AHandlerOfOnExportThatThrows_LeavesTheExportDelivered_NotFailed()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.ThrowOnExport, true)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        // The host's exception reaches the caller, as any event handler's does.
        await Assert.ThrowsAsync<InvalidOperationException>(() => Button(host, "CSV").ClickAsync(new()));

        Assert.Single(_download.Invocations["download"]);
        Assert.NotNull(host.Instance.Exported);
        Assert.Null(host.Instance.Failure);
    }

    [Fact]
    public void TheDocument_SurvivesJson_SoABrowserHostCanSendItToItsServer()
    {
        var document = new OmniTableExportDocument
        {
            Title = "Commandes",
            Culture = "fr-FR",
            GeneratedAt = Now,
            TotalCount = 9,
            RowLimit = 5,
            Fields = [new("Période", "24 h")],
            Columns = [new("Montant", OmniTableExportValueKind.Number), new("Payée", OmniTableExportValueKind.Boolean)],
            Rows = [[new("12,50") { Number = 12.5m }, new("True") { Boolean = true }]]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(document, System.Text.Json.JsonSerializerOptions.Web);
        var back = System.Text.Json.JsonSerializer.Deserialize<OmniTableExportDocument>(json, System.Text.Json.JsonSerializerOptions.Web)!;

        Assert.Equal(document.Title, back.Title);
        Assert.Equal(document.Fields, back.Fields);
        Assert.Equal(document.Columns, back.Columns);
        Assert.Equal(12.5m, back.Rows[0][0].Number);
        Assert.True(back.Rows[0][1].Boolean);
        Assert.True(back.Truncated);
        Assert.DoesNotContain("rowCount", json, StringComparison.Ordinal);
    }
}
