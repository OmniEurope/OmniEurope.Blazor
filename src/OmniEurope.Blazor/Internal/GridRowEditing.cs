using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The rows of a grid in edit mode, entered, saved and cancelled by their key.</summary>
internal sealed class GridRowEditing<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly HashSet<object> _editedKeys = [];

    internal bool IsRowEditing(TItem item) => _editedKeys.Contains(grid.ItemKey(item));

    /// <summary>Puts a row in edit mode; in single mode the row previously edited leaves it without any callback.</summary>
    internal async Task EditAsync(TItem item)
    {
        if (grid.EditMode == OmniDataGridRowMode.Single)
        {
            _editedKeys.Clear();
        }

        _editedKeys.Add(grid.ItemKey(item));
        await grid.OnRowEdit.InvokeAsync(item);
        grid.Render();
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
