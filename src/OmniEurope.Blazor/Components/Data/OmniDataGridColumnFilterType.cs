namespace OmniEurope.Blazor.Components;

/// <summary>
/// Shape of the value control a filterable column renders, inline and in the header menu. A column
/// needing something none of these covers supplies its own <c>FilterTemplate</c> instead.
/// </summary>
public enum OmniDataGridColumnFilterType
{
    /// <summary>Free-text input, matched with the column's <c>FilterOperator</c>.</summary>
    Text,

    /// <summary>Closed dropdown of the column's distinct values, matched with equality.</summary>
    Select,

    /// <summary>Text input with a suggestion list built from the column's distinct values.</summary>
    Combo,

    /// <summary>
    /// Checkable list of the column's values; a row matches any of the checked ones. Add the
    /// column's <c>FilterSearchable</c> to put a narrowing box above the list.
    /// </summary>
    MultiSelect,

    /// <summary>
    /// A start and an end date, either optional. A day alone covers the whole day, and a range
    /// runs from the start of its first day to the end of its last one; the column's
    /// <c>FilterIncludesTime</c> lets the user pick the hours too. Read by the grid through
    /// <see cref="OmniDataGridDateRange"/>, and sent to a remote loader as two bounds.
    /// </summary>
    DateRange
}
