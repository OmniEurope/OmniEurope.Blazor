namespace OmniEurope.Blazor.Components;

public sealed record OmniDataGridFilter(string Key, OmniDataGridFilterOperator Operator, string Value);
