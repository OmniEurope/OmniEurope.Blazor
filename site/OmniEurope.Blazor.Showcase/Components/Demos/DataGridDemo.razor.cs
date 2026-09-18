namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataGridDemo
{
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

    private static readonly IReadOnlyList<RunRow> Runs =
    [
        new("#2400", "atlas-candidate", 192, "Échec"),
        new("#2399", "aetheus-deploy-prod", 108, "Succès"),
        new("#2398", "portfolio-build", 52, "Succès"),
        new("#2397", "atlas-candidate", 164, "En cours"),
        new("#2396", "aetheus-candidate", 121, "Succès"),
        new("#2395", "aetheus-deploy-prod", 309, "En attente"),
        new("#2394", "aetheus-candidate", 125, "Succès"),
        new("#2393", "portfolio-build", 47, "Annulé")
    ];

    private static readonly IReadOnlyList<GridRow> FewRows = [.. Rows.Take(3)];

    private IReadOnlyList<object> SelectedRuns { get; set; } = ["#2395"];

    /// <summary>The compact status of a list: a dot in the fill colour of the intention, the page text.</summary>
    private static string StatusClass(string status) => status switch
    {
        "Succès" => "omni-status omni-status--success",
        "Échec" => "omni-status omni-status--danger",
        "En cours" => "omni-status omni-status--info",
        "En attente" or "Annulé" => "omni-status omni-status--warning",
        _ => "omni-status"
    };

    private static readonly IReadOnlyList<GridRow> ManyRows = [.. Enumerable.Range(1, 10_000)
        .Select(index => Rows[index % Rows.Count] with { Reference = $"D-{index:00000}" })];
}
