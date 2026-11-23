using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The column order and visibility of a grid as its other collaborators use them, from <see cref="GridColumnArrangement{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridColumnArrangement<TItem>
{
    IReadOnlyList<string>? Order { get; }

    IReadOnlyDictionary<string, bool> Visibility { get; }

    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Arrange(IReadOnlyList<OmniDataGridColumnDefinition<TItem>> declared);

    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Visible(IReadOnlyList<OmniDataGridColumnDefinition<TItem>> arranged);

    void Restore(IEnumerable<string>? order, IEnumerable<KeyValuePair<string, bool>>? visibility);

    void Reset();

    Task<bool> MoveNextToAsync(string key, string target, bool after);

    Task<bool> SetVisibleAsync(string key, bool visible);
}
