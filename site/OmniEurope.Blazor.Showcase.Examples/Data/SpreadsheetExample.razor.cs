using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A quarterly budget in a spreadsheet whose behaviour is set by parameters, with its model read back below it.</summary>
public partial class SpreadsheetExample
{
    /// <summary>Whether the cells can be selected and read but never changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether the bar showing the active address and the typed content is displayed.</summary>
    [Parameter]
    public bool ShowFormulaBar { get; set; } = true;

    /// <summary>Whether the lines between cells are drawn.</summary>
    [Parameter]
    public bool ShowGridLines { get; set; } = true;

    /// <summary>Whether a button above the sheet adds a row at the bottom.</summary>
    [Parameter]
    public bool AllowAddRows { get; set; } = true;

    /// <summary>Whether a button above the sheet adds a column on the right.</summary>
    [Parameter]
    public bool AllowAddColumns { get; set; } = true;

    /// <summary>Whether the model is shown as it would be saved: the inputs only, formulas included.</summary>
    [Parameter]
    public bool ShowJson { get; set; }

    /// <summary>Whether a second, read-only sheet shows the same model.</summary>
    [Parameter]
    public bool ShowMirror { get; set; } = true;

    private OmniSpreadsheetData Budget { get; set; } = CreateBudget();

    private string ActiveCell { get; set; } = "A1";

    private string ActiveEcho
    {
        get
        {
            var input = Budget.GetInput(ActiveCell);
            return string.Create(
                CultureInfo.CurrentCulture,
                $"{ActiveCell}: {(input.Length > 0 ? input : "(empty)")} shows {Budget.Evaluate(ActiveCell).ToDisplayString()}");
        }
    }

    // The total of the quarter, read through the model's own API: it follows every entry.
    private string TotalEcho => Budget.Evaluate("E8").ToDisplayString();

    private string SizeEcho => string.Create(CultureInfo.CurrentCulture, $"{Budget.RowCount} rows, {Budget.ColumnCount} columns");

    /// <summary>Puts the sample budget back.</summary>
    public void Reset()
    {
        Budget = CreateBudget();
        ActiveCell = "A1";
        StateHasChanged();
    }

    private void OnActiveCellChanged(string address) => ActiveCell = address;

    // A quarter's budget: the amounts are typed as a person would, the totals and averages are
    // formulas, the last row is the ratio of expenses to income. A decimal amount is written with the
    // reader's decimal separator, as the sheet reads it.
    private static OmniSpreadsheetData CreateBudget() => OmniSpreadsheetData.FromRows(
        [
            ["Item", "January", "February", "March", "Quarter"],
            ["Rent", "850", "850", "850", "=SUM(B2:D2)"],
            ["Groceries", Typed(320.40m), "298", Typed(341.15m), "=SUM(B3:D3)"],
            ["Energy", "96", "104", "88", "=SUM(B4:D4)"],
            ["Transport", "75", "75", "=C5*1.1", "=SUM(B5:D5)"],
            ["Leisure", "120", "=B6/2", "140", "=SUM(B6:D6)"],
            ["Savings", "=B10-B8", "=C10-C8", "=D10-D8", "=SUM(B7:D7)"],
            ["Expenses", "=SUM(B2:B6)", "=SUM(C2:C6)", "=SUM(D2:D6)", "=SUM(E2:E6)"],
            ["Average", "=ROUND(AVERAGE(B2:B6);2)", "=ROUND(AVERAGE(C2:C6);2)", "=ROUND(AVERAGE(D2:D6);2)", "=MAX(B8:D8)"],
            ["Income", "1800", "1800", "1950", "=SUM(B10:D10)"],
            ["Expenses %", "=ROUND(B8/B10*100;1)", "=ROUND(C8/C10*100;1)", "=ROUND(D8/D10*100;1)", "=ROUND(E8/E10*100;1)"]
        ],
        rowCount: 14,
        columnCount: 7);

    private static string Typed(decimal value) => value.ToString(CultureInfo.CurrentCulture);
}
