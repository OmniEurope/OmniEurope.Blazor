namespace OmniEurope.Blazor.Components;

public sealed record OmniDataGridLoadRequest(
    int Page,
    int PageSize,
    IReadOnlyList<OmniDataGridSort> Sorts,
    IReadOnlyList<OmniDataGridFilter> Filters,
    CancellationToken CancellationToken)
{
    public string? SortKey => Sorts.FirstOrDefault()?.Key;
    public bool SortDescending => Sorts.FirstOrDefault()?.Descending == true;
}
