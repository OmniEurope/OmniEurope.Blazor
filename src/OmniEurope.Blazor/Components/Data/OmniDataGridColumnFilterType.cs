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
    MultiSelect
}
