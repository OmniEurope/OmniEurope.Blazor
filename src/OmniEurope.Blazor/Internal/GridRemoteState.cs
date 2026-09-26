using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class GridRemoteState<TItem> : IAsyncDisposable
{
    private CancellationTokenSource? _cancellation;
    private int _generation;

    internal IReadOnlyList<TItem> Items { get; private set; } = Array.Empty<TItem>();
    internal int TotalCount { get; private set; }
    internal bool Loading { get; private set; }
    internal Exception? Error { get; private set; }
    internal bool HasLoaded { get; private set; }

    /// <summary>
    /// Loads the rows. A <paramref name="quiet"/> load keeps the rows held and raises no loading
    /// state while it runs: the new rows replace the old ones when they arrive, which is what a
    /// live refresh wants rather than a table blanking under a loader.
    /// </summary>
    internal async Task LoadAsync(Func<CancellationToken, Task<OmniDataGridResult<TItem>>> loader, bool quiet = false)
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var generation = ++_generation;
        Loading = !quiet;
        Error = null;
        try
        {
            // WaitAsync: a cancelled load stops being awaited even when the loader ignores its token.
            var result = await loader(token).WaitAsync(token);
            if (generation == _generation)
            {
                Items = result.Items;
                TotalCount = result.TotalCount;
                HasLoaded = true;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (generation == _generation)
            {
                Error = exception;
            }
        }
        finally
        {
            if (generation == _generation)
            {
                Loading = false;
            }
        }
    }

    internal void Reset()
    {
        _generation++;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        Items = Array.Empty<TItem>();
        TotalCount = 0;
        Loading = false;
        Error = null;
        HasLoaded = false;
    }

    public ValueTask DisposeAsync()
    {
        Reset();
        return ValueTask.CompletedTask;
    }
}
