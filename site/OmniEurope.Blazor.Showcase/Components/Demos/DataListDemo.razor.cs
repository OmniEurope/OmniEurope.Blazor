namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataListDemo
{
    private static readonly IReadOnlyList<OmniKanbanColumn> WorkflowColumns =
    [new("new", "À examiner"), new("review", "En cours"), new("done", "Terminé")];

    private IReadOnlyList<GridRow> KanbanRows { get; set; } = Rows.Take(3).ToArray();
    private Dictionary<string, string> WorkflowPositions { get; } = new()
    {
        ["D-2401"] = "new", ["D-2402"] = "review", ["D-2403"] = "done"
    };

    private string WorkflowColumnOf(GridRow row) => WorkflowPositions[row.Reference];

    private static object WorkflowKeyOf(GridRow row) => row.Reference;

    private static string WorkflowLabelOf(GridRow row) => row.Reference;

    private void MoveWorkflowRow(OmniKanbanMove<GridRow> move) =>
        WorkflowPositions[move.Item.Reference] = move.ToColumn;

    private static readonly IReadOnlyList<int> Sizes = [2, 4, 8];

    private static readonly IReadOnlyList<GridRow> Rows =
    [
        new("D-2401", "Camille Durand", "Belgique", 12400),
        new("D-2402", "Jonas Meyer", "Luxembourg", 8600),
        new("D-2403", "Sofia Rossi", "France", 21500),
        new("D-2404", "Lars Jansen", "Pays-Bas", 4300),
        new("D-2405", "Ana Silva", "France", 17800),
        new("D-2406", "Piet de Vries", "Pays-Bas", 9100)
    ];

    private int PageIndex { get; set; }

    private int PageSize { get; set; } = 2;

    private int PageCount => (int)Math.Ceiling(Rows.Count / (double)PageSize);

    private IReadOnlyList<GridRow> Page => [.. Rows.Skip(PageIndex * PageSize).Take(PageSize)];

    private static readonly IReadOnlyList<OmniLogLine> LogLines =
    [
        new("Recherche du dossier", OmniLogLevel.Trace),
        new("Connexion établie", OmniLogLevel.Debug),
        new("Traitement commencé", OmniLogLevel.Information),
        new("Pièce manquante", OmniLogLevel.Warning),
        new("Validation refusée", OmniLogLevel.Error),
        new("Traitement interrompu", OmniLogLevel.Critical)
    ];

    private bool DraftCreated { get; set; }

    private static bool MatchesReference(GridRow row, string text) =>
        row.Reference.Contains(text, StringComparison.OrdinalIgnoreCase);

    private void CreateDraft() => DraftCreated = true;
}
