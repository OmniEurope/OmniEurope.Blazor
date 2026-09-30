using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The sorts and column filters of a grid: the applied filters, the drafts an advanced editor holds
/// until applied, the declared defaults taken once per column, and the sorts and filters a loader
/// receives.
/// </summary>
internal sealed class GridQueryState<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly Dictionary<string, GridColumnFilter> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GridColumnFilter> _draftFilters = new(StringComparer.Ordinal);
    private readonly List<OmniDataGridSort> _sorts = [];
    private readonly HashSet<string> _initialSortKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _defaultFilterKeys = new(StringComparer.Ordinal);

    /// <summary>The applied filters, by column key.</summary>
    internal IReadOnlyDictionary<string, GridColumnFilter> Filters => _filters;

    /// <summary>The applied sorts, outermost first.</summary>
    internal IReadOnlyList<OmniDataGridSort> Sorts => _sorts;

    /// <summary>Whether any applied filter filters something.</summary>
    internal bool HasActiveFilter() => _filters.Values.Any(filter => filter.IsActive);

    /// <summary>
    /// Applies a column's declared <c>SortOrder</c> the first time that column registers. Columns
    /// register while the child content renders, after the grid's own parameters are set, so this
    /// runs per column rather than once for the grid. Returns whether it added a sort.
    /// </summary>
    internal bool ApplyInitialSort(OmniDataGridColumnDefinition<TItem> column)
    {
        if (column.SortOrder is null || !_initialSortKeys.Add(column.Key))
        {
            return false;
        }

        // A sort already present for this key (set from code, or restored from the persisted state
        // before the column registered) takes precedence over the column's own default.
        if (_sorts.Any(sort => sort.Key == column.Key))
        {
            return false;
        }

        _sorts.Add(new OmniDataGridSort(column.Key, column.SortOrder == OmniDataGridSortOrder.Descending)
        {
            Property = column.SortProperty ?? column.Property
        });
        grid.View.InvalidateLocalProjection();
        return true;
    }

    /// <summary>
    /// Applies a column's declared <c>DefaultFilterValue</c> the first time that column registers,
    /// the way <see cref="ApplyInitialSort"/> does for its sort. A filter already held for the key
    /// (restored state, or set from code before the column rendered) wins. Returns whether it added
    /// a filter.
    /// </summary>
    internal bool ApplyDefaultFilter(OmniDataGridColumnDefinition<TItem> column)
    {
        if (string.IsNullOrEmpty(column.DefaultFilterValue) || !_defaultFilterKeys.Add(column.Key)
            || _filters.ContainsKey(column.Key))
        {
            return false;
        }

        _filters[column.Key] = GridFilterOperators<TItem>.DefaultFilter(column) with { Value = column.DefaultFilterValue };
        return true;
    }

    /// <summary>
    /// Sets column filters from code exactly as the user would in the column headers. A null or empty
    /// value clears that column; with <paramref name="replace"/> every other filter is cleared too.
    /// </summary>
    internal void SetFilters(IReadOnlyDictionary<string, string?> values, bool replace)
    {
        if (replace)
        {
            _filters.Clear();
            _draftFilters.Clear();
        }

        foreach (var (key, value) in values)
        {
            _draftFilters.Remove(key);
            var column = grid.ColumnSet.Declared.FirstOrDefault(candidate => candidate.Key == key);
            if (string.IsNullOrEmpty(value))
            {
                _filters.Remove(key);
            }
            else
            {
                _filters[key] = column is null
                    ? GridColumnFilter.Empty with { Operator = OmniDataGridFilterOperator.Equals, Value = value }
                    : GridFilterOperators<TItem>.DefaultFilter(column) with { Value = value };
            }

            // Set from code, a key must not be overwritten by its column's default when it renders.
            _defaultFilterKeys.Add(key);
        }
    }

    /// <summary>Replaces the filters and sorts with the ones restored from the persisted state.</summary>
    internal void Restore(IEnumerable<KeyValuePair<string, GridColumnFilter>> filters, IEnumerable<OmniDataGridSort> sorts)
    {
        _filters.Clear();
        foreach (var (columnKey, filter) in filters)
        {
            _filters[columnKey] = filter;
        }

        _sorts.Clear();
        _sorts.AddRange(sorts);
    }

    /// <summary>Drops the filter, draft and sort of a column that left the grid.</summary>
    internal void Forget(string key)
    {
        _filters.Remove(key);
        _draftFilters.Remove(key);
        _sorts.RemoveAll(sort => sort.Key == key);
    }

    /// <summary>
    /// Cycles a column through the three sort states on successive clicks: ascending, descending,
    /// then unsorted. The third click removes the column from the sorts rather than looping back to
    /// ascending, so the grid can be returned to its natural order.
    /// </summary>
    internal async Task SortAsync(string key, bool append)
    {
        var column = grid.ColumnSet.Find(key);
        // The handler now sits on the whole header cell, so a click on a column that does not sort
        // has to be turned away here rather than by not wiring the handler at all.
        if (column is null || !IsSortable(column))
        {
            return;
        }

        var existing = _sorts.FindIndex(sort => sort.Key == key);
        var wasDescending = existing >= 0 && _sorts[existing].Descending;
        var clear = existing >= 0 && wasDescending;
        if (!append) _sorts.Clear();
        else if (existing >= 0) _sorts.RemoveAt(existing);
        if (!clear)
        {
            _sorts.Add(new OmniDataGridSort(key, existing >= 0) { Property = column?.SortProperty ?? column?.Property });
        }

        await grid.View.QueryChangedAsync();
    }

    internal bool IsSortable(OmniDataGridColumnDefinition<TItem> column) => grid.AllowSorting && column.Sortable;

    internal string? AriaSort(OmniDataGridColumnDefinition<TItem> column)
    {
        var sort = _sorts.FirstOrDefault(candidate => candidate.Key == column.Key);
        return sort is null ? null : sort.Descending ? "descending" : "ascending";
    }

    /// <summary>
    /// Visual sort indicator next to a sortable header's title, mirroring aria-sort. Null on an
    /// unsorted column so no icon is rendered at all.
    /// </summary>
    internal OmniIconName? SortIcon(OmniDataGridColumnDefinition<TItem> column) => AriaSort(column) switch
    {
        "ascending" => OmniIconName.SortAscending,
        "descending" => OmniIconName.SortDescending,
        _ => null
    };

    /// <summary>
    /// Whether the column is filtered right now. Read from the applied filter, not the draft: an
    /// advanced condition typed but not yet applied filters nothing, so it offers nothing to clear.
    /// </summary>
    internal bool HasActiveFilter(OmniDataGridColumnDefinition<TItem> column) =>
        _filters.TryGetValue(column.Key, out var filter) && filter.IsActive;

    /// <summary>A column is active while it carries the sort or a filter value.</summary>
    internal bool IsColumnActive(OmniDataGridColumnDefinition<TItem> column) =>
        _sorts.Any(sort => sort.Key == column.Key)
        || (_filters.TryGetValue(column.Key, out var filter) && filter.IsActive);

    /// <summary>The applied filter of a column, or its empty default.</summary>
    internal GridColumnFilter FilterOf(OmniDataGridColumnDefinition<TItem> column) =>
        _filters.GetValueOrDefault(column.Key, GridFilterOperators<TItem>.DefaultFilter(column));

    /// <summary>The filter being edited for a column: its draft, or its applied filter.</summary>
    internal GridColumnFilter DraftOf(OmniDataGridColumnDefinition<TItem> column) =>
        _draftFilters.GetValueOrDefault(column.Key, FilterOf(column));

    /// <summary>Holds a pending change of a column's filter.</summary>
    internal void Stage(OmniDataGridColumnDefinition<TItem> column, GridColumnFilter filter) => _draftFilters[column.Key] = filter;

    /// <summary>Applies the draft of a column: an active one filters, an empty one clears the column.</summary>
    internal void Commit(OmniDataGridColumnDefinition<TItem> column)
    {
        var filter = DraftOf(column);
        if (filter.IsActive)
        {
            _filters[column.Key] = filter;
        }
        else
        {
            _filters.Remove(column.Key);
        }
    }

    /// <summary>Clears the applied filter and the draft of a column.</summary>
    internal void Clear(OmniDataGridColumnDefinition<TItem> column)
    {
        _filters.Remove(column.Key);
        _draftFilters.Remove(column.Key);
    }

    /// <summary>The sorts a loader receives: those of the columns still declared.</summary>
    internal IReadOnlyList<OmniDataGridSort> CurrentSorts()
    {
        var keys = grid.ColumnSet.EffectiveColumns.Select(column => column.Key).ToHashSet(StringComparer.Ordinal);
        return _sorts.Where(sort => keys.Contains(sort.Key)).ToArray();
    }

    /// <summary>The active filters a loader receives, a date range already resolved to its bounds.</summary>
    internal IReadOnlyList<OmniDataGridFilter> CurrentFilters()
    {
        var columns = grid.ColumnSet.EffectiveColumns;
        var keys = columns.Select(column => column.Key).ToHashSet(StringComparer.Ordinal);
        var dateRanges = columns
            .Where(column => column.FilterType == OmniDataGridColumnFilterType.DateRange && column.FilterPredicate is null)
            .Select(column => column.Key)
            .ToHashSet(StringComparer.Ordinal);
        return _filters
            .Where(pair => keys.Contains(pair.Key) && pair.Value.IsActive)
            .Select(pair => dateRanges.Contains(pair.Key)
                ? DateRangeFilter(pair.Key, pair.Value.Value)
                : new OmniDataGridFilter(
                    pair.Key,
                    pair.Value.Operator,
                    pair.Value.Value,
                    pair.Value.LogicalOperator,
                    pair.Value.HasSecond ? pair.Value.SecondOperator : null,
                    pair.Value.HasSecond ? pair.Value.SecondValue : null))
            .OfType<OmniDataGridFilter>()
            .ToArray();
    }

    /// <summary>
    /// A date range reaches a loader already resolved, so no loader has to know the whole-day rule:
    /// an inclusive lower bound (GreaterThanOrEquals) and an exclusive upper one (LessThan), in
    /// invariant ISO form. A range with neither side readable sends nothing.
    /// </summary>
    private static OmniDataGridFilter? DateRangeFilter(string key, string value)
    {
        var (start, endExclusive) = OmniDataGridDateRange.Resolve(value);
        return (start, endExclusive) switch
        {
            ({ } from, { } to) => new OmniDataGridFilter(
                key,
                OmniDataGridFilterOperator.GreaterThanOrEquals,
                OmniDataGridDateRange.FormatBound(from),
                OmniDataGridLogicalOperator.And,
                OmniDataGridFilterOperator.LessThan,
                OmniDataGridDateRange.FormatBound(to)),
            ({ } from, null) => new OmniDataGridFilter(key, OmniDataGridFilterOperator.GreaterThanOrEquals, OmniDataGridDateRange.FormatBound(from)),
            (null, { } to) => new OmniDataGridFilter(key, OmniDataGridFilterOperator.LessThan, OmniDataGridDateRange.FormatBound(to)),
            _ => null
        };
    }
}
