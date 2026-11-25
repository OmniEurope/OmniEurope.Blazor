namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the context of a tree, and the items that share it, need from one item: the value it stands for, and
/// opening or closing it from the tree. The context holds its items through this contract, so it never names
/// the item component, which reads the context.
/// </summary>
/// <typeparam name="TValue">The type of the values the items stand for.</typeparam>
internal interface ITreeNode<TValue>
{
    /// <summary>The value the item stands for in the tree's selection.</summary>
    TValue Value { get; }

    /// <summary>Opens or closes the item from its tree (expand or collapse all), loading its children when it opens.</summary>
    Task SetExpandedAsync(bool expanded);
}
