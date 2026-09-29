using System.Globalization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SpreadsheetDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    /// <summary>
    /// A quarter's budget: inputs typed as a person would, totals and averages as formulas. The
    /// second sheet reads the same instance, so an edit in the first shows there at once.
    /// </summary>
    private OmniSpreadsheetData Budget { get; set; } = default!;

    private string Active { get; set; } = "A1";

    /// <summary>
    /// Builds the budget in the reader's language: the row and column titles are translated and the
    /// decimal inputs are typed with the reader's decimal separator, as the sheet reads them. The
    /// formulas keep the names and the syntax the formula engine expects.
    /// </summary>
    protected override void OnInitialized() => Budget = OmniSpreadsheetData.FromRows(
    [
        [Text["DemoSpreadsheetItem"], Text["DemoSpreadsheetJanuary"], Text["DemoSpreadsheetFebruary"], Text["DemoSpreadsheetMarch"], Text["DemoSpreadsheetQuarter"]],
        [Text["DemoSpreadsheetRent"], "850", "850", "850", "=SUM(B2:D2)"],
        [Text["DemoSpreadsheetGroceries"], Typed(320.40m), "298", Typed(341.15m), "=SUM(B3:D3)"],
        [Text["DemoSpreadsheetEnergy"], "96", "104", "88", "=SUM(B4:D4)"],
        [Text["DemoSpreadsheetTransport"], "75", "75", "=C5*1.1", "=SUM(B5:D5)"],
        [Text["DemoSpreadsheetLeisure"], "120", "=B6/2", "140", "=SUM(B6:D6)"],
        [Text["DemoSpreadsheetTotal"], "=SUM(B2:B6)", "=SUM(C2:C6)", "=SUM(D2:D6)", "=SUM(E2:E6)"],
        [Text["DemoSpreadsheetAverage"], "=AVERAGE(B2:B6)", "=AVERAGE(C2:C6)", "=AVERAGE(D2:D6)", "=ROUND(E7/3;2)"]
    ],
    rowCount: 10,
    columnCount: 6);

    /// <summary>A decimal input written the way the reader types it.</summary>
    private static string Typed(decimal value) => value.ToString(CultureInfo.CurrentCulture);
}
