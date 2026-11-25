namespace OmniEurope.Blazor.Components;

/// <summary>The new-row highlight as the grid's other collaborators use it, from <see cref="GridNewRowHighlight{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridNewRowHighlight<TItem>
{
    HashSet<object>? HeldKeys(IEnumerable<TItem> items);

    void HoldBaseline(IEnumerable<TItem> items);

    void CompareWithBaseline(IEnumerable<TItem> items);

    void MarkNewRows(HashSet<object>? before, IEnumerable<TItem> now);

    bool IsNewRow(TItem item);
}
