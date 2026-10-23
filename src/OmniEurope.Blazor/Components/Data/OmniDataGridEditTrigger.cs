namespace OmniEurope.Blazor.Components;

/// <summary>
/// What puts a row of an <see cref="OmniDataGrid{TItem}"/> in edit mode
/// (<see cref="OmniDataGrid{TItem}.EditTrigger"/>); <see cref="OmniDataGrid{TItem}.EditMode"/> says how many
/// rows may be edited at once.
/// </summary>
public enum OmniDataGridEditTrigger
{
    /// <summary>
    /// Only an action: the edit button of the row, or <see cref="OmniDataGrid{TItem}.EditRowAsync"/> called
    /// by the host (from <see cref="OmniDataGrid{TItem}.OnRowDoubleClick"/> for instance).
    /// </summary>
    Action,

    /// <summary>
    /// A single click anywhere in a cell whose column has an <see cref="OmniDataGridColumn{TItem}.EditTemplate"/>
    /// (and, for the keyboard, a button named after the column that the cell holds): the row enters edit mode
    /// through <see cref="OmniDataGrid{TItem}.EditRowAsync"/> and the field of that cell takes the focus, its
    /// text selected. A click that comes from a control inside the cell (button, link, field) does nothing.
    /// </summary>
    Cell
}
