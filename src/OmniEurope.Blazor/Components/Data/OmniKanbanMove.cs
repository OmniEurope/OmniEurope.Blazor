namespace OmniEurope.Blazor.Components;

/// <summary>A card the reader moved on an <see cref="OmniKanban{TItem}"/>, with the mouse or the keyboard.</summary>
/// <typeparam name="TItem">The type of the cards.</typeparam>
/// <param name="Item">The card moved.</param>
/// <param name="FromColumn">Key of the column it left.</param>
/// <param name="ToColumn">Key of the column it was dropped in; the same key for a move within a column.</param>
/// <param name="Index">Its place in that column once moved, from 0, counted among the other cards of the column.</param>
public sealed record OmniKanbanMove<TItem>(TItem Item, string FromColumn, string ToColumn, int Index);
