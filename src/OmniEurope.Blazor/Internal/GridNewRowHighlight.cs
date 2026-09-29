using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The new-row mark of a live grid: the keys held before a refresh, the rows the refresh brought in
/// and the deadline after which each of them stops reading as new.
/// </summary>
internal sealed class GridNewRowHighlight<TItem>(OmniDataGrid<TItem> grid) : IAsyncDisposable
{
    private readonly Dictionary<object, long> _newRowKeys = [];
    private readonly CancellationTokenSource _highlightLifetime = new();
    // Keys held when a refresh of host-supplied rows was asked for; the next Items change is compared to them.
    private HashSet<object>? _refreshBaseline;

    private bool HighlightsNewRows => grid.NewRowHighlight is { } duration && duration > TimeSpan.Zero
        && grid.KeyOf is not null;

    /// <summary>The keys of <paramref name="items"/>, or null when the grid marks no new row.</summary>
    internal HashSet<object>? HeldKeys(IEnumerable<TItem> items) =>
        HighlightsNewRows ? items.Select(grid.ItemKey).ToHashSet() : null;

    /// <summary>Remembers the rows held now, to compare the next item list the host hands with them.</summary>
    internal void HoldBaseline(IEnumerable<TItem> items) => _refreshBaseline = HeldKeys(items);

    /// <summary>Marks the rows of a new item list that the held baseline did not have, then drops it.</summary>
    internal void CompareWithBaseline(IEnumerable<TItem> items)
    {
        if (_refreshBaseline is { } baseline)
        {
            _refreshBaseline = null;
            MarkNewRows(baseline, items);
        }
    }

    /// <summary>Marks the rows of <paramref name="now"/> whose key <paramref name="before"/> did not hold.</summary>
    internal void MarkNewRows(HashSet<object>? before, IEnumerable<TItem> now)
    {
        if (before is null || grid.NewRowHighlight is not { } duration)
        {
            return;
        }

        var until = Environment.TickCount64 + (long)duration.TotalMilliseconds;
        var marked = false;
        foreach (var key in now.Select(grid.ItemKey).Where(key => !before.Contains(key)))
        {
            _newRowKeys[key] = until;
            marked = true;
        }

        if (marked)
        {
            _ = ExpireNewRowsAsync(until);
        }
    }

    /// <summary>
    /// Clears the marks set with <paramref name="until"/> once it has passed. The system tick is
    /// coarser than a short delay, so the wait repeats until the clock agrees; a row marked again
    /// since holds a later deadline and is left to its own call.
    /// </summary>
    private async Task ExpireNewRowsAsync(long until)
    {
        try
        {
            long remaining;
            while ((remaining = until - Environment.TickCount64) > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(remaining), _highlightLifetime.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        foreach (var key in _newRowKeys.Where(pair => pair.Value <= until).Select(pair => pair.Key).ToArray())
        {
            _newRowKeys.Remove(key);
        }

        await grid.RenderLaterAsync();
    }

    internal bool IsNewRow(TItem item) => _newRowKeys.Count > 0 && _newRowKeys.ContainsKey(grid.ItemKey(item));

    public async ValueTask DisposeAsync()
    {
        await _highlightLifetime.CancelAsync();
        _highlightLifetime.Dispose();
    }
}
