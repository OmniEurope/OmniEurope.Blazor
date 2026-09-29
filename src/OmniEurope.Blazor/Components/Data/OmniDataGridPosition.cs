namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where a band of an <see cref="OmniDataGrid{TItem}"/> is rendered: the pager
/// (<see cref="OmniDataGrid{TItem}.PagerPosition"/>) or the footer row
/// (<see cref="OmniDataGrid{TItem}.FooterPosition"/>).
/// </summary>
public enum OmniDataGridPosition
{
    /// <summary>After the rows: the pager under the table, the footer row in a real table footer.</summary>
    Bottom,

    /// <summary>
    /// Before the rows: the pager above the table, the footer row directly under the header. What a
    /// footer holding a creation form needs: at the bottom it sits past every row, so adding an entry
    /// to a full grid starts with a scroll.
    /// </summary>
    Top,

    /// <summary>
    /// Both places. The band is rendered twice, so a footer template rendered this way must not
    /// carry element ids.
    /// </summary>
    TopAndBottom
}
