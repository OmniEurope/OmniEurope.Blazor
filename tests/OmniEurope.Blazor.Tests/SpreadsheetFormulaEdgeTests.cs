using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The formula language at its edges, over the sheet A1 = 8, B1 empty, C1 = abc, A2 = 2: signs, empty
/// calls, references among the arguments, numbers written with an exponent, and every text the parser
/// refuses; then the A1 addresses it reads.
/// </summary>
public sealed class SpreadsheetFormulaEdgeTests
{
    private static OmniSpreadsheetValue Compute(string formula) =>
        OmniSpreadsheetData.FromRows([["8", "", "abc"], ["2"], [formula]]).Evaluate(2, 0);

    [Theory]
    [InlineData("=+5", 5d)]
    [InlineData("=SUM()", 0d)]
    [InlineData("=SUM($A$1, A2)", 10d)]
    [InlineData("=SUM(A1 + 1; A2)", 11d)]
    [InlineData("=SUM(A1;A2)", 10d)]
    [InlineData("=ABS(A1)", 8d)]
    [InlineData("=1e2", 100d)]
    [InlineData("=1E+2", 100d)]
    [InlineData("=2e-1", 0.2d)]
    public void Formula_Computes(string formula, double expected)
    {
        var value = Compute(formula);

        Assert.Equal(OmniSpreadsheetValueKind.Number, value.Kind);
        Assert.Equal(expected, value.Number, 10);
    }

    [Theory]
    [InlineData("=#", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=SUM(A1:1)", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=SUM(1,", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=1e", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=1e+", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=1..2", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=\"abc", OmniSpreadsheetValue.SyntaxError)]
    [InlineData("=SUM(\"a\")", OmniSpreadsheetValue.ValueError)]
    [InlineData("=ABS(A1:A2)", OmniSpreadsheetValue.ValueError)]
    [InlineData("=ABS(C1)", OmniSpreadsheetValue.ValueError)]
    [InlineData("=ABS(Z99)", OmniSpreadsheetValue.ReferenceError)]
    [InlineData("=MY_FN.X(1)", OmniSpreadsheetValue.NameError)]
    public void Formula_ReportsWhatItCannotRead(string formula, string code)
    {
        var value = Compute(formula);

        Assert.True(value.IsError);
        Assert.Equal(code, value.Text);
    }

    [Fact]
    public void CellOutsideTheSheet_IsEmpty()
    {
        var sheet = OmniSpreadsheetData.FromRows([["1"]]);

        Assert.Equal(OmniSpreadsheetValue.Empty, sheet.Evaluate(5, 5));
        Assert.Equal(OmniSpreadsheetValue.Empty, new SpreadsheetEvaluator(sheet).Evaluate(-1, 0));
    }

    [Theory]
    [InlineData("", false, -1, -1)]
    [InlineData("$", false, -1, -1)]
    [InlineData("b3", true, 2, 1)]
    [InlineData("$AB$10", true, 9, 27)]
    [InlineData("A", false, -1, -1)]
    [InlineData("A0", false, -1, -1)]
    [InlineData("A1x", false, -1, -1)]
    [InlineData("A12345678", false, -1, -1)]
    [InlineData("ABCD1", false, -1, -1)]
    public void Address_ReadsA1AndRefusesTheRest(string text, bool read, int row, int column)
    {
        Assert.Equal(read, SpreadsheetAddress.TryParse(text, out var parsedRow, out var parsedColumn));
        Assert.Equal((row, column), (parsedRow, parsedColumn));
    }
}
