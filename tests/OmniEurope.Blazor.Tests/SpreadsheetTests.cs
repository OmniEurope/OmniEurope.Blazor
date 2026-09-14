using System.Text.Json;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The spreadsheet: the formula language computed over the sheet model, the model's own operations
/// and its JSON form, then the component driven through its cells, its keyboard and its formula bar.
/// </summary>
public sealed class SpreadsheetTests : OmniBunitContext
{
    private static OmniSpreadsheetData Sheet(params string[][] rows) => OmniSpreadsheetData.FromRows(rows);

    private static OmniSpreadsheetValue Compute(string formula, params string[][] rows) =>
        Sheet([.. rows, [formula]]).Evaluate(rows.Length, 0);

    // ---- formulas -------------------------------------------------------------------------------

    [Theory]
    [InlineData("=1+2*3", 7d)]
    [InlineData("=(1+2)*3", 9d)]
    [InlineData("=2^3", 8d)]
    [InlineData("=7/2", 3.5d)]
    [InlineData("=-3+10", 7d)]
    [InlineData("=50%", 0.5d)]
    [InlineData("= 1.5 * 2 ", 3d)]
    [InlineData("=1E3/4", 250d)]
    public void Formula_ComputesArithmeticWithTheUsualPrecedence(string formula, double expected)
    {
        var value = Compute(formula);

        Assert.Equal(OmniSpreadsheetValueKind.Number, value.Kind);
        Assert.Equal(expected, value.Number, 10);
    }

    [Fact]
    public void Formula_ReadsReferencesWhateverTheirCaseOrAnchors()
    {
        var sheet = Sheet(["2", "3"], ["4", "=A1+B1*a2"], ["=$A$1+B$2", "=B2-A3"]);

        Assert.Equal(14d, sheet.Evaluate("B2").Number);
        Assert.Equal(16d, sheet.Evaluate("A3").Number);
        Assert.Equal(-2d, sheet.Evaluate("B3").Number);
    }

    [Fact]
    public void Aggregates_ReadRangesAndSkipTheirTextAndEmptyCells()
    {
        string[][] rows = [["10"], ["Loyer"], [""], ["20"], ["30"]];

        Assert.Equal(60d, Compute("=SUM(A1:A5)", rows).Number);
        Assert.Equal(20d, Compute("=AVERAGE(A1:A5)", rows).Number);
        Assert.Equal(10d, Compute("=MIN(A1:A5)", rows).Number);
        Assert.Equal(30d, Compute("=MAX(A1:A5)", rows).Number);
        Assert.Equal(3d, Compute("=COUNT(A1:A5)", rows).Number);
        // A range written backwards is the same range, and a lone reference reads like one cell.
        Assert.Equal(60d, Compute("=SUM(A5:A1)", rows).Number);
        Assert.Equal(10d, Compute("=SUM(A1, A2)", rows).Number);
    }

    [Fact]
    public void Functions_AcceptBothArgumentSeparatorsAndTheirFrenchNames()
    {
        string[][] rows = [["1", "2", "3"]];

        Assert.Equal(16d, Compute("=SOMME(A1:C1;10)", rows).Number);
        Assert.Equal(16d, Compute("=sum(A1:C1,10)", rows).Number);
        Assert.Equal(2d, Compute("=MOYENNE(A1:C1)", rows).Number);
        Assert.Equal(3d, Compute("=NB(A1:C1)", rows).Number);
        Assert.Equal(2.35d, Compute("=ROUND(2.345;2)", rows).Number, 10);
        Assert.Equal(1200d, Compute("=ARRONDI(1234,-2)", rows).Number, 10);
        Assert.Equal(4d, Compute("=ABS(-4)", rows).Number);
        Assert.Equal(12d, Compute("=SUM(MAX(A1:C1), 3*3)", rows).Number);
    }

