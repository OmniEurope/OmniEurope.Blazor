using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Sparse item cache for a virtualized grid backed by a remote loader. Rows are fetched in
/// fixed-size blocks around the viewport, stale responses are discarded, and blocks far from the
/// viewport are evicted so an endless scroll cannot grow the cache without bound.
/// </summary>
internal sealed class GridVirtualDataSource<TItem> : IAsyncDisposable
{
    private const int MaxCachedBlocks = 24;

    private readonly Dictionary<int, TItem> _items = [];
    private readonly HashSet<int> _loadedBlocks = [];
    private CancellationTokenSource? _cancellation;
    private int _generation;

    internal int TotalCount { get; private set; }

    internal bool Loading { get; private set; }

    internal Exception? Error { get; private set; }

    internal bool HasLoaded { get; private set; }

    internal int CachedItemCount => _items.Count;

    internal bool TryGet(int index, out TItem item) => _items.TryGetValue(index, out item!);

    internal void Reset()
    {
        _generation++;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        _items.Clear();
        _loadedBlocks.Clear();
        TotalCount = 0;
        Loading = false;
        Error = null;
        HasLoaded = false;
    }

    /// <summary>
    /// Loads every block covering <paramref name="start"/>..<paramref name="start"/> + <paramref name="count"/>
    /// that is not cached yet. Returns <c>true</c> when the cache changed and the grid must re-render.
    /// </summary>
    internal async Task<bool> EnsureRangeAsync(
        int start,
        int count,
        int blockSize,
        Func<int, int, CancellationToken, Task<OmniDataGridResult<TItem>>> loader)
    {
        var size = Math.Max(1, blockSize);
        var firstBlock = Math.Max(0, start) / size;
        var lastBlock = Math.Max(0, start + Math.Max(1, count) - 1) / size;
        var missing = Enumerable.Range(firstBlock, lastBlock - firstBlock + 1)
            .Where(block => !_loadedBlocks.Contains(block))
            .ToArray();
        if (missing.Length == 0)
        {
            return false;
        }

        _cancellation ??= new CancellationTokenSource();
        var token = _cancellation.Token;
        var generation = _generation;
        Loading = true;
        Error = null;
        var changed = false;
        try
        {
            foreach (var block in missing)
            {
                var result = await loader(block * size, size, token);
                if (generation != _generation || token.IsCancellationRequested)
                {
                    return changed;
                }

                TotalCount = result.TotalCount;
                HasLoaded = true;
                _loadedBlocks.Add(block);
                for (var offset = 0; offset < result.Items.Count; offset++)
                {
                    _items[(block * size) + offset] = result.Items[offset];
                }

                changed = true;
            }

            Evict(firstBlock, lastBlock, size);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (generation == _generation)
            {
                Error = exception;
                changed = true;
            }
        }
        finally
        {
            if (generation == _generation)
            {
                Loading = false;
            }
        }

        return changed;
    }

    private void Evict(int firstBlock, int lastBlock, int size)
    {
        if (_loadedBlocks.Count <= MaxCachedBlocks)
        {
            return;
        }

        var center = (firstBlock + lastBlock) / 2;
        var stale = _loadedBlocks
            .OrderByDescending(block => Math.Abs(block - center))
            .Take(_loadedBlocks.Count - MaxCachedBlocks)
            .Where(block => block < firstBlock || block > lastBlock)
            .ToArray();
        foreach (var block in stale)
        {
            _loadedBlocks.Remove(block);
            for (var offset = 0; offset < size; offset++)
            {
                _items.Remove((block * size) + offset);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        Reset();
        return ValueTask.CompletedTask;
    }
}
