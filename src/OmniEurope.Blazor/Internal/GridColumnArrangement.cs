using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The order and the visibility of a grid's columns as the reader arranged them, over the order and the
/// <c>Visible</c> the columns declare. Columns are named by their key (<see cref="OmniDataGridColumn{TItem}.Key"/>),
/// the same identity as their sort, filter and width in the saved state.
/// </summary>
/// <remarks>
/// <para>
/// No arrangement keeps the declared order. A saved order is applied to the columns that still exist; a
/// column it does not name takes its declared place, right after the column declared before it, and a
/// name it holds whose column is gone is ignored.
/// </para>
/// <para>
/// Frozen columns stay in their frozen places: the declared sequence keeps which positions are frozen,
/// the order only permutes the frozen columns among those positions and the other columns among the
/// rest. A frozen column is pinned to the inline start by the width of the frozen columns before it, so
/// mixing the two groups would slide rows under a column that is not pinned.
/// </para>
/// </remarks>
internal sealed class GridColumnArrangement<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly Dictionary<string, bool> _visibility = new(StringComparer.Ordinal);
    private List<string>? _order;

    /// <summary>The order the reader chose, by column key, or null for the declared order.</summary>
    internal IReadOnlyList<string>? Order => _order;

    /// <summary>The visibility the reader chose, by column key; a column it does not name keeps its declared one.</summary>
    internal IReadOnlyDictionary<string, bool> Visibility => _visibility;

    /// <summary>The columns in their arranged order, hidden ones included.</summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Arrange(IReadOnlyList<OmniDataGridColumnDefinition<TItem>> declared)
    {
        if (_order is not { Count: > 0 })
        {
            return declared;
        }

        var byKey = new Dictionary<string, OmniDataGridColumnDefinition<TItem>>(StringComparer.Ordinal);
        foreach (var column in declared)
        {
            byKey[column.Key] = column;
        }

        var placed = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<OmniDataGridColumnDefinition<TItem>>(declared.Count);
        foreach (var key in _order)
        {
            if (byKey.TryGetValue(key, out var column) && placed.Add(key))
            {
                ordered.Add(column);
            }
        }

        for (var index = 0; index < declared.Count; index++)
        {
            var column = declared[index];
            if (placed.Add(column.Key))
            {
                // A column the saved order does not know: right after the one declared before it.
                var after = index == 0 ? -1 : ordered.IndexOf(declared[index - 1]);
                ordered.Insert(after + 1, column);
            }
        }

        var frozen = new Queue<OmniDataGridColumnDefinition<TItem>>(ordered.Where(column => column.Frozen));
        var free = new Queue<OmniDataGridColumnDefinition<TItem>>(ordered.Where(column => !column.Frozen));
        return declared.Select(column => column.Frozen ? frozen.Dequeue() : free.Dequeue()).ToArray();
    }

    /// <summary>
    /// Whether a column shows: the reader's choice, else its declared <c>Visible</c>. The reader's choice only
    /// counts for a column the reader may hide (<c>Hideable</c>) on a grid that lets them (<c>AllowColumnHide</c>):
    /// a saved state (the host's store, another tab, an older layout) never hides a column the reader could not
    /// bring back from the chooser. The choice stays kept, and applies again once the host allows it.
    /// </summary>
    internal bool IsVisible(OmniDataGridColumnDefinition<TItem> column) =>
        (ReaderMayHide(column) && _visibility.TryGetValue(column.Key, out var visible)) ? visible : column.Visible;

    /// <summary>Whether the reader's choice decides the visibility of a column (see <see cref="IsVisible"/>).</summary>
    internal bool ReaderMayHide(OmniDataGridColumnDefinition<TItem> column) => grid.AllowColumnHide && column.Hideable;

    /// <summary>
    /// The arranged columns that show. A saved state that hides every column the grid declares visible
    /// (another tab, an older layout) keeps the first of them, as the chooser always keeps one: a grid
    /// without any column has nothing left to show its rows nor to bring a column back.
    /// </summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Visible(IReadOnlyList<OmniDataGridColumnDefinition<TItem>> arranged)
    {
        var visible = arranged.Where(IsVisible).ToArray();
        if (visible.Length > 0)
        {
            return visible;
        }

        var kept = arranged.FirstOrDefault(column => column.Visible);
        return kept is null ? visible : [kept];
    }

    /// <summary>Replaces the arrangement with the one restored from the saved state.</summary>
    internal void Restore(IEnumerable<string>? order, IEnumerable<KeyValuePair<string, bool>>? visibility)
    {
        var keys = order?.Where(key => !string.IsNullOrEmpty(key)).ToList();
        _order = keys is { Count: > 0 } ? keys : null;
        _visibility.Clear();
        foreach (var (key, visible) in visibility ?? [])
        {
            _visibility[key] = visible;
        }
    }

    /// <summary>Back to the declared order and visibility.</summary>
    internal void Reset() => Restore(null, null);

    /// <summary>
    /// Moves a column to a position of the arranged order (hidden columns included), then saves the state.
    /// A frozen column only moves among the frozen ones and another column among the others (see the
    /// remarks), so a move across that boundary may change nothing.
    /// </summary>
    /// <param name="key">The key of the column to move.</param>
    /// <param name="index">The position it takes in the arranged order, brought within its bounds.</param>
    /// <returns>Whether the arranged order changed.</returns>
    internal async Task<bool> MoveAsync(string key, int index)
    {
        var arranged = Arrange(grid.ColumnSet.EffectiveColumns);
        var column = arranged.FirstOrDefault(candidate => candidate.Key == key);
        if (column is null)
        {
            return false;
        }

        var keys = arranged.Select(candidate => candidate.Key).ToList();
        keys.Remove(key);
        keys.Insert(Math.Clamp(index, 0, keys.Count), key);
        var before = _order;
        _order = keys;
        // What is kept is the order actually shown, so the saved state never says what the grid does not do.
        var after = Arrange(grid.ColumnSet.EffectiveColumns).Select(candidate => candidate.Key).ToList();
        if (after.SequenceEqual(arranged.Select(candidate => candidate.Key), StringComparer.Ordinal))
        {
            _order = before;
            return false;
        }

        _order = after;
        await ChangedAsync();
        return true;
    }

    /// <summary>
    /// Moves a column right before another one, or right after it, then saves the state (see
    /// <see cref="MoveAsync"/>).
    /// </summary>
    /// <param name="key">The key of the column to move.</param>
    /// <param name="target">The key of the column it lands next to.</param>
    /// <param name="after">True to land after that column, false before it.</param>
    /// <returns>Whether the arranged order changed.</returns>
    internal Task<bool> MoveNextToAsync(string key, string target, bool after)
    {
        var keys = Arrange(grid.ColumnSet.EffectiveColumns).Select(candidate => candidate.Key).Where(candidate => candidate != key).ToList();
        var index = keys.IndexOf(target);
        return index < 0 ? Task.FromResult(false) : MoveAsync(key, index + (after ? 1 : 0));
    }

    /// <summary>
    /// Shows or hides a column, then saves the state. The last visible column cannot be hidden: a grid
    /// without any column has nothing left to show its rows nor to bring a column back.
    /// </summary>
    /// <param name="key">The key of the column.</param>
    /// <param name="visible">Whether it shows.</param>
    /// <returns>Whether the visibility changed.</returns>
    internal async Task<bool> SetVisibleAsync(string key, bool visible)
    {
        var column = grid.ColumnSet.Find(key);
        if (column is null || !ReaderMayHide(column) || IsVisible(column) == visible
            || (!visible && grid.ColumnSet.VisibleColumns.Count <= 1))
        {
            return false;
        }

        _visibility[key] = visible;
        await ChangedAsync();
        return true;
    }

    private async Task ChangedAsync()
    {
        grid.LayoutInterop.InvalidateColumnLayout();
        grid.RebuildRenderSnapshot();
        grid.Render();
        await grid.Persistence.SaveAsync();
    }
}
