using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The grid every tile of the package sits in (recette R1-20): <see cref="OmniCard"/>,
/// <see cref="OmniSelectableCard"/>, <see cref="OmniStatTile"/> and <see cref="OmniNavTile"/> placed
/// directly inside have the same width (<see cref="Columns"/> equal columns), by default exactly the same
/// height (the height of the tallest tile, on every row; <see cref="EqualHeights"/> turns this off), and never
/// spill: a long word wraps or is cut inside its tile and the body of a card scrolls. A tile with
/// <c>DoubleWidth</c> spans two columns. From a window 40rem wide
/// the grid has at most two columns, from 64rem all of <see cref="Columns"/>, as the columns of an
/// <see cref="OmniRow"/>; narrower, one column, where a double-width tile is as wide as the others.
/// </summary>
/// <remarks>
/// That count follows the window, not the room the grid has: beside a sidebar four columns of a 1024px window
/// are narrow. <see cref="MinColumnWidth"/> makes the count follow the width of the grid itself instead: the
/// largest number of columns that keeps each one at least that wide, at most <see cref="Columns"/>, at least one,
/// at every window width.
/// </remarks>
public partial class OmniTileGrid
{
    /// <summary>The largest number of columns the grid takes.</summary>
    internal const int MaxColumns = 6;

    private ElementReference _grid;
    private IJSObjectReference? _module;
    private string? _appliedKey;
    private bool _disposed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The tiles of the grid. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The number of equal columns of a wide window, from 1 to 6; 4 by default. A grid of one column is a
    /// stack of tiles of the same height. With <see cref="MinColumnWidth"/> it is the largest number of
    /// columns, reached whenever the grid is wide enough for them.
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 4;

    /// <summary>The space between the tiles; <see cref="OmniSpacing.Medium"/> by default.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    /// <summary>
    /// Whether every tile of the grid has the same height, that of the tallest tile of the whole grid, on every
    /// row. True by default. False lets each row take the height of its own tallest tile and each tile keep its
    /// natural height, aligned to the top of its row, so a short tile (a fact tile of two rows) is not stretched
    /// to a tall neighbour. The widths are unchanged.
    /// </summary>
    [Parameter]
    public bool EqualHeights { get; set; } = true;

    /// <summary>
    /// The narrowest a column may be, as a positive CSS length (<c>18rem</c>, <c>240px</c>, <c>30ch</c>), so the
    /// number of columns follows the width of the grid itself, not that of the window: the largest number that
    /// keeps every column at least this wide, capped at <see cref="Columns"/>, never fewer than one, and never
    /// wider than the grid. A grid beside a sidebar then has fewer, wider tiles instead of four narrow ones. A
    /// tile with <c>DoubleWidth</c> spans two columns where the grid has two or more, else it is as wide as the
    /// others. Null, the default, keeps the columns of the window (two from 40rem, all of <see cref="Columns"/>
    /// from 64rem). The value is written by the interop script as a CSS custom property (never as a style
    /// attribute), which also watches the grid to tell when it has a single column; a grid rendered without
    /// interactivity shows one column, whatever the width.
    /// </summary>
    /// <exception cref="ArgumentException">The value is not a positive number followed by px, rem, em, ch, vw or %.</exception>
    [Parameter]
    public string? MinColumnWidth { get; set; }

    private string?[] TileGridClasses =>
    [
        "omni-tile-grid",
        $"omni-tile-grid--columns-{Columns}",
        $"omni-tile-grid--gap-{OmniSpacingTokens.Suffix(Gap)}",
        EqualHeights ? null : "omni-tile-grid--natural-heights",
        MinColumnWidth is null ? null : "omni-tile-grid--fluid"
    ];

    // What the script has to be told again when it changes: the length, and the class attribute (a new class
    // string replaces the one the script marked, and a grid whose columns change may keep the same size).
    private string? ScriptKey => MinColumnWidth is null
        ? null
        : string.Join('|', MinColumnWidth, Columns, Gap, EqualHeights);

    /// <summary>Rejects a number of columns outside 1 to 6 and a minimum width that is not a CSS length.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Columns"/> is not between 1 and 6.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Columns is < 1 or > MaxColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(Columns), Columns, "A tile grid has between 1 and 6 columns.");
        }

        if (MinColumnWidth is not null && !CssLength.IsWidth(MinColumnWidth, positive: true))
        {
            throw new ArgumentException($"'{MinColumnWidth}' is not a CSS length {nameof(OmniTileGrid)} accepts for {nameof(MinColumnWidth)} (a positive number followed by px, rem, em, ch, vw or %).", nameof(MinColumnWidth));
        }
    }

    /// <summary>
    /// Hands a <see cref="MinColumnWidth"/> that changed to the interop script, loaded only once a length has
    /// to be written; a grid that never sets it loads no script. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>The task of the write.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var key = ScriptKey;
        if (_disposed || string.Equals(key, _appliedKey, StringComparison.Ordinal))
        {
            return;
        }

        var minWidth = MinColumnWidth;
        try
        {
            // A grid disposed while the import was pending releases the module on arrival.
            _module ??= await OmniScriptModule.ImportAsync(JavaScript, OmniModules.Interop, () => _disposed);
            if (_module is not null && !_disposed)
            {
                await _module.InvokeVoidAsync("applyTileGridMinWidth", _grid, minWidth);
                _appliedKey = key;
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nothing left to size.
        }
    }

    /// <summary>Stops the script watching the grid and releases the interop script, if it was loaded.</summary>
    /// <returns>The task of the release.</returns>
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
                await _module.InvokeVoidAsync("applyTileGridMinWidth", _grid, null);
            }

            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is gone with the script.
        }
    }
}
