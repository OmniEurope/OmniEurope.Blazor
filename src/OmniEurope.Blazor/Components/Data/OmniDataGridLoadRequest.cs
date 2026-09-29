namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniDataGrid{TItem}"/> asks its <c>Load</c> callback for: one window of rows with
/// the sorts and filters currently applied. The grid builds a new request on each load; a newer load
/// cancels <see cref="CancellationToken"/> of the previous one.
/// </summary>
/// <param name="Page">One-based number of the requested page (a value below 1 reads as 1 in <see cref="Skip"/>).</param>
/// <param name="PageSize">Rows per page (a value below 1 reads as 1 in <see cref="Skip"/> and <see cref="Top"/>).</param>
/// <param name="Sorts">Sorts in priority order, the first being the primary one; empty when the rows are unsorted.</param>
/// <param name="Filters">Active filters of the columns still declared, all of which a row must match; empty when none.</param>
/// <param name="CancellationToken">Cancelled when a newer load replaces this one or the grid is disposed.</param>
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

    /// <summary>Column key of the primary sort, or <c>null</c> when <see cref="Sorts"/> is empty.</summary>
    public string? SortKey => Sorts.FirstOrDefault()?.Key;

    /// <summary>True when the primary sort is descending; false when it is ascending or there is no sort.</summary>
    public bool SortDescending => Sorts.FirstOrDefault()?.Descending == true;
}