    [Theory]
    [InlineData("=A1/0", OmniSpreadsheetValue.DivisionByZeroError)]
    [InlineData("=A1/B1", OmniSpreadsheetValue.DivisionByZeroError)]
    [InlineData("=AVERAGE(B1:B1)", OmniSpreadsheetValue.DivisionByZeroError)]
    [InlineData("=Z99", OmniSpreadsheetValue.ReferenceError)]
    [InlineData("=SUM(A1:A99)", OmniSpreadsheetValue.ReferenceError)]
    [InlineData("=FOO(1)", OmniSpreadsheetValue.NameError)]
    [InlineData("=total", OmniSpreadsheetValue.NameError)]
    [InlineData("=C1+1", OmniSpreadsheetValue.ValueError)]
    [InlineData("=\"a\"*2", OmniSpreadsheetValue.ValueError)]
    [InlineData("=A1:A2", OmniSpreadsheetValue.ValueError)]
    [InlineData("=ROUND(1)", OmniSpreadsheetValue.ValueError)]
    [InlineData("=1+", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=(1+2", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=1 2", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=10^400", OmniSpreadsheetValue.NumberError)]
    public void Formula_ReportsWhatCannotBeComputedAsAnErrorCode(string formula, string code)
    {
        var value = Compute(formula, ["8", "", "abc"], ["2"]);

        Assert.True(value.IsError);
        Assert.Equal(code, value.Text);
        Assert.Equal(code, value.ToDisplayString());
    }

    [Fact]
    public void Formula_PassesAnErrorOnToEveryCellThatReadsIt()
    {
        var sheet = Sheet(["=1/0", "=A1+1", "=SUM(A1:B1)"]);

        Assert.Equal(OmniSpreadsheetValue.DivisionByZeroError, sheet.Evaluate("B1").Text);
        Assert.Equal(OmniSpreadsheetValue.DivisionByZeroError, sheet.Evaluate("C1").Text);
    }

    [Fact]
    public void Formula_ThatReachesBackToItsOwnCellIsACycle()
    {
        var sheet = Sheet(["=B1", "=A1+1", "=C1"], ["=A1", "5", "=B2*2"]);

        Assert.Equal(OmniSpreadsheetValue.CircularError, sheet.Evaluate("A1").Text);
        Assert.Equal(OmniSpreadsheetValue.CircularError, sheet.Evaluate("B1").Text);
        Assert.Equal(OmniSpreadsheetValue.CircularError, sheet.Evaluate("C1").Text);
        Assert.Equal(OmniSpreadsheetValue.CircularError, sheet.Evaluate("A2").Text);
        // Cells outside the cycle are untouched by it.
        Assert.Equal(10d, sheet.Evaluate("C2").Number);
    }

    [Fact]
    public void Formula_OnTextOrAnEmptyCellReadsAsTextOrZero()
    {
        var sheet = Sheet(["Loyer", "", "=A1", "=B1", "=\"Total \"\"net\"\"\""]);

        Assert.Equal("Loyer", sheet.Evaluate("C1").Text);
        Assert.Equal(OmniSpreadsheetValueKind.Number, sheet.Evaluate("D1").Kind);
        Assert.Equal(0d, sheet.Evaluate("D1").Number);
        Assert.Equal("Total \"net\"", sheet.Evaluate("E1").Text);
    }

    [Fact]
    public void Input_ReadsNumbersInTheCultureOrWithAPointAndKeepsForcedText()
    {
        var sheet = Sheet(["12,5", "12.5", "12%", "'123", "1 250", " 7 ", "abc"]);

        Assert.Equal(12.5d, sheet.Evaluate("A1").Number);
        Assert.Equal(12.5d, sheet.Evaluate("B1").Number);
        Assert.Equal(0.12d, sheet.Evaluate("C1").Number, 10);
        Assert.Equal(OmniSpreadsheetValueKind.Text, sheet.Evaluate("D1").Kind);
        Assert.Equal("123", sheet.Evaluate("D1").Text);
        Assert.Equal(OmniSpreadsheetValueKind.Text, sheet.Evaluate("E1").Kind);
        Assert.Equal(7d, sheet.Evaluate("F1").Number);
        Assert.Equal(OmniSpreadsheetValueKind.Text, sheet.Evaluate("G1").Kind);
        // Text that reads as a number still counts once referenced in arithmetic.
        Assert.Equal(25d, Compute("=D1+D1*0+A1*0+2*A1", ["12,5", "", "", "'0"]).Number);
    }

    [Fact]
    public void Value_DisplaysNumbersInTheCurrentCulture()
    {
        Assert.Equal("1 250,5", OmniSpreadsheetValue.FromNumber(1250.5d).ToDisplayString());
        Assert.Equal("0,333333333", OmniSpreadsheetValue.FromNumber(1d / 3d).ToDisplayString().Substring(0, 11));
        Assert.Equal("-3", OmniSpreadsheetValue.FromNumber(-3d).ToDisplayString());
        Assert.Equal(string.Empty, OmniSpreadsheetValue.Empty.ToDisplayString());
    }

    // ---- model ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ColumnName_CountsInLettersPastZ(int column, string expected) =>
        Assert.Equal(expected, OmniSpreadsheetData.ColumnName(column));

    [Fact]
    public void Model_ChangesReturnNewSheetsAndGrowToReachAPosition()
    {
        var empty = OmniSpreadsheetData.Create(2, 2);
        var edited = empty.WithInput("C4", "=1+1");

        Assert.Equal(string.Empty, empty.GetInput("C4"));
        Assert.Equal((2, 2), (empty.RowCount, empty.ColumnCount));
        Assert.Equal((4, 3), (edited.RowCount, edited.ColumnCount));
        Assert.Equal("=1+1", edited.GetInput(3, 2));
        Assert.Equal(2d, edited.Evaluate("C4").Number);
        Assert.Equal("B3", OmniSpreadsheetData.Address(2, 1));

        var grown = edited.AddRow().AddColumn();
        Assert.Equal((5, 4), (grown.RowCount, grown.ColumnCount));
        Assert.All(grown.Rows, row => Assert.Equal(4, row.Count));
        Assert.Throws<ArgumentException>(() => edited.WithInput("3C", "x"));
    }

    [Fact]
    public void Json_RoundTripsTheInputsAndSquaresOffUnevenRows()
    {
        var sheet = Sheet(["Poste", "Montant"], ["Loyer", "800"], ["Total", "=SUM(B2:B2)"]);

        var json = sheet.ToJson();
        Assert.Equal("{\"columnCount\":2,\"rows\":[[\"Poste\",\"Montant\"],[\"Loyer\",\"800\"],[\"Total\",\"=SUM(B2:B2)\"]]}", json);

        var back = OmniSpreadsheetData.FromJson(json);
        Assert.Equal(sheet.Rows.SelectMany(row => row), back.Rows.SelectMany(row => row));
        Assert.Equal(800d, back.Evaluate("B3").Number);

        var uneven = OmniSpreadsheetData.FromJson("{\"columnCount\":1,\"rows\":[[\"a\"],[\"b\",\"c\",\"d\"]]}");
        Assert.Equal(3, uneven.ColumnCount);
        Assert.Equal(["a", "", ""], uneven.Rows[0]);
        Assert.Throws<JsonException>(() => OmniSpreadsheetData.FromJson("[1,2]"));
    }

    // ---- component ------------------------------------------------------------------------------

    [Fact]
    public void Sheet_RendersLettersNumbersAndComputedCells()
    {
        var host = Render<SpreadsheetTestHost>();

        Assert.Equal(["A", "B", "C"], host.FindAll("thead th").Select(th => th.TextContent));
        Assert.Equal(["1", "2", "3", "4"], host.FindAll("tbody th").Select(th => th.TextContent));
        var total = host.Find("#sheet-r3-c1");
        Assert.Equal("1 120,5", total.TextContent.Trim());
        Assert.Contains("omni-spreadsheet__cell--number", total.ClassName, StringComparison.Ordinal);
        Assert.Equal("1 098", host.Find("#sheet-r3-c2").TextContent.Trim());
        Assert.DoesNotContain("omni-spreadsheet__cell--number", host.Find("#sheet-r0-c0").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Click_SelectsACellAndTheFormulaBarShowsItsInput()
    {
        var host = Render<SpreadsheetTestHost>();

        host.Find("#sheet-r3-c1").Click();

        Assert.Equal("true", host.Find("#sheet-r3-c1").GetAttribute("aria-selected"));
        Assert.Equal("sheet-r3-c1", host.Find(".omni-spreadsheet__viewport").GetAttribute("aria-activedescendant"));
        Assert.Equal("B4", host.Find(".omni-spreadsheet__address").TextContent);
        Assert.Equal("=SUM(B2:B3)", host.Find(".omni-spreadsheet__formula-input").GetAttribute("value"));
        Assert.Contains("omni-spreadsheet__column-header--active", host.FindAll("thead th")[1].ClassName, StringComparison.Ordinal);
        Assert.Equal(["B4"], host.Instance.ActiveCells);
    }

    [Theory]
    [InlineData("ArrowDown", false, false, "A2")]
    [InlineData("ArrowRight", false, false, "B1")]
    [InlineData("Tab", false, false, "B1")]
    [InlineData("End", false, false, "C1")]
    [InlineData("ArrowDown", false, true, "A4")]
    [InlineData("End", false, true, "C4")]
    [InlineData("PageDown", false, false, "A4")]
    [InlineData("ArrowUp", false, false, "A1")]
    [InlineData("ArrowLeft", false, false, "A1")]
    public void Keyboard_MovesTheActiveCellAndStopsAtTheEdges(string key, bool shift, bool control, string expected)
    {
        var host = Render<SpreadsheetTestHost>();

        host.Find(".omni-spreadsheet__viewport").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = key, ShiftKey = shift, CtrlKey = control });

        Assert.Equal(expected, host.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void Keyboard_ShiftTabGoesBackAndTabAtTheRowEndStays()
    {
        var host = Render<SpreadsheetTestHost>();
        var grid = host.Find(".omni-spreadsheet__viewport");

        grid.KeyDown("End");
        Assert.Equal("2", host.Find(".omni-spreadsheet__viewport").GetAttribute("data-active-column"));
        host.Find(".omni-spreadsheet__viewport").KeyDown("Tab");
        // Nothing moves: at the edge Tab is left to the browser, which takes the focus out.
        Assert.Equal("C1", host.Find(".omni-spreadsheet__address").TextContent);

        host.Find(".omni-spreadsheet__viewport").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Tab", ShiftKey = true });
        Assert.Equal("B1", host.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void Typing_ReplacesTheCellAndEnterCommitsAndMovesDown()
    {
        var host = Render<SpreadsheetTestHost>();
        host.Find("#sheet-r1-c1").Click();

        host.Find(".omni-spreadsheet__viewport").KeyDown("9");
        var editor = host.Find("#sheet-r1-c1 .omni-spreadsheet__editor");
        Assert.Equal("9", editor.GetAttribute("value"));
        Assert.Equal("replace", editor.GetAttribute("data-omni-sheet-mode"));
        // The formula bar follows the draft.
        Assert.Equal("9", host.Find(".omni-spreadsheet__formula-input").GetAttribute("value"));

        editor.Input("950");
        host.Find("#sheet-r1-c1 .omni-spreadsheet__editor").KeyDown("Enter");

        Assert.Equal("950", host.Instance.Data.GetInput("B2"));
        Assert.Equal("B3", host.Find(".omni-spreadsheet__address").TextContent);
        Assert.Empty(host.FindAll(".omni-spreadsheet__editor"));
        // The total is recomputed from the new input.
        Assert.Equal("1 270,5", host.Find("#sheet-r3-c1").TextContent.Trim());
    }

    [Fact]
    public void EditMode_KeepsTheInputAndTabCommitsToTheRight()
    {
        var host = Render<SpreadsheetTestHost>();
        host.Find("#sheet-r3-c1").Click();

        host.Find(".omni-spreadsheet__viewport").KeyDown("Enter");
        var editor = host.Find("#sheet-r3-c1 .omni-spreadsheet__editor");
        Assert.Equal("=SUM(B2:B3)", editor.GetAttribute("value"));
        Assert.Equal("edit", editor.GetAttribute("data-omni-sheet-mode"));

        editor.Input("=MAX(B2:B3)*2");
        host.Find("#sheet-r3-c1 .omni-spreadsheet__editor").KeyDown("Tab");

        Assert.Equal("=MAX(B2:B3)*2", host.Instance.Data.GetInput("B4"));
        Assert.Equal("1 600", host.Find("#sheet-r3-c1").TextContent.Trim());
        Assert.Equal("C4", host.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void Escape_CancelsTheEditAndDeleteEmptiesTheCell()
    {
        var host = Render<SpreadsheetTestHost>();
        var original = host.Instance.Data;
        host.Find("#sheet-r1-c1").Click();

        host.Find(".omni-spreadsheet__viewport").KeyDown("F2");
        host.Find(".omni-spreadsheet__editor").Input("0");
        host.Find(".omni-spreadsheet__editor").KeyDown("Escape");

        Assert.Same(original, host.Instance.Data);
        Assert.Equal("800", host.Find("#sheet-r1-c1").TextContent.Trim());

        host.Find(".omni-spreadsheet__viewport").KeyDown("Delete");

        Assert.Equal(string.Empty, host.Instance.Data.GetInput("B2"));
        Assert.Equal("320,5", host.Find("#sheet-r3-c1").TextContent.Trim());
    }

    [Fact]
    public void DoubleClick_EditsAndBlurCommits()
    {
        var host = Render<SpreadsheetTestHost>();

        host.Find("#sheet-r0-c0").DoubleClick();
        host.Find(".omni-spreadsheet__editor").Input("Charges");
        host.Find(".omni-spreadsheet__editor").Blur();

        Assert.Equal("Charges", host.Instance.Data.GetInput("A1"));
    }

    [Fact]
    public void FormulaBar_EditsTheActiveCell()
    {
        var host = Render<SpreadsheetTestHost>();
        host.Find("#sheet-r2-c2").Click();

        var bar = host.Find(".omni-spreadsheet__formula-input");
        bar.Focus();
        bar.Input("=B3+2");
        // The cell shows the draft while the bar is being typed into.
        Assert.Equal("=B3+2", host.Find("#sheet-r2-c2").TextContent.Trim());
        host.Find(".omni-spreadsheet__formula-input").KeyDown("Enter");

        Assert.Equal("=B3+2", host.Instance.Data.GetInput("C3"));
        Assert.Equal("322,5", host.Find("#sheet-r2-c2").TextContent.Trim());
        Assert.Equal("C4", host.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void Toolbar_AddsARowAndAColumn()
    {
        var host = Render<SpreadsheetTestHost>();

        host.Find(".omni-spreadsheet__add-row").Click();
        host.Find(".omni-spreadsheet__add-column").Click();

        Assert.Equal((5, 4), (host.Instance.Data.RowCount, host.Instance.Data.ColumnCount));
        Assert.Equal(["A", "B", "C", "D"], host.FindAll("thead th").Select(th => th.TextContent));
        Assert.Equal(5, host.FindAll("tbody tr").Count);
        Assert.Equal("Ajouter une ligne", host.Find(".omni-spreadsheet__add-row").GetAttribute("aria-label"));
    }

    [Fact]
    public void ReadOnly_SelectsButNeverEdits()
    {
        var host = Render<SpreadsheetTestHost>(parameters => parameters.Add(component => component.ReadOnly, true));
        var original = host.Instance.Data;

        host.Find("#sheet-r1-c1").Click();
        host.Find(".omni-spreadsheet__viewport").KeyDown("5");
        host.Find(".omni-spreadsheet__viewport").KeyDown("Delete");
        host.Find("#sheet-r1-c1").DoubleClick();

        Assert.Same(original, host.Instance.Data);
        Assert.Empty(host.FindAll(".omni-spreadsheet__editor"));
        Assert.Empty(host.FindAll(".omni-spreadsheet__toolbar"));
        Assert.True(host.Find(".omni-spreadsheet__formula-input").HasAttribute("readonly"));
        Assert.Equal("true", host.Find(".omni-spreadsheet__viewport").GetAttribute("aria-readonly"));
        Assert.Equal("B2", host.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void Options_HideTheFormulaBarAndTheGridLines()
    {
        var host = Render<SpreadsheetTestHost>(parameters => parameters
            .Add(component => component.ShowFormulaBar, false)
            .Add(component => component.ShowGridLines, false)
            .Add(component => component.AllowAddColumns, false));

        Assert.Empty(host.FindAll(".omni-spreadsheet__formula-bar"));
        Assert.Contains("omni-spreadsheet--no-lines", host.Find(".omni-spreadsheet").ClassName, StringComparison.Ordinal);
        Assert.Single(host.FindAll(".omni-spreadsheet__toolbar button"));
    }

    [Fact]
    public void Script_IsAttachedToTheSheetOnce()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-spreadsheet.js");
        var host = Render<SpreadsheetTestHost>();

        host.Find("#sheet-r1-c1").Click();
        host.Find(".omni-spreadsheet__viewport").KeyDown("ArrowDown");

        Assert.Single(module.Invocations["attach"]);
        Assert.NotEmpty(module.Invocations["reveal"]);
        Assert.Equal("sheet-r2-c1", module.Invocations["reveal"][^1].Arguments[1]);
    }
}
