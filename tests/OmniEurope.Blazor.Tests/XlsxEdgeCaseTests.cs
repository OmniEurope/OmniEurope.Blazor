using System.IO.Compression;
using System.Xml.Linq;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The workbook against the texts and dates a spreadsheet cannot hold: an unpaired surrogate half, a text past
/// the 32 767 characters of a cell, a date before the 1900 calendar, a reserved sheet name. None of them may
/// fail the export or write a part the spreadsheet reports as damaged.
/// </summary>
public sealed class XlsxEdgeCaseTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static OmniTableExportDocument Single(OmniTableExportCell cell, string title = "Feuille") => new()
    {
        Title = title,
        Columns = [new("Valeur", OmniTableExportValueKind.Text)],
        Rows = [[cell]]
    };

    private static XDocument Part(byte[] bytes, string name)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var stream = zip.GetEntry(name)!.Open();
        return XDocument.Load(stream);
    }

    private static XElement Cell(byte[] bytes, string reference) =>
        Part(bytes, "xl/worksheets/sheet1.xml").Descendants(Main + "c").Single(cell => (string?)cell.Attribute("r") == reference);

    // Built at run time: an attribute argument is stored as UTF-8, which cannot hold a lone surrogate.
    public static TheoryData<string, string> Surrogates => new()
    {
        { "avant \ud83d après", "avant  après" },
        { "avant \ude00 après", "avant  après" },
        { "fin \ud83d", "fin " },
        { "emoji 😀 gardé", "emoji 😀 gardé" }
    };

    [Theory]
    [MemberData(nameof(Surrogates))]
    public void AnUnpairedSurrogateHalf_IsDropped_AndAPairIsKept(string text, string expected)
    {
        var bytes = OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(text)));

        Assert.Equal(expected, Cell(bytes, "A2").Value);
    }

    [Fact]
    public void AnUnpairedSurrogateHalfInTheTitleOrAHeading_DoesNotFailTheExport()
    {
        var document = new OmniTableExportDocument
        {
            Title = "Titre \ud83d",
            Columns = [new("Colonne \ude00", OmniTableExportValueKind.Text)],
            Rows = []
        };

        var bytes = OmniTableExporter.ToXlsx(document);

        Assert.DoesNotContain('\ud83d', (string?)Part(bytes, "xl/workbook.xml").Descendants(Main + "sheet").Single().Attribute("name") ?? string.Empty);
        Assert.Equal("Colonne ", Cell(bytes, "A1").Value);
    }

    [Fact]
    public void ATextPastTheLimitOfACell_IsCutToIt_WithoutSplittingAPair()
    {
        var text = new string('a', 32_766) + "😀" + new string('b', 100);

        var value = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(text))), "A2").Value;

        Assert.True(value.Length <= 32_767);
        Assert.Equal(new string('a', 32_766), value);
    }

    [Fact]
    public void ATextAtTheLimitOfACell_IsKeptWhole()
    {
        var text = new string('a', 32_767);

        Assert.Equal(text, Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(text))), "A2").Value);
    }

    [Theory]
    [InlineData(1899, 12, 31)]
    [InlineData(1850, 6, 1)]
    [InlineData(1900, 1, 1)]
    [InlineData(1900, 2, 28)]
    public void ADateBeforeTheSpreadsheetCalendar_IsWrittenAsItsText_NotAsASerialThatReadsWrong(int year, int month, int day)
    {
        var date = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);

        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell("ancienne") { Date = date })), "A2");

        Assert.Equal("inlineStr", (string?)cell.Attribute("t"));
        Assert.Equal("ancienne", cell.Value);
    }

    [Fact]
    public void ADateOfTheSpreadsheetCalendar_StaysADate()
    {
        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell("récente") { Date = new DateTimeOffset(1900, 3, 1, 0, 0, 0, TimeSpan.Zero) })), "A2");

        Assert.Null(cell.Attribute("t"));
        Assert.Equal("61", cell.Element(Main + "v")!.Value);
    }

    [Theory]
    [InlineData("History")]
    [InlineData("history")]
    public void TheReservedSheetNameIsNotUsed(string title)
    {
        var bytes = OmniTableExporter.ToXlsx(Single(new OmniTableExportCell("x"), title));

        var name = (string?)Part(bytes, "xl/workbook.xml").Descendants(Main + "sheet").Single().Attribute("name");

        Assert.NotEqual("history", name?.ToLowerInvariant());
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void AnEarlyDateWithoutText_IsWrittenAsItsIsoDate()
    {
        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(string.Empty) { Date = new DateTimeOffset(1850, 6, 1, 0, 0, 0, TimeSpan.Zero) })), "A2");

        Assert.Equal("1850-06-01", cell.Value);
    }

    [Fact]
    public void ATextPastTheLimitOfACell_WithNoPairAtTheCut_IsCutExactlyAtTheLimit()
    {
        var value = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(new string('a', 40_000)))), "A2").Value;

        Assert.Equal(32_767, value.Length);
    }

    [Fact]
    public void ADateBeforeTheSpreadsheetCalendar_WithATimeOfDay_IsWrittenWithItsTime()
    {
        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(string.Empty) { Date = new DateTimeOffset(1850, 6, 1, 14, 30, 0, TimeSpan.Zero) })), "A2");

        Assert.Equal("1850-06-01 14:30", cell.Value);
    }

    [Fact]
    public void ACellWithANumber_KeepsItsNumber_EvenWithAnEarlyDate()
    {
        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(string.Empty) { Number = 5m, Date = new DateTimeOffset(1850, 6, 1, 0, 0, 0, TimeSpan.Zero) })), "A2");

        Assert.Equal("5", cell.Element(Main + "v")?.Value ?? cell.Value);
    }

    [Fact]
    public void ARecentDateWithoutText_StaysADate()
    {
        var cell = Cell(OmniTableExporter.ToXlsx(Single(new OmniTableExportCell(string.Empty) { Date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) })), "A2");

        Assert.Null(cell.Attribute("t"));
        Assert.Equal("36526", cell.Element(Main + "v")!.Value);
    }
}
