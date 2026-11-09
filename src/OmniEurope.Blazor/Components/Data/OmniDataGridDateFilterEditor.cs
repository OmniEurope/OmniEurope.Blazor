namespace OmniEurope.Blazor.Components;

/// <summary>
/// Which editor the header filter menu of a date column holds (the <c>HeaderMenuDateFilter</c> parameter of an
/// <see cref="OmniDataGrid{TItem}"/> or of one <see cref="OmniDataGridColumn{TItem}"/>). Both read and write the
/// same encoded filter value, so a value set from one is shown by the other.
/// </summary>
public enum OmniDataGridDateFilterEditor
{
    /// <summary>The From and To range, with its two native pickers, and no typed field.</summary>
    Range,

    /// <summary>The one typed field (a day, a month or a year), as in the inline filter row, and no range.</summary>
    Single
}
