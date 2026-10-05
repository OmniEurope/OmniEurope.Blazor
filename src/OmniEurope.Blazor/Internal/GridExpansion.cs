using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The open detail rows of a grid: the keys the host passes, the expand button of each row and the
/// header button that opens or closes every expandable row on screen.
/// </summary>
internal sealed class GridExpansion<TItem>(OmniDataGrid<TItem> grid)
{
    private HashSet<object> _expandedKeyIndex = [];

    /// <summary>Indexes the host's expanded keys for the rows about to render.</summary>
    internal void Reindex() => _expandedKeyIndex = grid.ExpandedKeys.ToHashSet();

    internal bool IsExpanded(object key) => _expandedKeyIndex.Contains(key);

    internal async Task ToggleExpandedAsync(TItem item)
    {
        var key = grid.ItemKey(item);
        var keys = grid.ExpandedKeys.ToList();
        var expanding = !keys.Remove(key);
        if (expanding)
        {
            if (grid.ExpandMode == OmniDataGridRowMode.Single)
            {
                keys.Clear();
            }
            keys.Add(key);
        }

        _expandedKeyIndex = keys.ToHashSet();
        grid.Virtual.RefreshSlots();
        await grid.ExpandedKeysChanged.InvokeAsync(keys);
        await (expanding ? grid.OnRowExpand.InvokeAsync(item) : grid.OnRowCollapse.InvokeAsync(item));
    }

    /// <summary>The expandable rows on screen, the ones the header button acts on.</summary>
    private IReadOnlyList<object> ExpandableVisibleKeys => grid.Rows.PageRows()
        .Where(row => row.Expandable)
        .Select(row => grid.ItemKey(row.Item))
        .ToArray();

    internal bool AllVisibleExpanded
    {
        get
        {
            var keys = ExpandableVisibleKeys;
            return keys.Count > 0 && keys.All(IsExpanded);
        }
    }

    /// <summary>
    /// Only offered when several rows may be open at once: under a single-row expand mode the
    /// button could only ever break the rule it sits above. A virtualized grid has no page of rows to
    /// act on, as for the header checkbox: the button opened nothing there and is not offered.
    /// </summary>
    internal bool ShowsExpandAll => grid.ShowExpandAll && grid.ExpandMode == OmniDataGridRowMode.Multiple && !grid.View.Virtualized;

    /// <summary>
    /// Opens every expandable row on screen, or closes them all when they already are open. Rows of
    /// other pages keep their state: the previous version compared the count of every expanded key
    /// with the rows on screen, so a row left open on another page turned the button into a no-op.
    /// </summary>
    internal async Task ToggleAllExpandedAsync()
    {
        var visible = ExpandableVisibleKeys;
        var collapsing = visible.Count > 0 && visible.All(IsExpanded);
        var keys = grid.ExpandedKeys.ToList();
        foreach (var key in visible)
        {
            if (collapsing)
            {
                keys.Remove(key);
            }
            else if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        _expandedKeyIndex = keys.ToHashSet();
        grid.Virtual.RefreshSlots();
        await grid.ExpandedKeysChanged.InvokeAsync(keys);
    }
}
