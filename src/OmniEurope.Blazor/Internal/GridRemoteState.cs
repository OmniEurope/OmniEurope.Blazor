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

    internal async Task LoadAsync(Func<CancellationToken, Task<OmniDataGridResult<TItem>>> loader)
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var generation = ++_generation;
        Loading = true;
        Error = null;
        try
        {
            var result = await loader(token);
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
