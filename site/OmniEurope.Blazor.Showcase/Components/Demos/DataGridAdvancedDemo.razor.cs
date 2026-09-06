namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataGridAdvancedDemo
{
    private static readonly IReadOnlyList<string> CountryValues = ["Belgique", "France", "Luxembourg", "Pays-Bas"];

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
}
