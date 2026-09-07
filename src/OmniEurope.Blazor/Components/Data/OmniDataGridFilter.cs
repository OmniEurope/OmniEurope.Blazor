namespace OmniEurope.Blazor.Components;

/// <summary>One filter condition sent to a remote loader, optionally joined with a second one.</summary>
public sealed record OmniDataGridFilter(
    string Key,
    OmniDataGridFilterOperator Operator,
    string Value,
    OmniDataGridLogicalOperator LogicalOperator = OmniDataGridLogicalOperator.And,
    OmniDataGridFilterOperator? SecondOperator = null,
    string? SecondValue = null);
