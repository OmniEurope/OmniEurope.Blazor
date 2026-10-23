namespace OmniEurope.Blazor.Components;

/// <summary>Where the widths of an <see cref="OmniDataGrid{TItem}"/>'s columns come from.</summary>
public enum OmniDataGridColumnSizing
{
    /// <summary>
    /// The columns' declaration decides: a <see cref="OmniDataGridColumn{TItem}.Width"/> is the width,
    /// <see cref="OmniDataGridColumn{TItem}.MinWidth"/> a floor, and a column without a width sizes itself
    /// to its longest cell and shares the rest. The default.
    /// </summary>
    Declared,

    /// <summary>
    /// The content decides: each column takes the width of its widest content (its header on one line with
    /// its icons included), the room left goes to the columns marked <see cref="OmniDataGridColumn{TItem}.Grow"/>
    /// (or is shared out), and a narrower grid scrolls sideways instead of cutting anything.
    /// <see cref="OmniDataGridColumn{TItem}.Width"/> is ignored.
    /// </summary>
    Content
}
