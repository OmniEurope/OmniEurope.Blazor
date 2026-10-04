namespace OmniEurope.Blazor.Components;

/// <summary>
/// An item of an <see cref="OmniTree{TValue}"/> dropped onto another (<see cref="OmniTree{TValue}.OnItemDropped"/>):
/// the host makes <see cref="Target"/> the new parent of <see cref="Dragged"/> in its data, then renders the tree again.
/// </summary>
/// <typeparam name="TValue">The type of the values the items stand for.</typeparam>
/// <param name="Dragged">The value of the item that was dragged.</param>
/// <param name="Target">The value of the item it was dropped onto.</param>
public sealed record OmniTreeDropEventArgs<TValue>(TValue Dragged, TValue Target);
