using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The selected rows of a grid, kept by the grid and mirrored from the host's value when it passes a
/// new one, and the row activation (click, Enter, Space) that selects a row or reports it.
/// </summary>
internal sealed class GridSelection<TItem>(OmniDataGrid<TItem> grid)
{
    // The grid's own selection, mirrored from the parameter when the host passes a new value: a
    // component never writes its own parameters.
    private IReadOnlyList<TItem> _selection = Array.Empty<TItem>();
    private IReadOnlyList<TItem>? _receivedValue;
    private HashSet<object> _selectedKeyIndex = [];

    /// <summary>Takes the host's selection when it is a new list.</summary>
    internal void Mirror()
    {
        if (!ReferenceEquals(_receivedValue, grid.Value))
        {
            _receivedValue = grid.Value;
            _selection = grid.Value;
        }
    }

    /// <summary>Indexes the keys of the selection for the rows about to render.</summary>
    internal void Reindex() => _selectedKeyIndex = SelectionKeys().ToHashSet();

    internal bool IsSelected(object key) => _selectedKeyIndex.Contains(key);

    internal bool RowsAreInteractive => grid.AllowRowSelectOnRowClick || grid.OnRowClick.HasDelegate || grid.OnRowDoubleClick.HasDelegate;

    internal async Task ToggleSelectionAsync(TItem item)
    {
        var key = grid.ItemKey(item);
        var keys = SelectionKeys().ToList();
        var selecting = !keys.Remove(key);
        if (selecting)
        {
            if (grid.SelectionMode == OmniDataGridSelectionMode.Single)
            {
                keys.Clear();
            }
            keys.Add(key);
        }

        await CommitSelectionAsync(keys);
    }

    /// <summary>
    /// The rows the header checkbox acts on: the ones on screen that the host lets be selected.
    /// Rows of other pages are left as they are, which is what a reader expects from a box that sits
    /// above the rows it can see.
    /// </summary>
    private IReadOnlyList<TItem> SelectableVisibleRows => grid.View.VisibleItems
        .Where(item => grid.RowRender is null || grid.Rows.Describe(item, 0, [], false).Selectable)
        .ToArray();

    internal bool HasSelectableVisibleRows => SelectableVisibleRows.Count > 0;

    internal bool AllVisibleSelected
    {
        get
        {
            var rows = SelectableVisibleRows;
            return rows.Count > 0 && rows.All(item => IsSelected(grid.ItemKey(item)));
        }
    }

