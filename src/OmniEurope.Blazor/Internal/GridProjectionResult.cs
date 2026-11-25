namespace OmniEurope.Blazor.Components;

internal sealed record GridProjectionResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
