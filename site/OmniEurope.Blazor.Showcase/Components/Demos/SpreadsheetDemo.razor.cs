using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SpreadsheetDemo
{
    /// <summary>
    /// A quarter's budget: inputs typed as a person would, totals and averages as formulas. The
    /// second sheet reads the same instance, so an edit in the first shows there at once.
    /// </summary>
    private OmniSpreadsheetData Budget { get; set; } = OmniSpreadsheetData.FromRows(
    [
        ["Poste", "Janvier", "Février", "Mars", "Trimestre"],
        ["Loyer", "850", "850", "850", "=SUM(B2:D2)"],
        ["Courses", "320,40", "298", "341,15", "=SUM(B3:D3)"],
        ["Énergie", "96", "104", "88", "=SUM(B4:D4)"],
        ["Transport", "75", "75", "=C5*1.1", "=SUM(B5:D5)"],
        ["Loisirs", "120", "=B6/2", "140", "=SUM(B6:D6)"],
        ["Total", "=SUM(B2:B6)", "=SUM(C2:C6)", "=SUM(D2:D6)", "=SUM(E2:E6)"],
        ["Moyenne", "=AVERAGE(B2:B6)", "=AVERAGE(C2:C6)", "=AVERAGE(D2:D6)", "=ROUND(E7/3;2)"]
    ],
    rowCount: 10,
    columnCount: 6);

    private string Active { get; set; } = "A1";
}
