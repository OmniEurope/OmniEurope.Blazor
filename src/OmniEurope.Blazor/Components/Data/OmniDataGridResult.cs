namespace OmniEurope.Blazor.Components;

public sealed record OmniDataGridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
