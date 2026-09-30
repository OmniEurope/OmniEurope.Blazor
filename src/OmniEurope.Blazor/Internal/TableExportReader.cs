using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>What an export read: the rows kept, and how many the source holds as far as the reading can tell.</summary>
/// <param name="Rows">The rows, at most the row limit.</param>
/// <param name="TotalCount">Rows the source announced, or the distinct rows read when it announced fewer than it returned.</param>
/// <param name="TotalIsLowerBound">True when the total was counted and the reading stopped on a full page: more rows may exist.</param>
internal sealed record TableExportRows<TItem>(IReadOnlyList<TItem> Rows, int TotalCount, bool TotalIsLowerBound);

/// <summary>
/// Reads the rows of an export page by page, for every format. Reading stops at a page shorter than
/// the page size, or once the rows announced by the first page (capped by the row limit) are read. A
/// source that returns more rows than it announced (a count of 0 when it does not know one) is counted
/// instead: reading goes on to a short page or the row limit, and the total is the distinct rows read.
/// </summary>
internal static class TableExportReader
{
    internal static async Task<TableExportRows<TItem>> ReadAsync<TItem>(
        Func<int, int, CancellationToken, Task<OmniDataGridResult<TItem>>> loadPage,
        int rowLimit,
        int pageSize,
        Func<TItem, object>? rowKey,
        CancellationToken cancellationToken)
    {
        pageSize = Math.Min(pageSize, rowLimit);
        var maxPages = (rowLimit + pageSize - 1) / pageSize;
        var rows = new List<TItem>();
        var seen = new HashSet<object>();
        var totalCount = 0;
        // A source whose pages hold more rows than it announced has announced no usable count (0 when
        // it does not know): the reader then counts the rows itself, reading on to a short page or
        // the row limit.
        var counting = false;
        var lastPageFull = false;
        for (var page = 1; page <= maxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await loadPage(page, pageSize, cancellationToken);
            if (page == 1)
            {
                totalCount = result.TotalCount;
            }

            foreach (var item in result.Items)
            {
                if (rowKey is null || seen.Add(rowKey(item)))
                {
                    rows.Add(item);
                }
            }

            counting |= rows.Count > totalCount;
            lastPageFull = result.Items.Count >= pageSize;
            if (!lastPageFull || rows.Count >= (counting ? rowLimit : Math.Min(totalCount, rowLimit)))
            {
                break;
            }
        }

        // A counted source: its total is the distinct rows read, including those past the limit. Reading
        // that stopped on a full page (the limit, not the end of the data) leaves rows unread, so that
        // total is only a lower bound.
        var totalIsLowerBound = counting && lastPageFull;
        if (counting)
        {
            totalCount = rows.Count;
        }

        if (rows.Count > rowLimit)
        {
            rows.RemoveRange(rowLimit, rows.Count - rowLimit);
        }

        return new TableExportRows<TItem>(rows, totalCount, totalIsLowerBound);
    }
}
