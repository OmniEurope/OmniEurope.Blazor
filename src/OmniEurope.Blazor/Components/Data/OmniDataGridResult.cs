namespace OmniEurope.Blazor.Components;

/// <summary>What an <see cref="OmniDataGrid{TItem}"/> <c>Load</c> callback returns for one <see cref="OmniDataGridLoadRequest"/>.</summary>
/// <typeparam name="TItem">Type of the grid rows.</typeparam>
/// <param name="Items">The rows of the requested window only, already sorted and filtered by the loader.</param>
/// <param name="TotalCount">
/// Number of rows matching the filters across all pages; it sizes the pager and the virtualized scroll,
/// unless the grid's <c>Count</c> parameter is set.
/// </param>
public sealed record OmniDataGridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
