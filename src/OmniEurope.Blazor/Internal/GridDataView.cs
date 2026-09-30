using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The rows a grid shows and how it gets them: the local projection of its items, the remote page of
/// its loader or the blocks of a remote virtualized grid, with the loading, failure and empty states
/// that follow from them.
/// </summary>
internal sealed class GridDataView<TItem>(OmniDataGrid<TItem> grid) : IAsyncDisposable
{
    private readonly GridRemoteState<TItem> _remote = new();
    private readonly GridVirtualDataSource<TItem> _virtualSource = new();
    private GridProjectionResult<TItem>? _localProjection;
    private IReadOnlyList<TItem>? _virtualLocalItems;
    private IReadOnlyList<TItem>? _observedItems;
    private Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<TItem>>>? _observedLoader;
    private bool _virtualBootstrapped;
    // The columns register while the grid renders, and a saved state is restored before the first
    // parameters pass: the first request waits for both, so it carries the defaults and the saved state.
    private bool _columnsRendered;
    private bool _parametersObserved;

    internal bool Virtualized => grid.ScrollMode == OmniDataGridScrollMode.Virtual;
    internal bool Paged => grid.ScrollMode == OmniDataGridScrollMode.Paged;

    /// <summary>
    /// A local virtualized grid whose body is not one row per item: group header rows and detail rows
    /// sit between the items. The virtual window then indexes "slots", an item row together with the
    /// group headers opening above it and its detail row, and measures each slot as a whole.
    /// </summary>
    internal bool StructuredVirtual => Virtualized && grid.Load is null
        && (grid.DetailTemplate is not null || grid.Grouping.ActiveGroups.Count > 0);

    internal IReadOnlyList<TItem> VirtualLocalItems => _virtualLocalItems ??= GridProjection<TItem>.Create(
        grid.Items, grid.ColumnSet.EffectiveColumns, grid.Query.Filters, grid.Query.Sorts,
        grid.CaseSensitiveFilters, grid.IgnoreDiacritics, 1, int.MaxValue).Items;

    private GridProjectionResult<TItem> LocalView => _localProjection ??= GridProjection<TItem>.Create(
        grid.Items, grid.ColumnSet.EffectiveColumns, grid.Query.Filters, grid.Query.Sorts,
        grid.CaseSensitiveFilters, grid.IgnoreDiacritics, grid.Paging.Page, Paged ? grid.Paging.PageSize : int.MaxValue);

    internal IReadOnlyList<TItem> VisibleItems => Virtualized
        ? Array.Empty<TItem>()
        : grid.Load is null ? LocalView.Items : _remote.Items;

    internal int TotalCount => grid.Count
        ?? (Virtualized
            ? grid.Load is null ? VirtualLocalItems.Count : _virtualSource.TotalCount
            : grid.Load is null ? LocalView.TotalCount : _remote.TotalCount);

    internal int BlockSize => grid.VirtualBlockSize > 0 ? grid.VirtualBlockSize : Math.Max(1, grid.Paging.PageSize);

    /// <summary>The columns have rendered and the saved state is read: the first request can go, or has gone.</summary>
    internal bool FirstRequestReady => _columnsRendered && _parametersObserved;

    /// <summary>A loader is set and its first request waits for the columns and the saved state.</summary>
    private bool InitialLoadPending => !FirstRequestReady && grid.Load is not null;

    internal bool Loading => grid.Busy || InitialLoadPending || _remote.Loading || (Virtualized && _virtualSource.Loading && _virtualSource.CachedItemCount == 0);

    // The bar follows every load: the grid's own requests, also the blocks a virtualized grid fetches while
    // rows are already shown, and a host load signalled by Busy. A live refresh keeps the rows and shows no
    // loading state, so it shows no bar either.
    internal bool ShowsLoadingBar => grid.ShowLoadingBar
        && (grid.Busy || InitialLoadPending || _remote.Loading || (Virtualized && _virtualSource.Loading));

    internal Exception? Failure => Virtualized ? _virtualSource.Error : _remote.Error;

    internal string EmptyMessage => grid.Query.HasActiveFilter()
        ? grid.Text("GridEmptyFiltered")
        : string.IsNullOrWhiteSpace(grid.EmptyText) ? grid.Text("GridEmpty") : grid.EmptyText;

    /// <summary>
    /// The loading row that replaces the rows: only when the bar is turned off. A virtualized grid keeps
    /// the rows it holds and shows it only while it has none.
    /// </summary>
    internal bool ShowLoadingRow => !grid.ShowLoadingBar && (Virtualized ? Loading && TotalCount == 0 : Loading);

