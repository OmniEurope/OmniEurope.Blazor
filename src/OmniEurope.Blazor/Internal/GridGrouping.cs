using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The row groups of a grid: the active groups, the header toggles that add or remove one, the groups
/// the reader closed and the grouped sequence of header and item rows.
/// </summary>
internal sealed class GridGrouping<TItem>(IGridHost<TItem> grid) : IGridGrouping<TItem>
{
    // The segment of a null value. An escaped value only has a backslash before another backslash or a
    // slash, so a backslash before a zero cannot come from one.
    private const string NullGroupSegment = @"\0";
    // The groups whose state differs from the default: the closed ones while groups start open, the open
    // ones while they start closed. The default is AllGroupsExpanded until the reader or the host opens or
    // closes every group at once, which replaces it and forgets the exceptions.
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);
    private bool? _allExpanded;
    private bool _observedDefault = true;
    // Mirrored from the Groups parameter when the host passes a new value, like the page size: the grid keeps
    // its own groups, so a header toggle or a restored state applies whether or not the host binds them.
    private IReadOnlyList<OmniDataGridGroup> _groups = Array.Empty<OmniDataGridGroup>();
    private IReadOnlyList<OmniDataGridGroup>? _receivedGroups;
    // The groups the reader set (or a saved state restored): the ones the state keeps. A different value from
    // the host is the host's decision and forgets them; the same value is its binding.
    private IReadOnlyList<OmniDataGridGroup>? _chosenGroups;

    // A tree grid is not grouped: its rows are already nested under their parents.
    public IReadOnlyList<OmniDataGridGroup> ActiveGroups => grid.AllowGrouping && !grid.Tree.Active ? _groups : Array.Empty<OmniDataGridGroup>();

    /// <summary>The groups the reader set (or a saved state restored), null while they are the host's.</summary>
    public IReadOnlyList<OmniDataGridGroup>? ChosenGroups => _chosenGroups;

    /// <summary>Takes the groups the host passes when they are a new value (another content).</summary>
    internal void Mirror()
    {
        var received = grid.Groups;
        if (_receivedGroups is not null && _receivedGroups.SequenceEqual(received))
        {
            return;
        }

        _receivedGroups = [.. received];
        _groups = _receivedGroups;
        if (_chosenGroups is not null && !_chosenGroups.SequenceEqual(received))
        {
            _chosenGroups = null;
        }
    }

    /// <summary>
    /// Takes saved groups, on a grid that lets the reader group (<c>AllowGrouping</c>) and can group its rows
    /// (not a remote virtualized grid, which only holds blocks of them). True when they changed the groups;
    /// the caller raises the change.
    /// </summary>
    public bool Restore(IReadOnlyList<OmniDataGridGroup>? groups)
    {
        if (groups is null || !grid.AllowGrouping || grid.Tree.Active || (grid.View.Virtualized && grid.Load is not null))
        {
            return false;
        }

        IReadOnlyList<OmniDataGridGroup> valid = [.. groups.Where(group => group is { Key.Length: > 0 })];
        _chosenGroups = valid;
        if (_groups.SequenceEqual(valid))
        {
            return false;
        }

        _groups = valid;
        return true;
    }

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

        _groups = groups;
        _chosenGroups = groups;
        grid.View.InvalidateLocalProjection();
        grid.RebuildRenderSnapshot();
        await grid.GroupsChanged.InvokeAsync(groups);
        await grid.Persistence.SaveAsync();
    }

    /// <summary>Removes a group from the group panel.</summary>
    internal Task UngroupAsync(OmniDataGridGroup group) =>
        ToggleGroupingAsync(grid.ColumnSet.EffectiveColumns.First(column => column.Key == group.Key));

    internal string GroupTitle(OmniDataGridGroup group) =>
        grid.ColumnSet.Find(group.Key)?.Title ?? group.Key;

    /// <summary>A host that changes AllGroupsExpanded takes back the default that opening or closing every group had replaced.</summary>
    internal void ObserveParameters()
    {
        Mirror();
        if (grid.AllGroupsExpanded != _observedDefault)
        {
            _observedDefault = grid.AllGroupsExpanded;
            _allExpanded = null;
        }
    }

    private bool DefaultExpanded => _allExpanded ?? grid.AllGroupsExpanded;

    private bool IsGroupExpanded(string path) => DefaultExpanded
        ? !_collapsedGroups.Contains(path)
        : _collapsedGroups.Contains(path);

    /// <summary>Whether the grid offers the button that opens or closes every group at once.</summary>
    internal bool ShowsExpandAllGroups => grid.ShowExpandAllGroups && ActiveGroups.Count > 0;

    /// <summary>Whether every group is open: the default is open and no group was closed on its own.</summary>
    internal bool AllGroupsOpen => DefaultExpanded && _collapsedGroups.Count == 0;

    /// <summary>Opens every group of every level, or closes them all, then draws the rows again.</summary>
    internal Task SetAllAsync(bool expanded)
    {
        _allExpanded = expanded;
        _collapsedGroups.Clear();
        grid.Virtual.RefreshSlots();
        grid.Render();
        return Task.CompletedTask;
    }

    /// <summary>
    /// The button that opens or closes every group, drawn inside the group panel; its name says what the
    /// next click does, so no aria-expanded repeats it. Null while it is not offered.
    /// </summary>
    internal RenderFragment? ExpandAllButton => ShowsExpandAllGroups ? ToggleAllButton : null;

    /// <summary>The same button in a band of its own, for a grid that shows no group panel.</summary>
    internal RenderFragment? ExpandAllBand => ShowsExpandAllGroups && !(grid.ShowGroupPanel && grid.AllowGrouping)
        ? builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "omni-data-grid__group-panel");
            builder.AddContent(2, ToggleAllButton);
            builder.CloseElement();
        }
        : null;

    private RenderFragment ToggleAllButton => builder =>
    {
        var open = AllGroupsOpen;
        builder.OpenComponent<OmniButton>(0);
        builder.AddComponentParameter(1, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddComponentParameter(2, nameof(OmniButton.Size), OmniControlSize.Small);
        builder.AddComponentParameter(3, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(grid, ToggleAllAsync));
        builder.AddComponentParameter(4, nameof(OmniButton.Icon), (RenderFragment)(icon =>
        {
            icon.OpenComponent<OmniIcon>(0);
            icon.AddComponentParameter(1, nameof(OmniIcon.Name), open ? OmniIconName.ChevronUp : OmniIconName.ChevronDown);
            icon.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Small);
            icon.CloseComponent();
        }));
        builder.AddComponentParameter(5, nameof(OmniButton.ChildContent), (RenderFragment)(content => content.AddContent(0, grid.Text(open ? "GridCollapseAllGroups" : "GridExpandAllGroups"))));
        builder.CloseComponent();
    };

    /// <summary>The click of that button: closes every group when all are open, opens them all otherwise.</summary>
    internal Task ToggleAllAsync() => SetAllAsync(!AllGroupsOpen);

    internal void ToggleGroup(string path)
    {
        if (!_collapsedGroups.Remove(path))
        {
            _collapsedGroups.Add(path);
        }

        grid.Virtual.RefreshSlots();
    }

    /// <summary>The rows of <paramref name="items"/> grouped by <paramref name="groups"/>, each group opened by its header row.</summary>
    public IReadOnlyList<GridRenderRow<TItem>> GroupedRows(IReadOnlyList<OmniDataGridGroup> groups, IReadOnlyList<TItem> items)
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
