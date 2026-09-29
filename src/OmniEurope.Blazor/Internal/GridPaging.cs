using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The page and page size of a grid, kept by the grid and mirrored from the host's parameters when
/// it passes new values, with the pager and the paging summary that read them.
/// </summary>
internal sealed class GridPaging<TItem>(OmniDataGrid<TItem> grid)
{
    // Mirrored from the parameters when the host passes a new value: a component never writes its own parameters.
    private int _page = 1;
    private int? _receivedPage;
    private int _pageSize = 20;
    private int? _receivedPageSize;

    internal int Page => _page;

    internal int PageSize => _pageSize;

    /// <summary>Takes the page and page size the host passes when they are new values.</summary>
    internal void Mirror()
    {
        if (_receivedPage != grid.Page)
        {
            _receivedPage = grid.Page;
            _page = grid.Page;
        }

        if (_receivedPageSize != grid.PageSize)
        {
            _receivedPageSize = grid.PageSize;
            _pageSize = grid.PageSize;
        }
    }

    internal int PageCount => Math.Max(1, (int)Math.Ceiling(grid.View.TotalCount / (double)Math.Max(1, _pageSize)));

    /// <summary>
    /// The page actually shown. The local projection already clamps a page past the end to the last
    /// one; the pager and the summary read the same value, instead of announcing "page 5 of 1" over
    /// the rows of page 1 once the item list shrinks.
    /// </summary>
    internal int EffectivePage => Math.Clamp(_page, 1, PageCount);

    private bool ShowPager => grid.View.Paged && (PageCount > 1 || grid.AlwaysShowPager);

    internal bool ShowPagerTop => ShowPager && grid.PagerPosition is OmniDataGridPosition.Top or OmniDataGridPosition.TopAndBottom;

    internal bool ShowPagerBottom => ShowPager && grid.PagerPosition is OmniDataGridPosition.Bottom or OmniDataGridPosition.TopAndBottom;

    internal async Task ResetToFirstPageAsync()
    {
        if (_page == 1)
        {
            return;
        }

        _page = 1;
        await grid.PageChanged.InvokeAsync(1);
    }

    /// <summary>
    /// A local list that shrank under the current page (rows removed, another data set) is shown from
    /// its last page; the host's bound page is told, so it never disagrees with it.
    /// </summary>
    internal async Task ClampToLastPageAsync()
    {
        if (grid.Load is null && grid.View.Paged && _page > PageCount)
        {
            _page = PageCount;
            grid.View.InvalidateLocalProjection();
            await grid.PageChanged.InvokeAsync(_page);
        }
    }

    internal async Task ChangePageAsync(int page)
    {
        _page = page;
        grid.View.InvalidateLocalProjection();
        await grid.PageChanged.InvokeAsync(page);
        if (grid.Load is not null && !grid.View.Virtualized) await grid.View.ReloadAsync();
        grid.RebuildRenderSnapshot();
    }

    internal async Task ChangePageSizeAsync(int pageSize)
    {
        _pageSize = pageSize;
        grid.View.InvalidateLocalProjection();
        await grid.PageSizeChanged.InvokeAsync(pageSize);
        await ResetToFirstPageAsync();
        if (grid.Load is not null && !grid.View.Virtualized) await grid.View.ReloadAsync();
        grid.RebuildRenderSnapshot();
    }

    internal string PagingSummary()
    {
        var total = grid.View.TotalCount;
        var first = total == 0 ? 0 : ((EffectivePage - 1) * Math.Max(1, _pageSize)) + 1;
        var last = total == 0 ? 0 : Math.Min(total, first + Math.Max(1, _pageSize) - 1);
        return grid.Text("GridPagingSummary", first, last, total);
    }
}
