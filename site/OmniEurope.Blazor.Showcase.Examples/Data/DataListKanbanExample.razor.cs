using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Three cases on a three-column board; a move is recorded by the example.</summary>
public partial class DataListKanbanExample
{
    private IReadOnlyList<OmniKanbanColumn> WorkflowColumns { get; } =
    [
        new("new", "To review"),
        new("review", "In progress"),
        new("done", "Done")
    ];

    private IReadOnlyList<CaseRow> KanbanRows { get; } = [.. CaseData.Rows.Take(3)];

    private Dictionary<string, string> WorkflowPositions { get; } = new()
    {
        ["D-2401"] = "new", ["D-2402"] = "review", ["D-2403"] = "done"
    };

    private string WorkflowColumnOf(CaseRow row) => WorkflowPositions[row.Reference];

    private static object WorkflowKeyOf(CaseRow row) => row.Reference;

    private static string WorkflowLabelOf(CaseRow row) => row.Reference;

    private void MoveWorkflowRow(OmniKanbanMove<CaseRow> move) =>
        WorkflowPositions[move.Item.Reference] = move.ToColumn;
}
