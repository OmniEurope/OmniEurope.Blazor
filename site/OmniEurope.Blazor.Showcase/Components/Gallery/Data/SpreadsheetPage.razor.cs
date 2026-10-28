using OmniEurope.Blazor.Showcase.Examples.Data;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Spreadsheet page: a quarterly budget with formulas, whose editing, bars, lines and column width are settings.</summary>
public partial class SpreadsheetPage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;

    private SpreadsheetExample? Sheet { get; set; }

    private bool ReadOnly { get; set; }

    private bool ShowFormulaBar { get; set; } = true;

    private bool ShowGridLines { get; set; } = true;

    private bool AllowAddRows { get; set; } = true;

    private bool AllowAddColumns { get; set; } = true;

    private bool ShowJson { get; set; }

    private bool ShowMirror { get; set; } = true;

    private SheetWidth Width { get; set; } = SheetWidth.Normal;

    // The column width is the package's own variable, redefined by a class of the page around the example.
    private string StageClass => Width switch
    {
        SheetWidth.Narrow => "gallery-sheet-stage gallery-sheet-narrow",
        SheetWidth.Wide => "gallery-sheet-stage gallery-sheet-wide",
        _ => "gallery-sheet-stage"
    };

    private IReadOnlyList<OmniOption<SheetWidth>> WidthOptions =>
    [
        new(SheetWidth.Narrow, Text["SpreadsheetWidthNarrow"]),
        new(SheetWidth.Normal, Text["SpreadsheetWidthNormal"]),
        new(SheetWidth.Wide, Text["SpreadsheetWidthWide"])
    ];

    private void ResetSheet() => Sheet?.Reset();

    private enum SheetWidth
    {
        Narrow,
        Normal,
        Wide
    }
}
