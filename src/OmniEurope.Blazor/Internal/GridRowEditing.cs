using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The rows of a grid in edit mode, entered, saved and cancelled by their key.</summary>
internal sealed class GridRowEditing<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly HashSet<object> _editedKeys = [];

    // The row last put in edit mode, whose editor takes the focus after the next render (recette R1-5).
    private object? _focusKey;

    internal bool IsRowEditing(TItem item) => _editedKeys.Contains(grid.ItemKey(item));

    /// <summary>Whether this row is the one whose editor takes the focus after the render (<c>data-omni-edit-focus</c>).</summary>
    internal bool TakesFocus(TItem item) => _focusKey is not null && _focusKey.Equals(grid.ItemKey(item)) && IsRowEditing(item);

    /// <summary>Whether a row in edit mode waits for its editor to take the focus.</summary>
    internal bool FocusRequested => _focusKey is not null && _editedKeys.Contains(_focusKey);

    /// <summary>The focus was given (or the row has no editor): the request is spent.</summary>
    internal void FocusGiven() => _focusKey = null;

    /// <summary>Puts a row in edit mode; in single mode the row previously edited leaves it without any callback.</summary>
    internal async Task EditAsync(TItem item)
    {
        if (grid.EditMode == OmniDataGridRowMode.Single)
        {
            _editedKeys.Clear();
        }

        _editedKeys.Add(grid.ItemKey(item));
        _focusKey = grid.ItemKey(item);
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
