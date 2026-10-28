using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Documents dragged between folders in a tree, with a data grid that shows the same folders as a tree.</summary>
public partial class TreeMoveExample
{
    private OmniTree<string>? Tree { get; set; }

    // The folders and the documents filed in them: the tree and the grid both read this one dictionary.
    private Dictionary<string, List<string>> Folders { get; } = new(StringComparer.Ordinal)
    {
        ["Documents"] = ["Invoices", "Contracts"],
        ["Archives"] = ["Certificates"]
    };

    private IReadOnlyList<Node> Rows => [.. Folders.Select(folder => new Node(
        folder.Key,
        folder.Key,
        folder.Value.Count,
        [.. folder.Value.Select(item => new Node(item, item, 1, []))]))];

    private bool CanDrag(string key) => !Folders.ContainsKey(key);

    private bool CanDrop(string dragged, string target) => CanDrag(dragged) && Folders.ContainsKey(target);

    private void Move(OmniTreeDropEventArgs<string> drop)
    {
        if (!CanDrop(drop.Dragged, drop.Target))
        {
            return;
        }

        foreach (var documents in Folders.Values)
        {
            documents.Remove(drop.Dragged);
        }

        Folders[drop.Target].Add(drop.Dragged);
    }

    private Task ExpandAsync() => Tree?.ExpandAllAsync() ?? Task.CompletedTask;

    private Task CollapseAsync() => Tree?.CollapseAllAsync() ?? Task.CompletedTask;

    private static IReadOnlyList<Node>? ChildrenOf(Node row) => row.Children;

    private static object KeyOf(Node row) => row.Key;

    private static bool InitiallyExpanded(Node row) => true;

    private sealed record Node(string Key, string Name, int Count, IReadOnlyList<Node> Children);
}
