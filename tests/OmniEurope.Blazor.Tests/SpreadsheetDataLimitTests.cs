using System.Text.Json;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A sheet is bounded in rows, columns and cells: an address typed far away, a size read from a stored
/// JSON or a huge request is refused with a clear exception instead of allocating the memory it names.
/// </summary>
public sealed class SpreadsheetDataLimitTests
{
    [Fact]
    public void The_limits_are_the_documented_ones()
    {
        Assert.Equal(10_000, OmniSpreadsheetData.MaxRows);
        Assert.Equal(500, OmniSpreadsheetData.MaxColumns);
        Assert.Equal(250_000, OmniSpreadsheetData.MaxCells);
    }

    [Fact]
    public void Create_AtTheLimits_Works_AndBeyondThemIsRefused()
    {
        var wide = OmniSpreadsheetData.Create(1, OmniSpreadsheetData.MaxColumns);
        Assert.Equal(OmniSpreadsheetData.MaxColumns, wide.ColumnCount);
        Assert.Equal(OmniSpreadsheetData.MaxRows, OmniSpreadsheetData.Create(OmniSpreadsheetData.MaxRows, 1).RowCount);

        var columns = Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.Create(1, OmniSpreadsheetData.MaxColumns + 1));
        Assert.Equal("columnCount", columns.ParamName);
        var rows = Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.Create(OmniSpreadsheetData.MaxRows + 1, 1));
        Assert.Equal("rowCount", rows.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.Create(OmniSpreadsheetData.MaxRows, OmniSpreadsheetData.MaxColumns));
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.Create(int.MaxValue, int.MaxValue));
    }

    [Fact]
    public void WithInput_AtAFarAddress_IsRefusedWithoutAllocatingTheRowsBetween()
    {
        var sheet = OmniSpreadsheetData.Create(2, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.WithInput("A2000000", "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.WithInput("A9999999", "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.WithInput("ZZZ1", "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.WithInput(0, int.MaxValue - 1, "x"));
        Assert.Equal("x", sheet.WithInput("B2", "x").GetInput("B2"));
        Assert.Equal("x", sheet.WithInput(OmniSpreadsheetData.MaxRows - 1, 0, "x").GetInput(OmniSpreadsheetData.MaxRows - 1, 0));
    }

    [Fact]
    public void AddRow_AndAddColumn_StopAtTheLimits()
    {
        var full = OmniSpreadsheetData.Create(OmniSpreadsheetData.MaxRows, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => full.AddRow());
        var wide = OmniSpreadsheetData.Create(1, OmniSpreadsheetData.MaxColumns);
        Assert.Throws<ArgumentOutOfRangeException>(() => wide.AddColumn());
        Assert.Equal(3, OmniSpreadsheetData.Create(1, 2).AddColumn().ColumnCount);
    }

    [Fact]
    public void FromRows_RefusesMoreRowsOrColumnsThanTheLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.FromRows([["a"]], columnCount: OmniSpreadsheetData.MaxColumns + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.FromRows([["a"]], rowCount: OmniSpreadsheetData.MaxRows + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.FromRows([Enumerable.Repeat("a", OmniSpreadsheetData.MaxColumns + 1)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.FromRows(Enumerable.Repeat<IEnumerable<string?>>(["a"], OmniSpreadsheetData.MaxRows + 1)));
        Assert.Equal(2, OmniSpreadsheetData.FromRows([["a", "b"]]).ColumnCount);
    }

    [Theory]
    [InlineData("""{"columnCount":2000000000,"rows":[["a"]]}""")]
    [InlineData("""{"columnCount":5000000,"rows":[["a"]]}""")]
    public void FromJson_WithAnOversizedColumnCount_ThrowsTheDocumentedJsonException(string json)
    {
        Assert.Throws<JsonException>(() => OmniSpreadsheetData.FromJson(json));
    }

    [Fact]
    public void FromJson_WithTooManyRows_ThrowsTheDocumentedJsonException()
    {
        var rows = string.Join(',', Enumerable.Repeat("[\"a\"]", OmniSpreadsheetData.MaxRows + 1));

        Assert.Throws<JsonException>(() => OmniSpreadsheetData.FromJson("{\"columnCount\":1,\"rows\":[" + rows + "]}"));
    }
}

/// <summary>The sheet component stops adding rows or columns at the limits instead of throwing.</summary>
public sealed class SpreadsheetLimitComponentTests : OmniBunitContext
{
    [Fact]
    public async Task AddRow_AtTheLastRow_IsDisabledAndDoesNothing()
    {
        var published = new List<OmniSpreadsheetData>();
        var sheet = Render<OmniSpreadsheet>(parameters => parameters
            .Add(component => component.Value, OmniSpreadsheetData.Create(OmniSpreadsheetData.MaxRows, 1))
            .Add(component => component.ValueChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<OmniSpreadsheetData>(this, published.Add)));

        Assert.True(sheet.Find(".omni-spreadsheet__add-row").HasAttribute("disabled"));
        Assert.False(sheet.Find(".omni-spreadsheet__add-column").HasAttribute("disabled"));
        await sheet.InvokeAsync(sheet.Instance.AddRowAsync);
        Assert.Empty(published);
        await sheet.InvokeAsync(sheet.Instance.AddColumnAsync);
        Assert.Single(published);
    }

    [Fact]
    public async Task AddColumn_AtTheLastColumn_IsDisabledAndDoesNothing()
    {
        var sheet = Render<OmniSpreadsheet>(parameters => parameters.Add(component => component.Value, OmniSpreadsheetData.Create(1, OmniSpreadsheetData.MaxColumns)));

        Assert.True(sheet.Find(".omni-spreadsheet__add-column").HasAttribute("disabled"));
        await sheet.InvokeAsync(sheet.Instance.AddColumnAsync);
        Assert.Equal(OmniSpreadsheetData.MaxColumns, sheet.Instance.Value!.ColumnCount);
    }
}
