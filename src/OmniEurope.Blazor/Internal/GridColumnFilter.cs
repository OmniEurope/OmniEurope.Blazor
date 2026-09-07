using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>Filter state held by the grid for one column: up to two conditions joined by AND or OR.</summary>
internal sealed record GridColumnFilter(
    OmniDataGridFilterOperator Operator,
    string Value,
    OmniDataGridLogicalOperator LogicalOperator,
    OmniDataGridFilterOperator SecondOperator,
    string SecondValue)
{
    internal static GridColumnFilter Empty { get; } = new(
        OmniDataGridFilterOperator.Contains,
        string.Empty,
        OmniDataGridLogicalOperator.And,
        OmniDataGridFilterOperator.Contains,
        string.Empty);

    /// <summary>An operator such as <c>IsNull</c> filters without a typed value.</summary>
    internal static bool IsValueless(OmniDataGridFilterOperator candidate) => candidate
        is OmniDataGridFilterOperator.IsNull
        or OmniDataGridFilterOperator.IsNotNull
        or OmniDataGridFilterOperator.IsEmpty
        or OmniDataGridFilterOperator.IsNotEmpty;

    internal bool HasFirst => IsValueless(Operator) || !string.IsNullOrWhiteSpace(Value);

    internal bool HasSecond => IsValueless(SecondOperator) || !string.IsNullOrWhiteSpace(SecondValue);

    internal bool IsActive => HasFirst || HasSecond;
}
