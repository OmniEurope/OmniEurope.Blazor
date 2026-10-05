using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Flattens a grid's current view into the exact sequence of rows its markup emits: flat or grouped
/// rows, the virtual window of rows, or the slots of a grouped or detailed virtualized grid.
/// </summary>
internal sealed class GridRowBuilder<TItem>(OmniDataGrid<TItem> grid)
{
    private IReadOnlyList<GridRenderRow<TItem>>? _slots;
    private bool _slotsRebuilt;
    private int _slotShape;

    internal IReadOnlyList<GridRenderRow<TItem>> Slots => _slots ??= BuildSlots();

    internal void ForgetSlots() => _slots = null;

    /// <summary>Whether the slots changed shape since the last call, which then forgets it.</summary>
    internal bool TakeSlotsRebuilt()
    {
        var rebuilt = _slotsRebuilt;
        _slotsRebuilt = false;
        return rebuilt;
    }

    // Rendered only for slotted rows: a null value leaves the attribute out of every other grid.
    internal string? SlotAttribute(GridRenderRow<TItem> row) =>
        row.Slot >= 0 ? row.Slot.ToString(CultureInfo.InvariantCulture) : null;

    private IReadOnlyList<GridRenderRow<TItem>> BuildSlots()
    {
        var items = grid.View.VirtualLocalItems;
        var groups = grid.Grouping.ActiveGroups;
        var rows = groups.Count > 0 ? grid.Grouping.GroupedRows(groups, items) : FlatRows(items);
        var slots = rows.Select((row, slot) => row with { Slot = slot }).ToArray();

        // The projection is rebuilt on every parameter pass; the measured heights only go when the
        // shape of the body really changed (another item at a slot, a header or a detail row more or less).
        var signature = new HashCode();
        signature.Add(slots.Length);
        foreach (var slot in slots)
        {
            signature.Add(slot.Index);
            signature.Add(slot.Headers.Count);
            signature.Add(slot.ShowDetail);
            signature.Add(slot.Footers.Count);
        }

        var shape = signature.ToHashCode();
        _slotsRebuilt |= shape != _slotShape;
        _slotShape = shape;
        return slots;
    }

    /// <summary>Flattens the current view into the exact sequence of rows the markup emits.</summary>
    internal IReadOnlyList<GridRenderRow<TItem>> RenderRows()
    {
        var range = grid.Virtual.Range;
        if (grid.View.StructuredVirtual)
        {
            var slots = Slots;
            var end = Math.Min(range.EndIndex, slots.Count);
            var windowRows = new List<GridRenderRow<TItem>>(Math.Max(0, end - range.StartIndex));
            for (var slot = range.StartIndex; slot < end; slot++)
            {
                windowRows.Add(slots[slot]);
            }

            return windowRows;
        }

        if (grid.View.Virtualized)
        {
            var virtualRows = new List<GridRenderRow<TItem>>(Math.Max(0, range.Count));
            for (var index = range.StartIndex; index < range.EndIndex; index++)
            {
                var found = grid.View.TryGetVirtualItem(index, out var virtualItem);
                virtualRows.Add(found
                    ? Describe(virtualItem, index, [], false)
                    : new GridRenderRow<TItem>(index, default!, false, [], false, null, false, false));
            }

            return virtualRows;
        }

        if (grid.Tree.Active)
        {
            return TreeRows();
        }

        var groups = grid.Grouping.ActiveGroups;
        var visible = grid.View.VisibleItems;
        return groups.Count > 0 ? grid.Grouping.GroupedRows(groups, visible) : FlatRows(visible);
    }

    internal IReadOnlyList<GridRenderRow<TItem>> FlatRows(IReadOnlyList<TItem> items)
    {
        var rows = new List<GridRenderRow<TItem>>(items.Count);
        var index = 0;
        foreach (var item in items)
        {
            rows.Add(Describe(item, index, [], grid.DetailTemplate is not null && grid.Expansion.IsExpanded(grid.ItemKey(item))));
            index++;
        }

        return rows;
    }

    private List<GridRenderRow<TItem>> TreeRows()
    {
        var nodes = grid.Tree.Nodes;
        var rows = new List<GridRenderRow<TItem>>(nodes.Count);
        for (var index = 0; index < nodes.Count; index++)
        {
            var item = nodes[index].Item;
            rows.Add(Describe(item, index, [], grid.DetailTemplate is not null && grid.Expansion.IsExpanded(grid.ItemKey(item))) with { Tree = nodes[index] });
        }

        return rows;
    }

    /// <summary>
    /// The rows of the page the header checkbox and the header expand button act on, each described with
    /// the index it is rendered under, so a row callback that reads the index vetoes the same row in the
    /// header as in the body. A row of a closed group is not rendered: it keeps its position on the page.
    /// </summary>
    internal IEnumerable<GridRenderRow<TItem>> PageRows()
    {
        var items = grid.View.VisibleItems;
        if (grid.RowRender is null || items.Count == 0)
        {
            return items.Select((item, position) => new GridRenderRow<TItem>(position, item, true, [], false, null, true, true));
        }

        var rendered = new Dictionary<object, GridRenderRow<TItem>>();
        foreach (var row in RenderRows().Where(row => row.HasItem))
        {
            rendered.TryAdd(grid.ItemKey(row.Item), row);
        }

        return items.Select((item, position) =>
            rendered.TryGetValue(grid.ItemKey(item), out var row) ? row : Describe(item, position, [], false));
    }

    /// <summary>A row of an item, with the class and the controls the host's row callback gives it.</summary>
    internal GridRenderRow<TItem> Describe(TItem item, int index, IReadOnlyList<GridGroupHeader> headers, bool showDetail)
    {
        if (grid.RowRender is null)
        {
            return new GridRenderRow<TItem>(index, item, true, headers, showDetail, null, true, true);
        }

        var args = new OmniDataGridRowRenderArgs<TItem>(item, index);
        grid.RowRender(args);
        return new GridRenderRow<TItem>(index, item, true, headers, showDetail, args.Class, args.Expandable, args.Selectable);
    }

    /// <summary>The items of the rows on screen.</summary>
    internal IEnumerable<TItem> CurrentRows()
    {
        var range = grid.Virtual.Range;
        return grid.View.StructuredVirtual
            ? RenderRows().Where(row => row.HasItem).Select(row => row.Item)
            : grid.View.Virtualized
            ? Enumerable.Range(range.StartIndex, Math.Max(0, range.Count))
                .Select(index => grid.View.TryGetVirtualItem(index, out var item) ? item : default!)
                .Where(item => item is not null)
            : grid.View.VisibleItems;
    }
}
