using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The row groups of a grid: the active groups, the header toggles that add or remove one, the groups
/// the reader closed and the grouped sequence of header and item rows.
/// </summary>
internal sealed class GridGrouping<TItem>(OmniDataGrid<TItem> grid)
{
    // The segment of a null value. An escaped value only has a backslash before another backslash or a
    // slash, so a backslash before a zero cannot come from one.
    private const string NullGroupSegment = @"\0";
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);

    // A tree grid is not grouped: its rows are already nested under their parents.
    internal IReadOnlyList<OmniDataGridGroup> ActiveGroups => grid.AllowGrouping && !grid.Tree.Active ? grid.Groups : Array.Empty<OmniDataGridGroup>();

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
        // Each group keeps its own direction: a group naming no column is dropped with its direction,
        // not left to shift the directions of the groups after it.
        var levels = groups
            .Select(group => (Group: group, Column: grid.ColumnSet.Find(group.Key)!))
            .Where(level => level.Column is not null)
            .ToArray();
        if (levels.Length == 0)
        {
            return grid.Rows.FlatRows(items);
        }

        var accessors = levels.Select(level => level.Column).ToArray();
        var sorted = Sort(levels, items);
        var paths = sorted.Select(item => GroupPaths(accessors, item)).ToArray();
        var counts = Counts(paths);
        var footers = grid.ColumnSet.HasGroupFooter ? GroupItems(sorted, paths) : null;
        var rows = new List<GridRenderRow<TItem>>(sorted.Length);
        var previousPaths = new string[accessors.Length];
        var index = 0;
        for (var position = 0; position < sorted.Length; position++)
        {
            var item = sorted[position];
            var (headers, hidden) = Headers(accessors, item, paths[position], previousPaths, counts);
            var closing = footers is null ? [] : ClosingGroups(accessors, sorted, paths, position, footers);
            if (!hidden)
            {
                rows.Add(grid.Rows.Describe(item, index, headers, grid.DetailTemplate is not null && grid.Expansion.IsExpanded(grid.ItemKey(item))) with { Footers = closing });
                index++;
            }
            else if (headers.Count > 0 || closing.Count > 0)
            {
                rows.Add(new GridRenderRow<TItem>(-1, default!, false, headers, false, null, false, false) { Footers = closing });
            }
        }

        return rows;
    }

    /// <summary>The items ordered by each group in turn, in its direction, the original order last.</summary>
    private static TItem[] Sort((OmniDataGridGroup Group, OmniDataGridColumnDefinition<TItem> Column)[] levels, IReadOnlyList<TItem> items)
    {
        var ordered = items
            .Select((item, index) => (Item: item, Index: index))
            .OrderBy(entry => 0);
        foreach (var (group, accessor) in levels)
        {
            ordered = group.Descending
                ? ordered.ThenByDescending(entry => accessor.Value(entry.Item), Comparer<object?>.Default)
                : ordered.ThenBy(entry => accessor.Value(entry.Item), Comparer<object?>.Default);
        }

        return ordered.ThenBy(entry => entry.Index).Select(entry => entry.Item).ToArray();
    }

    /// <summary>How many items each group holds, by path.</summary>
    private static Dictionary<string, int> Counts(string[][] paths)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var itemPaths in paths)
        {
            foreach (var path in itemPaths)
            {
                counts[path] = counts.GetValueOrDefault(path) + 1;
            }
        }

        return counts;
    }

    /// <summary>
    /// The headers that open above an item: from the first level whose group changed, every level down;
    /// none under a closed group. Hidden when a group of the item is closed.
    /// </summary>
    private (List<GridGroupHeader> Headers, bool Hidden) Headers(
        OmniDataGridColumnDefinition<TItem>[] accessors, TItem item, string[] itemPaths, string[] previousPaths, Dictionary<string, int> counts)
    {
        var headers = new List<GridGroupHeader>();
        var reopened = false;
        var hidden = false;
        for (var level = 0; level < accessors.Length; level++)
        {
            var path = itemPaths[level];
            if (reopened || !string.Equals(previousPaths[level], path, StringComparison.Ordinal))
            {
                reopened = true;
                previousPaths[level] = path;
                var value = accessors[level].Value(item);
                var count = counts[path];
                var text = grid.GroupLabel?.Invoke(value, count) ?? $"{accessors[level].Title} : {value} ({count})";
                if (!hidden)
                {
                    headers.Add(new GridGroupHeader(path, text, level, count, IsGroupExpanded(path)));
                }
            }

            hidden |= !IsGroupExpanded(path);
        }

        return (headers, hidden);
    }

    /// <summary>The path of each grouping level of an item, outermost first.</summary>
    private static string[] GroupPaths(OmniDataGridColumnDefinition<TItem>[] accessors, TItem item)
    {
        var paths = new string[accessors.Length];
        var path = string.Empty;
        for (var level = 0; level < accessors.Length; level++)
        {
            path = $"{path}/{Segment(accessors[level].Value(item))}";
            paths[level] = path;
        }

        return paths;
    }

    /// <summary>
    /// One level of a group path. The value is escaped so a slash inside it cannot pass for the separator:
    /// the groups (a/b, c) and (a, b/c) used to share one path, their count and their open state.
    /// </summary>
    private static string Segment(object? value) => value is null
        ? NullGroupSegment
        : $"{value}".Replace(@"\", @"\\", StringComparison.Ordinal).Replace("/", @"\/", StringComparison.Ordinal);

    /// <summary>The rows of every group, by path, in display order.</summary>
    private static Dictionary<string, List<TItem>> GroupItems(TItem[] sorted, string[][] paths)
    {
        var groups = new Dictionary<string, List<TItem>>(StringComparer.Ordinal);
        for (var position = 0; position < sorted.Length; position++)
        {
            foreach (var path in paths[position])
            {
                if (!groups.TryGetValue(path, out var items))
                {
                    groups[path] = items = [];
                }

                items.Add(sorted[position]);
            }
        }

        return groups;
    }

    /// <summary>
    /// The groups the item at <paramref name="position"/> is the last row of, innermost first, each as the
    /// context of its footer row. A group inside a closed group has no footer: its rows are not shown.
    /// </summary>
    private List<OmniDataGridGroupContext<TItem>> ClosingGroups(
        OmniDataGridColumnDefinition<TItem>[] accessors, TItem[] sorted, string[][] paths, int position, Dictionary<string, List<TItem>> groups)
    {
        var closing = new List<OmniDataGridGroupContext<TItem>>();
        for (var level = accessors.Length - 1; level >= 0; level--)
        {
            var path = paths[position][level];
            var last = position + 1 == sorted.Length || !string.Equals(paths[position + 1][level], path, StringComparison.Ordinal);
            var visible = true;
            for (var ancestor = 0; ancestor < level; ancestor++)
            {
                visible &= IsGroupExpanded(paths[position][ancestor]);
            }

            if (last && visible)
            {
                closing.Add(new OmniDataGridGroupContext<TItem>(path, accessors[level].Value(sorted[position]), accessors[level].Key, level, groups[path]));
            }
        }

        return closing;
    }
}