    /// <summary>Selects every selectable row on screen, or clears them when they all already are.</summary>
    internal async Task ToggleAllSelectionAsync()
    {
        var rows = SelectableVisibleRows;
        var clearing = rows.Count > 0 && rows.All(item => IsSelected(grid.ItemKey(item)));
        var keys = SelectionKeys().ToList();
        foreach (var item in rows)
        {
            var key = grid.ItemKey(item);
            if (clearing)
            {
                keys.Remove(key);
            }
            else if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        await CommitSelectionAsync(keys);
    }

    /// <summary>The keys of the selected rows, read from the grid's own selection.</summary>
    private IReadOnlyList<object> SelectionKeys() => _selection.Select(grid.ItemKey).ToArray();

    /// <summary>Keeps the new selection and reports it to the host.</summary>
    private async Task CommitSelectionAsync(IReadOnlyCollection<object> keys)
    {
        _selectedKeyIndex = keys.ToHashSet();
        _selection = SelectedItems();
        await grid.ValueChanged.InvokeAsync(_selection);
    }

    /// <summary>
    /// The selected rows once the keys changed: the rows already selected that still are, rows of other
    /// pages included, then the rows on screen newly selected.
    /// </summary>
    private TItem[] SelectedItems()
    {
        var reported = new HashSet<object>();
        return _selection.Concat(grid.Rows.CurrentRows())
            .Where(candidate =>
            {
                var key = grid.ItemKey(candidate);
                return _selectedKeyIndex.Contains(key) && reported.Add(key);
            })
            .ToArray();
    }

    private async Task ActivateRowAsync(GridRenderRow<TItem> row, MouseEventArgs? mouse = null)
    {
        if (!row.Selectable)
        {
            return;
        }

        if (grid.AllowRowSelectOnRowClick && grid.SelectionMode != OmniDataGridSelectionMode.None)
        {
            await ToggleSelectionAsync(row.Item);
        }

        if (grid.OnRowClick.HasDelegate)
        {
            await grid.OnRowClick.InvokeAsync(new OmniDataGridRowMouseEventArgs<TItem>(row.Item, row.Index, mouse));
        }
    }

    private Task RowKeyDownAsync(KeyboardEventArgs args, GridRenderRow<TItem> row) =>
        args.Key is "Enter" or " " ? ActivateRowAsync(row) : Task.CompletedTask;

    internal EventCallback<MouseEventArgs> RowClickCallback(GridRenderRow<TItem> row) => RowsAreInteractive
        ? EventCallback.Factory.Create<MouseEventArgs>(grid, mouse => ActivateRowAsync(row, mouse))
        : default;

    internal EventCallback<MouseEventArgs> RowContextMenuCallback(GridRenderRow<TItem> row) => grid.OnRowContextMenu.HasDelegate
        ? EventCallback.Factory.Create<MouseEventArgs>(grid, mouse =>
            grid.OnRowContextMenu.InvokeAsync(new OmniDataGridRowMouseEventArgs<TItem>(row.Item, row.Index, mouse)))
        : default;

    internal EventCallback<MouseEventArgs> RowDoubleClickCallback(GridRenderRow<TItem> row) => grid.OnRowDoubleClick.HasDelegate
        ? EventCallback.Factory.Create<MouseEventArgs>(grid, mouse =>
            grid.OnRowDoubleClick.InvokeAsync(new OmniDataGridRowMouseEventArgs<TItem>(row.Item, row.Index, mouse)))
        : default;

    /// <summary>The click of a data cell, or nothing while the host does not listen; a row being edited renders its cells without it.</summary>
    internal EventCallback<MouseEventArgs> CellClickCallback(GridRenderRow<TItem> row, OmniDataGridColumnDefinition<TItem> column) =>
        CellCallback(grid.OnCellClick, row, column);

    /// <summary>The double click of a data cell, or nothing while the host does not listen; a row being edited renders its cells without it.</summary>
    internal EventCallback<MouseEventArgs> CellDoubleClickCallback(GridRenderRow<TItem> row, OmniDataGridColumnDefinition<TItem> column) =>
        CellCallback(grid.OnCellDoubleClick, row, column);

    private EventCallback<MouseEventArgs> CellCallback(
        EventCallback<OmniDataGridCellMouseEventArgs<TItem>> target, GridRenderRow<TItem> row, OmniDataGridColumnDefinition<TItem> column) =>
        target.HasDelegate
            ? EventCallback.Factory.Create<MouseEventArgs>(grid, mouse => target.InvokeAsync(
                new OmniDataGridCellMouseEventArgs<TItem>(row.Item, row.Index, column, IndexOfVisible(column), mouse)))
            : default;

    private int IndexOfVisible(OmniDataGridColumnDefinition<TItem> column)
    {
        var columns = grid.ColumnSet.VisibleColumns;
        for (var index = 0; index < columns.Count; index++)
        {
            if (ReferenceEquals(columns[index], column))
            {
                return index;
            }
        }

        return -1;
    }

    internal EventCallback<KeyboardEventArgs> RowKeyDownCallback(GridRenderRow<TItem> row) => RowsAreInteractive
        ? EventCallback.Factory.Create<KeyboardEventArgs>(grid, args => RowKeyDownAsync(args, row))
        : default;

    internal string? SelectionState(TItem item) => grid.SelectionMode == OmniDataGridSelectionMode.None
        ? null
        : IsSelected(grid.ItemKey(item)) ? "true" : "false";
}
