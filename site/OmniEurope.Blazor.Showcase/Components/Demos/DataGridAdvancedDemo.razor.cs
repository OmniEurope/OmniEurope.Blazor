namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataGridAdvancedDemo
{
    private static readonly IReadOnlyList<string> CountryValues = ["Belgique", "France", "Luxembourg", "Pays-Bas"];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridLines>> LineOptions =
    [
        new(OmniDataGridLines.Default, "Réglage du thème"),
        new(OmniDataGridLines.None, "Aucune"),
        new(OmniDataGridLines.Horizontal, "Horizontales"),
        new(OmniDataGridLines.Vertical, "Verticales"),
        new(OmniDataGridLines.Both, "Les deux")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =
    [
        new(OmniDensity.Compact, "Compacte"),
        new(OmniDensity.Comfortable, "Confortable"),
        new(OmniDensity.Spacious, "Aérée")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridPagerPosition>> PagerOptions =
    [
        new(OmniDataGridPagerPosition.Bottom, "En bas"),
        new(OmniDataGridPagerPosition.Top, "En haut"),
        new(OmniDataGridPagerPosition.TopAndBottom, "En haut et en bas")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridSelectionMode>> SelectionOptions =
    [
        new(OmniDataGridSelectionMode.None, "Aucune"),
        new(OmniDataGridSelectionMode.Single, "Une ligne"),
        new(OmniDataGridSelectionMode.Multiple, "Plusieurs lignes")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridFilterMode>> FilterOptions =
    [
        new(OmniDataGridFilterMode.Simple, "Champ dans l'entête"),
        new(OmniDataGridFilterMode.SimpleWithMenu, "Champ et menu"),
        new(OmniDataGridFilterMode.Advanced, "Menu à deux conditions")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridEditMode>> EditOptions =
    [
        new(OmniDataGridEditMode.Single, "Une ligne à la fois"),
        new(OmniDataGridEditMode.Multiple, "Plusieurs lignes")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridExpandMode>> ExpandOptions =
    [
        new(OmniDataGridExpandMode.Single, "Un groupe ouvert"),
        new(OmniDataGridExpandMode.Multiple, "Plusieurs groupes ouverts")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniDataGridFooterPosition>> FooterOptions =
    [
        new(OmniDataGridFooterPosition.Bottom, "Sous le tableau"),
        new(OmniDataGridFooterPosition.Top, "Au-dessus du tableau")
    ];

    private static readonly IReadOnlyList<GridRow> Rows =
    [
        new("D-2401", "Camille Durand", "Belgique", 12400),
        new("D-2402", "Jonas Meyer", "Luxembourg", 8600),
        new("D-2403", "Sofia Rossi", "France", 21500),
        new("D-2404", "Lars Jansen", "Pays-Bas", 4300),
        new("D-2405", "Ana Silva", "France", 17800),
        new("D-2406", "Piet de Vries", "Pays-Bas", 9100),
        new("D-2407", "Marie Lambert", "Belgique", 15200)
    ];

    private IReadOnlyList<GridRow> Selection { get; set; } = [];

    private OmniDataGridLines Lines { get; set; } = OmniDataGridLines.Both;

    private OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    private OmniDataGridPagerPosition Pager { get; set; } = OmniDataGridPagerPosition.Bottom;

    private OmniDataGridSelectionMode Mode { get; set; } = OmniDataGridSelectionMode.Multiple;

    private OmniDataGridFilterMode Filters { get; set; } = OmniDataGridFilterMode.Advanced;

    private OmniDataGridEditMode Edit { get; set; } = OmniDataGridEditMode.Single;

    private OmniDataGridExpandMode Expand { get; set; } = OmniDataGridExpandMode.Multiple;

    private OmniDataGridFooterPosition Footer { get; set; } = OmniDataGridFooterPosition.Bottom;
}
