namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a column's own filter editor is given: the id to label, the current value, the candidate
/// values the grid would have offered, a placeholder, and the callback that applies a new value.
/// This is the extension point for a filter shape none of the built-in
/// <see cref="OmniDataGridColumnFilterType"/> values covers.
/// </summary>
/// <param name="Id">Element id the column's label points at.</param>
/// <param name="Value">Current filter value, encoded by <see cref="OmniDataGridFilterValues"/> when multi-valued.</param>
/// <param name="Suggestions">Candidate values, from the column's FilterValues or derived from its rows.</param>
/// <param name="Placeholder">Placeholder text the built-in editors use.</param>
/// <param name="ValueChanged">Applies a new value; the grid re-filters from there.</param>
public sealed record OmniDataGridFilterContext(
    string Id,
    string Value,
    IReadOnlyList<string> Suggestions,
    string Placeholder,
    Func<string, Task> ValueChanged);
