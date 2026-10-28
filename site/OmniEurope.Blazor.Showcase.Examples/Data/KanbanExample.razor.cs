using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A two-lane board of three cards that move from one lane to the other.</summary>
public partial class KanbanExample
{
    private IReadOnlyList<Record> Records { get; } =
    [
        new("one", "First record"),
        new("two", "Second record"),
        new("three", "Third record")
    ];

    private IReadOnlyList<OmniKanbanColumn> Columns { get; } =
    [
        new("new", "New"),
        new("done", "Done")
    ];

    // Where each card sits; a move reported by the board rewrites its entry.
    private Dictionary<string, string> Positions { get; } = new(StringComparer.Ordinal)
    {
        ["one"] = "new",
        ["two"] = "new",
        ["three"] = "done"
    };

    private static object KeyOf(Record record) => record.Id;

    private static string LabelOf(Record record) => record.Name;

    private string ColumnOf(Record record) => Positions[record.Id];

    private void Move(OmniKanbanMove<Record> move) => Positions[move.Item.Id] = move.ToColumn;

    private sealed record Record(string Id, string Name);
}