    /// <summary>
    /// A grid with no row yet, loading under the bar: its body holds the loading content or a status
    /// read to screen readers only, never the empty message, which would be false until the rows arrive.
    /// </summary>
    internal bool ShowPendingRow => grid.ShowLoadingBar && Loading && IsEmpty;

    internal bool IsEmpty => Virtualized ? TotalCount == 0 : VisibleItems.Count == 0;

    /// <summary>
    /// Every item the grid holds in memory, whatever part of it is on screen: the whole filtered and
    /// sorted local set (all pages, all virtual rows), the external or remote page, or the blocks a
    /// remote virtualized grid has fetched so far.
    /// </summary>
    internal IEnumerable<TItem> LoadedItems() => grid.Load is null
        ? VirtualLocalItems
        : Virtualized ? _virtualSource.CachedItems : _remote.Items;

    internal void InvalidateLocalProjection()
    {
        _localProjection = null;
        _virtualLocalItems = null;
        grid.Rows.ForgetSlots();
    }

    /// <summary>Takes a new item list from the host, comparing it with a refresh baseline when one is held.</summary>
    internal void ObserveItems()
    {
        if (!ReferenceEquals(_observedItems, grid.Items))
        {
            _observedItems = grid.Items;
            grid.Highlight.CompareWithBaseline(grid.Items);
        }
    }

    /// <summary>Follows the host's loader: a new one restarts the rows, a first one loads them.</summary>
    internal async Task ObserveLoaderAsync()
    {
        _parametersObserved = true;
        if (grid.Load is null)
        {
            if (_observedLoader is not null)
            {
                _remote.Reset();
                _virtualSource.Reset();
                _observedLoader = null;
                _virtualBootstrapped = false;
            }

            return;
        }

        // Delegate equality (same method, same target): a parent binding a method group passes a
        // new delegate on every render, which is not a new loader.
        var loaderChanged = !Equals(_observedLoader, grid.Load);
        _observedLoader = grid.Load;
        if (Virtualized)
        {
            if (loaderChanged)
            {
                _virtualSource.Reset();
                _virtualBootstrapped = false;
            }
        }
        else if (FirstRequestReady && (loaderChanged || (!_remote.HasLoaded && !_remote.Loading && _remote.Error is null)))
        {
            // A new loader replaces the load in progress, or the failed one, at once: the remote
            // state cancels the previous load and ignores its late answer.
            await ReloadAsync();
        }
    }

    /// <summary>
    /// Sends the first request once the grid has rendered, its columns registered with their default
    /// filters and sorts; a request sent before that did not carry them.
    /// </summary>
    internal async Task ColumnsRenderedAsync()
    {
        if (_columnsRendered)
        {
            return;
        }

        _columnsRendered = true;
        // A state still being restored: the first parameters pass that follows sends the request.
        if (grid.Load is null || !_parametersObserved)
        {
            return;
        }

        if (Virtualized)
        {
            await BootstrapVirtualizationAsync();
        }
        else if (!_remote.HasLoaded && !_remote.Loading && _remote.Error is null)
        {
            await ReloadAsync();
        }
    }

    internal bool TryGetVirtualItem(int index, out TItem item)
    {
        if (grid.Load is null)
        {
            var items = VirtualLocalItems;
            if (index >= 0 && index < items.Count)
            {
                item = items[index];
                return true;
            }

            item = default!;
            return false;
        }

        return _virtualSource.TryGet(index, out item);
    }

    internal async Task BootstrapVirtualizationAsync()
    {
        if (grid.Load is null || _virtualBootstrapped || !FirstRequestReady)
        {
            return;
        }

        _virtualBootstrapped = true;
        await EnsureVirtualDataAsync();
    }

    internal async Task EnsureVirtualDataAsync()
    {
        if (grid.Load is null || !FirstRequestReady)
        {
            return;
        }

        var range = grid.Virtual.Range;
        var count = Math.Max(1, range.Count);
        var failedBefore = _virtualSource.Error;
        var pending = _virtualSource.EnsureRangeAsync(range.StartIndex, count, BlockSize, LoadWindowAsync);
        // A block still on its way: draw the loading bar now, not only once the rows are in.
        var showedBar = !pending.IsCompleted && ShowsLoadingBar;
        if (showedBar)
        {
            grid.Render();
        }

        var changed = await pending;
        await ReportLoadErrorAsync(failedBefore, _virtualSource.Error);
        if (changed || showedBar)
        {
            grid.Virtual.Sync();
            grid.Render();
        }
    }

    private Task<OmniDataGridResult<TItem>> LoadWindowAsync(int skip, int take, CancellationToken token)
    {
        var page = (skip / Math.Max(1, take)) + 1;
        return grid.Load!(new OmniDataGridLoadRequest(page, take, grid.Query.CurrentSorts(), grid.Query.CurrentFilters(), token));
    }

