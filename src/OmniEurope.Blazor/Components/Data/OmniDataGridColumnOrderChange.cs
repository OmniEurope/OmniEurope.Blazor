namespace OmniEurope.Blazor.Components;

/// <summary>Reports a column the reader moved and the order of the columns that results.</summary>
/// <param name="Key">Key of the moved column.</param>
/// <param name="Order">The keys of every column in their new order, hidden columns included.</param>
public sealed record OmniDataGridColumnOrderChange(string Key, IReadOnlyList<string> Order);
