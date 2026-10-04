using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>One row of a tree grid as the body shows it: its item, depth and expander state.</summary>
internal sealed record GridTreeNode<TItem>(TItem Item, int Level, bool HasChildren, bool Toggleable, bool Expanded);

/// <summary>
/// The hierarchical rows of a grid given <see cref="OmniDataGrid{TItem}.ChildrenOf"/>: the expanded rows,
/// kept by the grid and mirrored from the host's keys, and the flattening of the tree into the rows on
/// screen, siblings sorted by the grid's sorts and the filters kept with the ancestors of what they match.
/// </summary>
internal sealed class GridTree<TItem>(OmniDataGrid<TItem> grid)
{
    private static readonly IReadOnlyDictionary<string, GridColumnFilter> NoFilter = new Dictionary<string, GridColumnFilter>();
    private HashSet<object> _expanded = [];
    // Keys whose state is known (toggled, given by the host, or decided by InitiallyExpanded on first sight).
    private HashSet<object> _decided = [];
    private IReadOnlyList<object>? _receivedKeys;
    private (IReadOnlyList<GridTreeNode<TItem>> Visible, IReadOnlyList<TItem> All, IReadOnlyList<TItem> Roots)? _flat;

    internal bool Active => grid.ChildrenOf is not null;

    /// <summary>The rows on screen, in display order.</summary>
    internal IReadOnlyList<GridTreeNode<TItem>> Nodes => Flatten().Visible;

    /// <summary>Every row the filters keep, at every depth and whatever is collapsed, in display order.</summary>
    internal IReadOnlyList<TItem> AllItems => Flatten().All;

    /// <summary>The top-level rows the filters keep, sorted.</summary>
    internal IReadOnlyList<TItem> Roots => Flatten().Roots;

    /// <summary>Takes the host's expanded keys when they are a new list; the host's list wins over earlier decisions.</summary>
    internal void Mirror()
    {
        if (ReferenceEquals(_receivedKeys, grid.TreeExpandedKeys))
        {
            return;
        }

        _receivedKeys = grid.TreeExpandedKeys;
        _expanded = [.. grid.TreeExpandedKeys];
        _decided.UnionWith(_expanded);
        Invalidate();
    }

    internal void Invalidate() => _flat = null;

    internal bool IsToggleable(TItem item) => grid.CanToggleTreeRow?.Invoke(item) ?? true;

    private bool IsExpanded(TItem item)
    {
        if (!IsToggleable(item))
        {
            return true;
        }

        var key = grid.ItemKey(item);
        if (_decided.Add(key) && grid.InitiallyExpanded?.Invoke(item) == true)
        {
            _expanded.Add(key);
        }

        return _expanded.Contains(key);
    }

    internal Task ToggleAsync(TItem item) => SetAsync([item], !_expanded.Contains(grid.ItemKey(item)));

    /// <summary>Opens or closes every row that has children and can be toggled, reported once.</summary>
    internal Task SetAllAsync(bool expanded) => SetAsync(Parents(AllNodes(grid.Items, [])).ToArray(), expanded);

    private async Task SetAsync(IReadOnlyList<TItem> items, bool expanded)
    {
        foreach (var item in items.Where(IsToggleable))
        {
            var key = grid.ItemKey(item);
            _decided.Add(key);
            if (expanded)
            {
                _expanded.Add(key);
            }
            else
            {
                _expanded.Remove(key);
            }
        }

        Invalidate();
        _receivedKeys = grid.TreeExpandedKeys;
        await grid.TreeExpandedKeysChanged.InvokeAsync(_expanded.ToArray());
        grid.Render();
    }

    private IEnumerable<TItem> Parents(IEnumerable<TItem> items) =>
        items.Where(item => Children(item).Count > 0);

    private IReadOnlyList<TItem> Children(TItem item) => grid.ChildrenOf!(item) ?? Array.Empty<TItem>();

    private (IReadOnlyList<GridTreeNode<TItem>> Visible, IReadOnlyList<TItem> All, IReadOnlyList<TItem> Roots) Flatten()
    {
        if (_flat is not { } flat)
        {
            var visible = new List<GridTreeNode<TItem>>();
            var all = new List<TItem>();
            var roots = Walk(grid.Items, 0, false, true, grid.Query.HasActiveFilter(), visible, all, []);
            _flat = flat = (visible, all, roots);
        }

        return flat;
    }

    /// <summary>Every row of the tree, unfiltered, each once even if the tree loops back on itself.</summary>
    private IEnumerable<TItem> AllNodes(IEnumerable<TItem> items, HashSet<object> seen)
    {
        foreach (var item in items)
        {
            if (!seen.Add(grid.ItemKey(item)))
            {
                continue;
            }

            yield return item;
            foreach (var child in AllNodes(Children(item), seen))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Sorts one list of siblings and keeps the rows that match the filters, that sit under a row that does,
    /// or that lead to one; returns the kept siblings. While a filter is active every kept row is shown open,
    /// so a match deep in the tree is seen without opening its ancestors. <paramref name="path"/> guards
    /// against a row that is its own ancestor.
    /// </summary>
    private List<TItem> Walk(
        IReadOnlyList<TItem> siblings, int level, bool ancestorMatched, bool shown, bool filtering,
        List<GridTreeNode<TItem>> visible, List<TItem> all, HashSet<object> path)
    {
        var columns = grid.ColumnSet.EffectiveColumns;
        var sorted = GridProjection<TItem>.Create(siblings, columns, NoFilter, grid.Query.Sorts,
            grid.CaseSensitiveFilters, grid.IgnoreDiacritics, 1, int.MaxValue).Items;
        var matching = filtering && !ancestorMatched
            ? GridProjection<TItem>.Create(siblings, columns, grid.Query.Filters, [], grid.CaseSensitiveFilters, grid.IgnoreDiacritics, 1, int.MaxValue).Items.ToHashSet()
            : null;
        var kept = new List<TItem>();
        foreach (var item in sorted)
        {
            var key = grid.ItemKey(item);
            if (!path.Add(key))
            {
                continue;
            }

            var matched = matching is null || matching.Contains(item);
            var children = Children(item);
            var expanded = filtering || IsExpanded(item);
            var node = new GridTreeNode<TItem>(item, level, children.Count > 0, IsToggleable(item), expanded);
            var visibleMark = visible.Count;
            var allMark = all.Count;
            if (shown)
            {
                visible.Add(node);
            }

            all.Add(item);
            var keptChildren = Walk(children, level + 1, matched, shown && expanded, filtering, visible, all, path);
            path.Remove(key);
            if (matched || keptChildren.Count > 0)
            {
                kept.Add(item);
                if (filtering && shown)
                {
                    visible[visibleMark] = node with { HasChildren = keptChildren.Count > 0 };
                }
            }
            else
            {
                visible.RemoveRange(visibleMark, visible.Count - visibleMark);
                all.RemoveRange(allMark, all.Count - allMark);
            }
        }

        return kept;
    }
}