    /// <summary>The whole reaction to a sort or filter change: first page, new rows, saved state.</summary>
    internal async Task QueryChangedAsync()
    {
        await grid.Paging.ResetToFirstPageAsync();
        InvalidateLocalProjection();
        await RefreshAfterQueryChangeAsync();
        await grid.Persistence.SaveAsync();
    }

    /// <summary>Restarts the row set after a sort or filter changed, discarding measurements and cached rows.</summary>
    internal async Task RefreshAfterQueryChangeAsync()
    {
        if (Virtualized)
        {
            grid.Virtual.ResetScroll();
            if (grid.Load is not null)
            {
                _virtualSource.Reset();
                _virtualBootstrapped = false;
            }

            grid.RebuildRenderSnapshot();
            await grid.Script.ScrollToOffsetAsync(0d);
            await BootstrapVirtualizationAsync();
            return;
        }

        if (grid.Load is not null)
        {
            await ReloadAsync();
        }

        grid.RebuildRenderSnapshot();
    }

    /// <summary>
    /// Fetches the rows of the loader again from the start of the current query, with the loading
    /// state; a virtualized grid starts again from its first block.
    /// </summary>
    internal async Task ReloadAsync()
    {
        if (grid.Load is null) return;
        if (Virtualized)
        {
            _virtualSource.Reset();
            grid.Virtual.ResetMeasurements();
            _virtualBootstrapped = false;
            grid.RebuildRenderSnapshot();
            await BootstrapVirtualizationAsync();
            return;
        }

        var sorts = grid.Query.CurrentSorts();
        var filters = grid.Query.CurrentFilters();
        var loader = grid.Load;
        var page = grid.Paging.Page;
        var pageSize = grid.Paging.PageSize;
        var load = _remote.LoadAsync(token => loader(new OmniDataGridLoadRequest(page, pageSize, sorts, filters, token)));
        // A reload the host starts from its own code (a timer, another component) is not wrapped in an
        // event of the grid, which would render it: the loading state is drawn here, then the result.
        if (!load.IsCompleted)
        {
            grid.Render();
        }

        await load;
        await ReportLoadErrorAsync(null, _remote.Error);
        grid.RebuildRenderSnapshot();
        grid.Render();
    }

    /// <summary>
    /// Fetches the rows again without leaving them, for live data: no loading state, the scroll
    /// position and the page stay, and rows that were not held before are marked as new.
    /// </summary>
    internal async Task RefreshAsync()
    {
        if (grid.Load is null)
        {
            grid.Highlight.HoldBaseline(grid.Items);
            return;
        }

        if (Virtualized)
        {
            var range = grid.Virtual.Range;
            var before = grid.Highlight.HeldKeys(_virtualSource.CachedItems);
            var failedBefore = _virtualSource.Error;
            var changed = await _virtualSource.RefreshAsync(range.StartIndex, Math.Max(1, range.Count), BlockSize, LoadWindowAsync);
            await ReportLoadErrorAsync(failedBefore, _virtualSource.Error);
            if (!changed)
            {
                return;
            }

            grid.Highlight.MarkNewRows(before, _virtualSource.CachedItems);
            grid.Virtual.Sync();
            await EnsureVirtualDataAsync();
            grid.Render();
            return;
        }

        var held = grid.Highlight.HeldKeys(_remote.Items);
        var sorts = grid.Query.CurrentSorts();
        var filters = grid.Query.CurrentFilters();
        var loader = grid.Load;
        var page = grid.Paging.Page;
        var pageSize = grid.Paging.PageSize;
        await _remote.LoadAsync(token => loader(new OmniDataGridLoadRequest(page, pageSize, sorts, filters, token)), quiet: true);
        await ReportLoadErrorAsync(null, _remote.Error);
        grid.Highlight.MarkNewRows(held, _remote.Items);
        grid.RebuildRenderSnapshot();
        grid.Render();
    }

    /// <summary>
    /// Reports a load failure to the host once: <paramref name="after"/> is the error the load left,
    /// <paramref name="before"/> the one held before it, so an error already reported is not reported
    /// again by a later pass that did not load.
    /// </summary>
    private Task ReportLoadErrorAsync(Exception? before, Exception? after) =>
        after is not null && !ReferenceEquals(before, after) ? grid.OnLoadError.InvokeAsync(after) : Task.CompletedTask;

    /// <summary>Cancels the loads still running; a late answer is then ignored.</summary>
    internal void CancelLoads()
    {
        _remote.Reset();
        _virtualSource.Reset();
    }

    public async ValueTask DisposeAsync()
    {
        await _virtualSource.DisposeAsync();
        await _remote.DisposeAsync();
    }
}
