namespace OmniEurope.Blazor.Components;

public sealed record OmniDataGridLoadRequest(
    int Page,
    int PageSize,
    IReadOnlyList<OmniDataGridSort> Sorts,
    IReadOnlyList<OmniDataGridFilter> Filters,
    CancellationToken CancellationToken)
{
    /// <summary>Rows to skip before the requested window, derived from <see cref="Page"/> and <see cref="PageSize"/>.</summary>
    public int Skip => (Math.Max(1, Page) - 1) * Math.Max(1, PageSize);

    /// <summary>Rows to return for this window.</summary>
    public int Top => Math.Max(1, PageSize);

    public string? SortKey => Sorts.FirstOrDefault()?.Key;
    public bool SortDescending => Sorts.FirstOrDefault()?.Descending == true;
}
