using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A pointer event on a data cell, passed to <c>OnCellClick</c> and <c>OnCellDoubleClick</c>: the row's
/// item and position, the column the cell belongs to, the value it reads, and the modifier keys and
/// pointer position, so a host can open a drill-down on the very figure that was double-clicked.
/// </summary>
/// <typeparam name="TItem">The type of the grid rows.</typeparam>
public sealed class OmniDataGridCellMouseEventArgs<TItem>
{
    internal OmniDataGridCellMouseEventArgs(TItem item, int rowIndex, OmniDataGridColumnDefinition<TItem> column, int columnIndex, MouseEventArgs mouse)
    {
        Item = item;
        RowIndex = rowIndex;
        ColumnKey = column.Key;
        ColumnTitle = column.Title;
        ColumnProperty = column.Property;
        ColumnIndex = columnIndex;
        Value = column.Value(item);
        CtrlKey = mouse.CtrlKey;
        ShiftKey = mouse.ShiftKey;
        AltKey = mouse.AltKey;
        MetaKey = mouse.MetaKey;
        ClientX = mouse.ClientX;
        ClientY = mouse.ClientY;
    }

    /// <summary>The row's item.</summary>
    public TItem Item { get; }

    /// <summary>The row's position among the rows of the current view, from 0.</summary>
    public int RowIndex { get; }

    /// <summary>The key of the cell's column (its <c>Key</c>, else its <c>Property</c>, else its <c>Title</c>).</summary>
    public string ColumnKey { get; }

    /// <summary>The title of the cell's column.</summary>
    public string ColumnTitle { get; }

    /// <summary>The property path the column reads, or null for a column read by <c>Value</c> or made of a template.</summary>
    public string? ColumnProperty { get; }

    /// <summary>The column's position among the visible data columns, from 0 (the grid's own control columns are not counted).</summary>
    public int ColumnIndex { get; }

    /// <summary>The value the column reads for this row (its <c>Value</c> or <c>Property</c>), unformatted; null for a column that reads none.</summary>
    public object? Value { get; }

    /// <summary>Whether Ctrl was held.</summary>
    public bool CtrlKey { get; }

    /// <summary>Whether Shift was held.</summary>
    public bool ShiftKey { get; }

    /// <summary>Whether Alt was held.</summary>
    public bool AltKey { get; }

    /// <summary>Whether the Meta (Command, Windows) key was held.</summary>
    public bool MetaKey { get; }

    /// <summary>Horizontal pointer position in the viewport.</summary>
    public double ClientX { get; }

    /// <summary>Vertical pointer position in the viewport.</summary>
    public double ClientY { get; }
}
