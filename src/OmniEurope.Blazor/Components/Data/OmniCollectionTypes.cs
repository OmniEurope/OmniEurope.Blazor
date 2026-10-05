namespace OmniEurope.Blazor.Components;

internal sealed class OmniTreeContext<TValue>
{
    public IReadOnlyList<TValue> SelectedValues { get; set; } = Array.Empty<TValue>();
    public required Func<TValue, Task> ToggleSelectionAsync { get; init; }

    /// <summary>The tree that created the context, for its drag-and-drop settings. An item outside a tree has no context.</summary>
    public required OmniTree<TValue> Tree { get; init; }

    /// <summary>The items rendered in the tree, for expanding or collapsing them all.</summary>
    public HashSet<OmniTreeItem<TValue>> Items { get; } = [];

    /// <summary>Set by expanding the whole tree: an item rendered afterwards, under a branch just opened, starts open too.</summary>
    public bool ExpandAll { get; set; }

    /// <summary>The item being dragged, null outside a drag.</summary>
    public OmniTreeItem<TValue>? Dragged { get; set; }

    /// <summary>The valid drop target under the pointer, null when none is.</summary>
    public OmniTreeItem<TValue>? DropTarget { get; set; }

    /// <summary>
    /// Redraws the tree, and with it every item, which reads this context: a drag started or ended, or the
    /// target changed, which changes where a drop is accepted and highlighted.
    /// </summary>
    public void Redraw() => Tree.Redraw();
}
