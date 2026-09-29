using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The row groups of a grid: the active groups, the header toggles that add or remove one, the groups
/// the reader closed and the grouped sequence of header and item rows.
/// </summary>
internal sealed class GridGrouping<TItem>(OmniDataGrid<TItem> grid)
{
    private static readonly object NullGroupKey = new();
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);

    internal IReadOnlyList<OmniDataGridGroup> ActiveGroups => grid.AllowGrouping ? grid.Groups : Array.Empty<OmniDataGridGroup>();

    internal bool IsGroupable(OmniDataGridColumnDefinition<TItem> column) => grid.AllowGrouping && column.Groupable;

    internal bool IsGrouped(string key) => ActiveGroups.Any(group => group.Key == key);

    internal async Task ToggleGroupingAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        var groups = ActiveGroups.ToList();
        var index = groups.FindIndex(group => group.Key == column.Key);
        if (index >= 0)
        {
            groups.RemoveAt(index);
        }
        else
        {
            groups.Add(new OmniDataGridGroup(column.Key));
        }

        await grid.GroupsChanged.InvokeAsync(groups);
    }

    /// <summary>Removes a group from the group panel.</summary>
    internal Task UngroupAsync(OmniDataGridGroup group) =>
        ToggleGroupingAsync(grid.ColumnSet.EffectiveColumns.First(column => column.Key == group.Key));

    internal string GroupTitle(OmniDataGridGroup group) =>
        grid.ColumnSet.Find(group.Key)?.Title ?? group.Key;

    private bool IsGroupExpanded(string path) => grid.AllGroupsExpanded
        ? !_collapsedGroups.Contains(path)
        : _collapsedGroups.Contains(path);

    internal void ToggleGroup(string path)
    {
        if (!_collapsedGroups.Remove(path))
        {
            _collapsedGroups.Add(path);
        }

        grid.Virtual.RefreshSlots();
    }

    /// <summary>The rows of <paramref name="items"/> grouped by <paramref name="groups"/>, each group opened by its header row.</summary>
    internal IReadOnlyList<GridRenderRow<TItem>> GroupedRows(IReadOnlyList<OmniDataGridGroup> groups, IReadOnlyList<TItem> items)
    {
        var accessors = groups
            .Select(group => grid.ColumnSet.Find(group.Key))
            .Where(column => column is not null)
            .Select(column => column!)
            .ToArray();
        if (accessors.Length == 0)
        {
            return grid.Rows.FlatRows(items);
        }

        var ordered = items
            .Select((item, index) => (Item: item, Index: index))
            .OrderBy(entry => 0);
        for (var level = 0; level < accessors.Length; level++)
        {
            var accessor = accessors[level];
            ordered = groups[level].Descending
                ? ordered.ThenByDescending(entry => accessor.Value(entry.Item), Comparer<object?>.Default)
                : ordered.ThenBy(entry => accessor.Value(entry.Item), Comparer<object?>.Default);
        }

        var sorted = ordered.ThenBy(entry => entry.Index).Select(entry => entry.Item).ToArray();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in sorted)
        {
            var path = string.Empty;
            for (var level = 0; level < accessors.Length; level++)
            {
                path = $"{path}/{accessors[level].Value(item) ?? NullGroupKey}";
                counts[path] = counts.GetValueOrDefault(path) + 1;
            }
        }

        var rows = new List<GridRenderRow<TItem>>(sorted.Length);
        var previousPaths = new string[accessors.Length];
        var index = 0;
        foreach (var item in sorted)
        {
            var headers = new List<GridGroupHeader>();
            var path = string.Empty;
            var reopened = false;
            var hidden = false;
            for (var level = 0; level < accessors.Length; level++)
            {
                var accessor = accessors[level];
                var value = accessor.Value(item);
                path = $"{path}/{value ?? NullGroupKey}";
                if (reopened || !string.Equals(previousPaths[level], path, StringComparison.Ordinal))
                {
                    reopened = true;
                    previousPaths[level] = path;
                    var count = counts.GetValueOrDefault(path);
                    var expanded = IsGroupExpanded(path);
                    var text = grid.GroupLabel?.Invoke(value, count) ?? $"{accessor.Title} : {value} ({count})";
                    if (!hidden)
                    {
                        headers.Add(new GridGroupHeader(path, text, level, count, expanded));
                    }
                }

                hidden |= !IsGroupExpanded(path);
            }

            if (!hidden)
            {
                rows.Add(grid.Rows.Describe(item, index, headers, grid.DetailTemplate is not null && grid.Expansion.IsExpanded(grid.ItemKey(item))));
                index++;
            }
            else if (headers.Count > 0)
            {
                rows.Add(new GridRenderRow<TItem>(-1, default!, false, headers, false, null, false, false));
            }
        }

        return rows;
    }
}
