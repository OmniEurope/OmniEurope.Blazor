namespace OmniEurope.Blazor.Internal;

internal sealed record GridProjectionResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
