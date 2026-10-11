using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The column chooser of a grid's header bar (<c>AllowColumnHide</c>, <c>AllowColumnReorder</c>): a box per
/// column that shows or hides it, two arrows per column that move it to the left or to the right (the
/// keyboard path of the header drag, which lands here too), the reset of the layout, and the polite
/// announcement of each change for screen readers.
/// </summary>
internal sealed class GridColumnChooser<TItem>(OmniDataGrid<TItem> grid)
{
    private string _announcement = string.Empty;
    private bool _announceToggle;
    private (string Key, int Step)? _focusRequest;
    private readonly GridColumnChooserView<TItem> _view = new(grid);

    /// <summary>Whether the header bar carries the chooser.</summary>
    internal bool Shown => grid.AllowColumnHide || grid.AllowColumnReorder;

    /// <summary>The chooser itself, at the end of the header bar.</summary>
    internal RenderFragment View => _view.Render;

    /// <summary>The list of the open chooser, where the focus goes back after a move.</summary>
    internal ElementReference List { get; set; }

    /// <summary>The last change, read by the grid's live region.</summary>
    internal string Announcement => _announcement;

    /// <summary>
    /// The columns listed, in their arranged order, hidden ones included. While the chooser only hides
    /// columns, a column that cannot be hidden (<c>Hideable</c> false) is not listed; while it also moves
    /// them, every column is, so each one can be moved.
    /// </summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Entries =>
        [.. grid.Arrangement.Arrange(grid.ColumnSet.EffectiveColumns).Where(column => grid.AllowColumnReorder || column.Hideable)];

    /// <summary>The name of a column in the chooser: its title, else its key (a column titled by a template).</summary>
    internal static string NameOf(OmniDataGridColumnDefinition<TItem> column) =>
        string.IsNullOrWhiteSpace(column.Title) ? column.Key : column.Title;

    /// <summary>Whether a column shows on the grid, so its box tells what the reader sees (a kept last column included).</summary>
    internal bool IsVisible(OmniDataGridColumnDefinition<TItem> column) => grid.ColumnSet.VisibleColumns.Contains(column);

    /// <summary>A box can change a hideable column, except the last visible one, which would leave no column.</summary>
    internal bool CanToggle(OmniDataGridColumnDefinition<TItem> column) =>
        column.Hideable && !(IsVisible(column) && grid.ColumnSet.VisibleColumns.Count <= 1);

    /// <summary>Whether an arrow can move a column: a neighbour in the list, of the same frozen group.</summary>
    internal bool CanMove(OmniDataGridColumnDefinition<TItem> column, int step) => Neighbour(column, step) is not null;

    internal async Task ToggleAsync(OmniDataGridColumnDefinition<TItem> column, bool visible)
    {
        if (!grid.AllowColumnHide || !column.Hideable || !await grid.Arrangement.SetVisibleAsync(column.Key, visible))
        {
            return;
        }

        Announce(grid.Text(visible ? "GridColumnShown" : "GridColumnHidden", NameOf(column)));
    }

    /// <summary>Moves a column past its neighbour in the list (-1 to the left, 1 to the right), keeping the focus on its arrows.</summary>
    internal async Task MoveAsync(OmniDataGridColumnDefinition<TItem> column, int step)
    {
        if (Neighbour(column, step) is not { } neighbour)
        {
            return;
        }

        // The neighbour is next to it in the list and of the same frozen group: passing it always changes
        // the order, so the move is not asked whether it happened.
        await grid.Arrangement.MoveNextToAsync(column.Key, neighbour.Key, step > 0);
        _focusRequest = (column.Key, step);
        await MovedAsync(column);
    }

    /// <summary>The end of a header drag: the column lands before or after the one it was dropped on, in its own group.</summary>
    internal async Task DropAsync(string key, string targetKey, bool after)
    {
        var column = grid.ColumnSet.Find(key);
        var target = grid.ColumnSet.Find(targetKey);
        if (!grid.AllowColumnReorder || grid.DisposeRequested || column is null || target is null || column.Frozen != target.Frozen
            || !await grid.Arrangement.MoveNextToAsync(key, targetKey, after))
        {
            return;
        }

        await MovedAsync(column);
        grid.Render();
    }

    /// <summary>The reset action of the chooser (<c>ShowResetLayout</c>).</summary>
    internal async Task ResetAsync()
    {
        await grid.ResetLayoutAsync();
        Announce(grid.Text("GridLayoutReset"));
    }

    /// <summary>After a render, gives the focus back to the arrows of a column moved from the chooser.</summary>
    internal async Task AfterRenderAsync()
    {
        if (_focusRequest is not { } request)
        {
            return;
        }

        _focusRequest = null;
        await grid.Script.FocusMoveControlAsync(List, request.Key, request.Step);
    }

    private async Task MovedAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        // Its place among the columns shown; a hidden column moved in the chooser, its place in the list.
        var visible = grid.ColumnSet.VisibleColumns;
        var entries = Entries;
        var place = IndexOf(visible, column) is var shown and >= 0 ? (Index: shown, visible.Count) : (Index: IndexOf(entries, column), entries.Count);
        Announce(grid.Text("GridColumnMoved", NameOf(column), place.Index + 1, place.Count));
        // A move always leaves an order of the reader.
        await grid.OnColumnReorder.InvokeAsync(new OmniDataGridColumnOrderChange(column.Key, [.. grid.Arrangement.Order!]));
    }

    private OmniDataGridColumnDefinition<TItem>? Neighbour(OmniDataGridColumnDefinition<TItem> column, int step)
    {
        if (!grid.AllowColumnReorder)
        {
            return null;
        }

        var entries = Entries;
        var position = IndexOf(entries, column);
        var index = position + step;
        return position >= 0 && index >= 0 && index < entries.Count && entries[index].Frozen == column.Frozen
            ? entries[index]
            : null;
    }

    private static int IndexOf(IReadOnlyList<OmniDataGridColumnDefinition<TItem>> columns, OmniDataGridColumnDefinition<TItem> column)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            if (columns[index].Key == column.Key)
            {
                return index;
            }
        }

        return -1;
    }

    // The same text twice is still announced: a no-break space alternates at its end, so the live region changes.
    private void Announce(string message)
    {
        _announceToggle = !_announceToggle;
        _announcement = _announceToggle ? message : message + ' ';
    }
}
