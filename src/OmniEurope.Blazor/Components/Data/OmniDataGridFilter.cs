namespace OmniEurope.Blazor.Components;

/// <summary>One filter condition sent to a remote loader, optionally joined with a second one.</summary>
/// <param name="Key">Key of the filtered column.</param>
/// <param name="Operator">Comparison of the first condition.</param>
/// <param name="Value">
/// Value of the first condition; several candidates encoded by <see cref="OmniDataGridFilterValues"/>
/// for <see cref="OmniDataGridFilterOperator.In"/> and <see cref="OmniDataGridFilterOperator.NotIn"/>.
/// </param>
/// <param name="LogicalOperator">How the second condition combines with the first. And by default.</param>
/// <param name="SecondOperator">Comparison of the second condition, or <c>null</c> when there is none.</param>
/// <param name="SecondValue">Value of the second condition, or <c>null</c> when there is none.</param>
public sealed record OmniDataGridFilter(
    string Key,
    OmniDataGridFilterOperator Operator,
    string Value,
    OmniDataGridLogicalOperator LogicalOperator = OmniDataGridLogicalOperator.And,
    OmniDataGridFilterOperator? SecondOperator = null,
    string? SecondValue = null);
