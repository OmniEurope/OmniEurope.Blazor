namespace OmniEurope.Blazor.Internal;

/// <summary>The tree rows of a grid as its other collaborators read them, from <see cref="GridTree{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridTree<TItem>
{
    bool Active { get; }

    IReadOnlyList<GridTreeNode<TItem>> Nodes { get; }

    IReadOnlyList<TItem> AllItems { get; }

    IReadOnlyList<TItem> Roots { get; }

    void Invalidate();

    Task ToggleAsync(TItem item);
}
