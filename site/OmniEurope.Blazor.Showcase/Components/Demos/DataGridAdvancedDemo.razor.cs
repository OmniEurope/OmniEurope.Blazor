using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataGridAdvancedDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    // A text column can narrow its operator menu to the conditions that make sense for it.
    private static readonly IReadOnlyList<OmniDataGridFilterOperator> ReferenceOperators =
    [
        OmniDataGridFilterOperator.Contains, OmniDataGridFilterOperator.StartsWith,
        OmniDataGridFilterOperator.EndsWith, OmniDataGridFilterOperator.DoesNotContain
    ];

    private IReadOnlyList<string> CountryValues { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridLines>> LineOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridPosition>> PagerOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridSelectionMode>> SelectionOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridFilterMode>> FilterOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridRowMode>> EditOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridRowMode>> ExpandOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDataGridPosition>> FooterOptions { get; set; } = [];

    private IReadOnlyList<GridRow> Rows { get; set; } = [];

    private IReadOnlyList<GridRow> Selection { get; set; } = [];

    private OmniDataGridLines Lines { get; set; } = OmniDataGridLines.Both;

    private OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    private OmniDataGridPosition Pager { get; set; } = OmniDataGridPosition.Bottom;

    private OmniDataGridSelectionMode Mode { get; set; } = OmniDataGridSelectionMode.Multiple;

    private OmniDataGridFilterMode Filters { get; set; } = OmniDataGridFilterMode.Advanced;

    private OmniDataGridRowMode Edit { get; set; } = OmniDataGridRowMode.Single;

    private OmniDataGridRowMode Expand { get; set; } = OmniDataGridRowMode.Multiple;

    private OmniDataGridPosition Footer { get; set; } = OmniDataGridPosition.Bottom;

    protected override void OnInitialized()
    {
        string belgium = Text["DemoGridCountryBelgium"], france = Text["DemoGridCountryFrance"],
            luxembourg = Text["DemoGridCountryLuxembourg"], netherlands = Text["DemoGridCountryNetherlands"];

        CountryValues = [belgium, france, luxembourg, netherlands];
        LineOptions =
        [
            new(OmniDataGridLines.None, Text["DemoGridAdvancedLinesNone"]),
            new(OmniDataGridLines.Horizontal, Text["DemoGridAdvancedLinesHorizontal"]),
            new(OmniDataGridLines.Vertical, Text["DemoGridAdvancedLinesVertical"]),
            new(OmniDataGridLines.Both, Text["DemoGridAdvancedLinesBoth"])
        ];
        DensityOptions =
        [
            new(OmniDensity.Compact, Text["DemoGridAdvancedDensityCompact"]),
            new(OmniDensity.Comfortable, Text["DemoGridAdvancedDensityComfortable"]),
            new(OmniDensity.Spacious, Text["DemoGridAdvancedDensitySpacious"])
        ];
        PagerOptions =
        [
            new(OmniDataGridPosition.Bottom, Text["DemoGridAdvancedPagerBottom"]),
            new(OmniDataGridPosition.Top, Text["DemoGridAdvancedPagerTop"]),
            new(OmniDataGridPosition.TopAndBottom, Text["DemoGridAdvancedPagerBoth"])
        ];
        SelectionOptions =
        [
            new(OmniDataGridSelectionMode.None, Text["DemoGridAdvancedSelectionNone"]),
            new(OmniDataGridSelectionMode.Single, Text["DemoGridAdvancedSelectionSingle"]),
            new(OmniDataGridSelectionMode.Multiple, Text["DemoGridAdvancedSelectionMultiple"])
        ];
        FilterOptions =
        [
            new(OmniDataGridFilterMode.Simple, Text["DemoGridAdvancedFilterSimple"]),
            new(OmniDataGridFilterMode.SimpleWithMenu, Text["DemoGridAdvancedFilterSimpleWithMenu"]),
            new(OmniDataGridFilterMode.Advanced, Text["DemoGridAdvancedFilterAdvanced"])
        ];
        EditOptions =
        [
            new(OmniDataGridRowMode.Single, Text["DemoGridAdvancedEditSingle"]),
            new(OmniDataGridRowMode.Multiple, Text["DemoGridAdvancedEditMultiple"])
        ];
        ExpandOptions =
        [
            new(OmniDataGridRowMode.Single, Text["DemoGridAdvancedExpandSingle"]),
            new(OmniDataGridRowMode.Multiple, Text["DemoGridAdvancedExpandMultiple"])
        ];
        FooterOptions =
        [
            new(OmniDataGridPosition.Bottom, Text["DemoGridAdvancedFooterBottom"]),
            new(OmniDataGridPosition.Top, Text["DemoGridAdvancedFooterTop"]),
            new(OmniDataGridPosition.TopAndBottom, Text["DemoGridAdvancedFooterBoth"])
        ];
        Rows =
        [
            new("D-2401", "Camille Durand", belgium, 12400, new DateTime(2026, 3, 2, 9, 15, 0)),
            new("D-2402", "Jonas Meyer", luxembourg, 8600, new DateTime(2026, 3, 2, 17, 40, 0)),
            new("D-2403", "Sofia Rossi", france, 21500, new DateTime(2026, 3, 9, 11, 5, 0)),
            new("D-2404", "Lars Jansen", netherlands, 4300, new DateTime(2026, 3, 12, 8, 30, 0)),
            new("D-2405", "Ana Silva", france, 17800, new DateTime(2026, 3, 16, 14, 0, 0)),
            new("D-2406", "Piet de Vries", netherlands, 9100, new DateTime(2026, 3, 20, 10, 45, 0)),
            new("D-2407", "Marie Lambert", belgium, 15200, new DateTime(2026, 3, 23, 16, 20, 0))
        ];
    }
}
