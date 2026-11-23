using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The sorts and filters of a grid as its other collaborators use them, from <see cref="GridQueryState{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridQueryState<TItem>
{
    IReadOnlyDictionary<string, GridColumnFilter> Filters { get; }

    IReadOnlyList<OmniDataGridSort> Sorts { get; }

    bool HasActiveFilter();

    bool HasActiveFilter(OmniDataGridColumnDefinition<TItem> column);

    bool ApplyInitialSort(OmniDataGridColumnDefinition<TItem> column);

    bool ApplyDefaultFilter(OmniDataGridColumnDefinition<TItem> column);

    void Restore(IEnumerable<KeyValuePair<string, GridColumnFilter>> filters, IEnumerable<OmniDataGridSort> sorts);

    void ResetToDeclared();

    void Forget(string key);

    bool IsSortable(OmniDataGridColumnDefinition<TItem> column);

    OmniIconName? SortIcon(OmniDataGridColumnDefinition<TItem> column);

    bool IsColumnActive(OmniDataGridColumnDefinition<TItem> column);

    GridColumnFilter DraftOf(OmniDataGridColumnDefinition<TItem> column);

    void Stage(OmniDataGridColumnDefinition<TItem> column, GridColumnFilter filter);

    void Commit(OmniDataGridColumnDefinition<TItem> column);

    void Clear(OmniDataGridColumnDefinition<TItem> column);

    IReadOnlyList<OmniDataGridSort> CurrentSorts();

    IReadOnlyList<OmniDataGridFilter> CurrentFilters();
}
