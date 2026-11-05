namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="Components.OmniTileGrid"/> shares with the tiles it holds and with the interop script. The
/// grid cascades it; a tile that is <c>DoubleWidth</c> tells it so. A double tile spans two columns only where the
/// grid has two, which the stylesheet cannot tell, so a grid whose columns follow its own width
/// (<c>MinColumnWidth</c> or <c>UniformSize</c>) has the script watch its columns while it holds a double tile. A
/// grid with neither, and a grid with no double tile and no <c>MinColumnWidth</c>, loads no script at all. A lost
/// circuit is ignored. The tiles that fit their title tell it the length of their title too, and the grid renders
/// the class of the longest, so that every one of them sizes its title alike.
/// </summary>
internal sealed class OmniTileGridScope(IJSRuntime javaScript, Action refresh) : IAsyncDisposable
{
    private ElementReference _grid;
    private IJSObjectReference? _module;
    private string? _appliedKey;
    private readonly Dictionary<object, int> _fitTitles = new(ReferenceEqualityComparer.Instance);
    private int _doubleTiles;
    private bool _watchable;
    private bool _disposed;

    /// <summary>Whether a tile of the grid is <c>DoubleWidth</c>.</summary>
    internal bool HasDoubleTile => _doubleTiles > 0;

    /// <summary>
    /// The title length, in characters, that every <c>FitTitle</c> tile of the grid sizes its title for: the
    /// longest title of those tiles, rounded up to a step of the stylesheet. Zero while no such tile is in the grid.
    /// </summary>
    internal int FitStep { get; private set; }

    /// <summary>
    /// Keeps what a tile has told its grid in step with its parameters: counts the tile once while it is double
    /// width inside a grid, and stops counting it when it is not, or when the grid around it changes.
    /// </summary>
    /// <param name="tracked">The grid this tile is counted in, null while it is not counted.</param>
    /// <param name="scope">The grid around the tile, null outside any.</param>
    /// <param name="doubleWidth">The tile's <c>DoubleWidth</c>.</param>
    internal static void Track(ref OmniTileGridScope? tracked, OmniTileGridScope? scope, bool doubleWidth)
    {
        var wanted = doubleWidth ? scope : null;
        if (ReferenceEquals(tracked, wanted))
        {
            return;
        }

        tracked?.Remove();
        tracked = wanted;
        wanted?.Add();
    }

    /// <summary>
    /// Tells the script what the grid needs, loading it only once something has to be written: the minimum column
    /// width of the grid, and the watch of its columns while it holds a double tile. Does nothing again until
    /// one of them, or the class attribute (a new one replaces the mark the script put on it), changes.
    /// </summary>
    /// <param name="grid">The grid's element.</param>
    /// <param name="minWidth">The grid's <c>MinColumnWidth</c>, null for the token's width.</param>
    /// <param name="columnsFollowGrid">Whether the columns follow the grid's own width now (<c>MinColumnWidth</c> or <c>UniformSize</c>), the only grids whose double tiles the script has to watch.</param>
    /// <param name="classes">The class attribute the grid renders.</param>
    /// <returns>A task that completes once the script knows.</returns>
    internal async Task SyncAsync(ElementReference grid, string? minWidth, bool columnsFollowGrid, string classes)
    {
        _watchable = columnsFollowGrid;
        var watch = columnsFollowGrid && HasDoubleTile;
        var key = minWidth is null && !watch ? null : string.Join('|', minWidth, watch, classes);
        if (_disposed || string.Equals(key, _appliedKey, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            // A grid disposed while the import was pending releases the module on arrival.
            _module ??= await OmniScriptModule.ImportAsync(javaScript, OmniModules.Interop, () => _disposed);
            if (_module is not null && !_disposed)
            {
                _grid = grid;
                await _module.InvokeVoidAsync("applyTileGrid", grid, minWidth, watch);
                _appliedKey = key;
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nothing left to size.
        }
    }

    /// <summary>Stops the script watching the grid and releases the interop script, if it was loaded.</summary>
    /// <returns>A task that completes once the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
        if (_module is null)
        {
            return;
        }

        try
        {
            if (_appliedKey is not null)
            {
                await _module.InvokeVoidAsync("applyTileGrid", _grid, null, false);
            }

            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is gone with the script.
        }
    }

    /// <summary>
    /// Records the title length of a <c>FitTitle</c> tile of the grid, or updates it. The grid re-renders only when
    /// the size class its tiles share changes, so a tile that tells the same length again costs nothing.
    /// </summary>
    /// <param name="tile">The tile, compared by reference.</param>
    /// <param name="titleLength">The length of the tile's title.</param>
    internal void SetFitTitle(object tile, int titleLength)
    {
        _fitTitles[tile] = titleLength;
        RefreshFitStep();
    }

    /// <summary>Forgets a tile that is no longer a <c>FitTitle</c> tile of the grid, or is gone.</summary>
    /// <param name="tile">The tile, compared by reference.</param>
    internal void ClearFitTitle(object tile)
    {
        if (_fitTitles.Remove(tile))
        {
            RefreshFitStep();
        }
    }

    // The step is the longest title, rounded up to a step of the stylesheet (see StepFor).
    private void RefreshFitStep()
    {
        var longest = 0;
        foreach (var length in _fitTitles.Values)
        {
            longest = Math.Max(longest, length);
        }

        var step = _fitTitles.Count == 0 ? 0 : StepFor(longest);
        if (step == FitStep)
        {
            return;
        }

        FitStep = step;
        if (!_disposed)
        {
            refresh();
        }
    }

    private static readonly int[] CoarseFitSteps = [40, 48];

    /// <summary>
    /// The step of the stylesheet (<c>omni-tile-grid--fit-N</c>) that holds a title of this many characters: the
    /// exact length from 1 to 32, then 40 and 48 for the rare longer ones, 48 for anything beyond. A length of
    /// zero or less reads as 1.
    /// </summary>
    internal static int StepFor(int length)
    {
        if (length <= 32)
        {
            return Math.Max(length, 1);
        }

        foreach (var step in CoarseFitSteps)
        {
            if (length <= step)
            {
                return step;
            }
        }

        return CoarseFitSteps[^1];
    }

    private void Add()
    {
        if (_doubleTiles++ == 0)
        {
            Changed();
        }
    }

    private void Remove()
    {
        if (--_doubleTiles == 0)
        {
            Changed();
        }
    }

    // The first double tile, or the last one leaving, changes what the script has to do, but only for a grid
    // whose columns follow its own width: any other grid has nothing to ask. The grid sees it at its next
    // render when it is rendering anyway; a tile that changes alone, inside a component of its own, asks for that
    // render here. Before the grid has rendered once, or after it is gone, there is nothing to ask.
    private void Changed()
    {
        if (_watchable && !_disposed)
        {
            refresh();
        }
    }
}
