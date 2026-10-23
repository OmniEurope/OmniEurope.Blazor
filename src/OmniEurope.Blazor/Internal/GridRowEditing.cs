using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The rows of a grid in edit mode, entered, saved and cancelled by their key.</summary>
internal sealed class GridRowEditing<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly HashSet<object> _editedKeys = [];

    // The row last put in edit mode, whose editor takes the focus after the next render (recette R1-5).
    private object? _focusKey;

    // The column whose editor takes that focus, when the entry named one (a cell trigger); otherwise the
    // script falls back to the cell last pressed, then to the first editor of the row.
    private string? _focusColumn;

    internal bool IsRowEditing(TItem item) => _editedKeys.Contains(grid.ItemKey(item));

    /// <summary>Whether this row is the one whose editor takes the focus after the render (<c>data-omni-edit-focus</c>).</summary>
    internal bool TakesFocus(TItem item) => _focusKey is not null && _focusKey.Equals(grid.ItemKey(item)) && IsRowEditing(item);

    /// <summary>Whether a row in edit mode waits for its editor to take the focus.</summary>
    internal bool FocusRequested => _focusKey is not null && _editedKeys.Contains(_focusKey);

    /// <summary>The key of the column whose editor takes the focus, or null to let the script choose.</summary>
    internal string? FocusColumn => _focusColumn;

    /// <summary>The focus was given (or the row has no editor): the request is spent.</summary>
    internal void FocusGiven()
    {
        _focusKey = null;
        _focusColumn = null;
    }

    /// <summary>Whether a click or a key in a cell of this column puts its row in edit mode (<see cref="OmniDataGrid{TItem}.EditTrigger"/>).</summary>
    internal bool TriggersFrom(OmniDataGridColumnDefinition<TItem> column) =>
        grid.EditTrigger == OmniDataGridEditTrigger.Cell && column.EditTemplate is not null;

    /// <summary>Puts a row in edit mode; in single mode the row previously edited leaves it without any callback.</summary>
    internal async Task EditAsync(TItem item, string? focusColumn = null)
    {
        if (grid.EditMode == OmniDataGridRowMode.Single)
        {
            _editedKeys.Clear();
        }

        _editedKeys.Add(grid.ItemKey(item));
        _focusKey = grid.ItemKey(item);
        _focusColumn = focusColumn;
        await grid.OnRowEdit.InvokeAsync(item);
        grid.Render();
    }

    /// <summary>
    /// A cell was pressed (script) or its button activated (keyboard): the row enters edit mode, the editor of that
    /// cell taking the focus. Nothing when the grid does not edit on a cell, the column is not visible or has no
    /// edit template, the row is not on the page, or it is already being edited (the pointer then only moves the focus).
    /// </summary>
    internal Task EditCellAsync(int rowIndex, string columnKey)
    {
        var column = grid.ColumnSet.FindVisible(columnKey);
        if (column is null || !TriggersFrom(column))
        {
            return Task.CompletedTask;
        }

        var row = grid.Rows.RenderRows().FirstOrDefault(candidate => candidate.HasItem && candidate.Index == rowIndex);
        return row is null || IsRowEditing(row.Item) ? Task.CompletedTask : EditAsync(row.Item, column.Key);
    }

    internal async Task UpdateAsync(TItem item)
    {
        _editedKeys.Remove(grid.ItemKey(item));
        await grid.OnRowUpdate.InvokeAsync(item);
        grid.Render();
    }

    internal async Task CancelAsync(TItem item)
    {
        _editedKeys.Remove(grid.ItemKey(item));
        await grid.OnRowEditCancel.InvokeAsync(item);
        grid.Render();
    }
}
