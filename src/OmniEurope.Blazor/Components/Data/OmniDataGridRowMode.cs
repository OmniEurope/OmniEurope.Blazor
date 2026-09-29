namespace OmniEurope.Blazor.Components;

/// <summary>
/// How many rows of an <see cref="OmniDataGrid{TItem}"/> may be in the same state at once: edited
/// (<see cref="OmniDataGrid{TItem}.EditMode"/>) or expanded (<see cref="OmniDataGrid{TItem}.ExpandMode"/>).
/// </summary>
public enum OmniDataGridRowMode
{
    /// <summary>One row at a time: putting a row in that state takes the previous one out of it.</summary>
    Single,

    /// <summary>Several rows can be in that state at the same time.</summary>
    Multiple
}
