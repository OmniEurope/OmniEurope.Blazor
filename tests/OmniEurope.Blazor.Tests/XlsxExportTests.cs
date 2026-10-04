using System.IO.Compression;
using System.Xml.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The workbook the package writes itself (OmniTableExporter.ToXlsx): the zip is opened and its
/// SpreadsheetML parts read back, cell by cell.
/// </summary>
public sealed class XlsxExportTests : OmniBunitContext
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly OmniTableExportDocument Document = new()
    {
        Title = "Bilan: actif/passif [2026]",
        Columns =
        [
            new("Compte", OmniTableExportValueKind.Text),
            new("Montant", OmniTableExportValueKind.Number),
            new("Date", OmniTableExportValueKind.Date),
            new("Lettré", OmniTableExportValueKind.Boolean)
        ],
        Rows =
        [
            [new("411000 Clients"), new("1 250,50") { Number = 1250.5m }, new("02/01/2026") { Date = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.FromHours(1)) }, new("Oui") { Boolean = true }],
            [new("=HYPERLINK(\"x\")"), new("-3") { Number = -3m }, new("02/01/2026 18:00") { Date = new DateTimeOffset(2026, 1, 2, 18, 0, 0, TimeSpan.Zero) }, new("Non") { Boolean = false }],
            [new("Caisse \u0001 <€> & 😀"), new(string.Empty), new(string.Empty), new(string.Empty)]
        ]
    };

    private static Dictionary<string, XDocument> Open(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(entry => entry.FullName, entry =>
        {
            using var stream = entry.Open();
            return XDocument.Load(stream);
        });
    }

    private static XElement Cell(XDocument sheet, string reference) =>
        sheet.Descendants(Main + "c").Single(cell => (string?)cell.Attribute("r") == reference);

    [Fact]
    public void TheWorkbook_HasThePartsASpreadsheetNeeds()
    {
        var parts = Open(OmniTableExporter.ToXlsx(Document));

        Assert.Equal(
            ["[Content_Types].xml", "_rels/.rels", "xl/_rels/workbook.xml.rels", "xl/styles.xml", "xl/workbook.xml", "xl/worksheets/sheet1.xml"],
            parts.Keys.Order(StringComparer.Ordinal));
        var sheet = parts["xl/workbook.xml"].Descendants(Main + "sheet").Single();
        // The characters a sheet name refuses are dropped, and it is cut to 31.
        Assert.Equal("Bilan actifpassif 2026", (string?)sheet.Attribute("name"));
        Assert.Equal("rId1", (string?)sheet.Attribute(Relationships + "id"));
        Assert.Contains(parts["[Content_Types].xml"].Root!.Elements(), element => (string?)element.Attribute("PartName") == "/xl/worksheets/sheet1.xml");
    }

    [Fact]
    public void Cells_AreTyped_UnderABoldFrozenHeading()
    {
        var sheet = Open(OmniTableExporter.ToXlsx(Document))["xl/worksheets/sheet1.xml"];

        var heading = Cell(sheet, "B1");
        Assert.Equal("1", (string?)heading.Attribute("s"));
        Assert.Equal("Montant", heading.Value);
        Assert.Equal("frozen", (string?)sheet.Descendants(Main + "pane").Single().Attribute("state"));
        Assert.Equal("A1:D4", (string?)sheet.Descendants(Main + "autoFilter").Single().Attribute("ref"));

        var number = Cell(sheet, "B2");
        Assert.Null(number.Attribute("t"));
        Assert.Equal("1250.5", number.Element(Main + "v")!.Value);
        Assert.Equal("-3", Cell(sheet, "B3").Element(Main + "v")!.Value);

        var date = Cell(sheet, "C2");
        Assert.Equal("2", (string?)date.Attribute("s"));
        Assert.Equal("46024", date.Element(Main + "v")!.Value);
        var dateTime = Cell(sheet, "C3");
        Assert.Equal("3", (string?)dateTime.Attribute("s"));
        Assert.Equal("46024.75", dateTime.Element(Main + "v")!.Value);

        Assert.Equal("b", (string?)Cell(sheet, "D2").Attribute("t"));
        Assert.Equal("1", Cell(sheet, "D2").Element(Main + "v")!.Value);
        Assert.Equal("0", Cell(sheet, "D3").Element(Main + "v")!.Value);

        var text = Cell(sheet, "A2");
        Assert.Equal("inlineStr", (string?)text.Attribute("t"));
        Assert.Equal("411000 Clients", text.Value);
        // An empty cell holds no value at all.
        Assert.Empty(Cell(sheet, "B4").Elements());
    }

    [Fact]
    public void NoCellIsAFormula_AndATextThatLooksLikeOneIsMarkedAsText()
    {
        var parts = Open(OmniTableExporter.ToXlsx(Document));
        var sheet = parts["xl/worksheets/sheet1.xml"];

        Assert.Empty(sheet.Descendants(Main + "f"));
        var formulaLike = Cell(sheet, "A3");
        Assert.Equal("=HYPERLINK(\"x\")", formulaLike.Value);
        Assert.Equal("4", (string?)formulaLike.Attribute("s"));
        var quoted = parts["xl/styles.xml"].Descendants(Main + "cellXfs").Single().Elements(Main + "xf").ElementAt(4);
        Assert.Equal("1", (string?)quoted.Attribute("quotePrefix"));
        Assert.Equal("1", (string?)parts["xl/styles.xml"].Descendants(Main + "cellXfs").Single().Elements(Main + "xf").ElementAt(1).Attribute("fontId"));
        Assert.NotNull(parts["xl/styles.xml"].Descendants(Main + "font").ElementAt(1).Element(Main + "b"));
    }

    [Fact]
    public void Texts_KeepTheirCharacters_ButNotTheOnesXmlForbids()
    {
        var sheet = Open(OmniTableExporter.ToXlsx(Document))["xl/worksheets/sheet1.xml"];

        Assert.Equal("Caisse  <€> & 😀", Cell(sheet, "A4").Value);
    }

    [Fact]
    public void Columns_AreAsWideAsTheirLongestText_WithinBounds()
    {
        var sheet = Open(OmniTableExporter.ToXlsx(Document))["xl/worksheets/sheet1.xml"];

        var widths = sheet.Descendants(Main + "col").Select(column => (string?)column.Attribute("width")).ToArray();
        Assert.Equal(["20", "11", "19", "9"], widths);
    }

    [Fact]
    public async Task TheExporter_WritesExcelWithoutAHostRenderer()
    {
        var exporter = Services.GetRequiredService<OmniTableExporter>();

        Assert.True(exporter.Supports(OmniTableExportFormat.Excel));
        var file = await exporter.RenderAsync(Document, OmniTableExportFormat.Excel, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("xlsx", file.Extension);
        Assert.Equal(OmniTableExporter.XlsxContentType, file.ContentType);
        Assert.Contains("xl/worksheets/sheet1.xml", Open(file.Content.ToArray()).Keys);
    }

    [Fact]
    public async Task TheDownloader_HandsAHostTableToTheBrowser()
    {
        var download = JSInterop.SetupModule(Internal.OmniModules.DocumentEditor);
        download.SetupVoid("download", _ => true).SetVoidResult();
        var downloader = Services.GetRequiredService<OmniTableExportDownloader>();

        var file = await downloader.DownloadAsync(Document, OmniTableExportFormat.Excel, " bilan-2026 ", Xunit.TestContext.Current.CancellationToken);

        var call = Assert.Single(download.Invocations["download"]);
        Assert.Equal("bilan-2026.xlsx", call.Arguments[0]);
        Assert.Equal(OmniTableExporter.XlsxContentType, call.Arguments[1]);
        Assert.Equal(file.Content.ToArray(), (byte[])call.Arguments[2]!);
        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadAsync(Document, OmniTableExportFormat.Csv, " ", Xunit.TestContext.Current.CancellationToken));
        await downloader.DisposeAsync();
    }
}
