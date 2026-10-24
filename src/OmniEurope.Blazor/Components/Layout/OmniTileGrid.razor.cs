using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The grid every tile of the package sits in (recette R1-20): <see cref="OmniCard"/>,
/// <see cref="OmniSelectableCard"/>, <see cref="OmniStatTile"/>, <see cref="OmniFactTile"/> and
/// <see cref="OmniNavTile"/> placed directly inside have the same width (<see cref="Columns"/> equal columns),
/// by default exactly the same height (the height of the tallest tile, on every row; <see cref="EqualHeights"/>
/// turns this off), and never spill: a long word wraps or is cut inside its tile and the body of a card
/// scrolls. A tile with <c>DoubleWidth</c> spans two columns. From a window 40rem wide the grid has at most two
/// columns, from 64rem all of <see cref="Columns"/>, as the columns of an <see cref="OmniRow"/>; narrower, one
/// column, where a double-width tile is as wide as the others.
/// </summary>
/// <remarks>
/// That count follows the window, not the room the grid has: beside a sidebar four columns of a 1024px window
/// are narrow. Two options make the count follow the width of the grid itself instead.
/// <see cref="MinColumnWidth"/> sets the narrowest a column may be: the largest number of columns that keeps each
/// one at least that wide, at most <see cref="Columns"/>, at least one. <see cref="UniformSize"/> applies the
/// common tile size rule of the application: the same count, from the shared token
/// <c>--omni-tile-min-width</c> (18rem by default, or <see cref="MinColumnWidth"/>), and every tile as tall as the
/// tallest of the grid and never shorter than the density's tile height (<c>--omni-tile-height</c>). In both
/// options a <c>DoubleWidth</c> tile spans two columns only where the grid has two or more. A grid with neither
/// loads no script and renders as it always has.
/// </remarks>
public partial class OmniTileGrid
{
    /// <summary>The largest number of columns the grid takes.</summary>
    internal const int MaxColumns = 6;

    private ElementReference _grid;
    private OmniTileGridScope? _scope;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The tiles of the grid. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The number of equal columns of a wide window, from 1 to 6; 4 by default. A grid of one column is a
    /// stack of tiles of the same height. With <see cref="MinColumnWidth"/> or <see cref="UniformSize"/> it is
    /// the largest number of columns, reached whenever the grid is wide enough for them.
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
    /// to a tall neighbour. The widths are unchanged. It contradicts <see cref="UniformSize"/>, which is the
    /// rule of equal heights.
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
    /// from 64rem), unless <see cref="UniformSize"/> is set, which then reads the shared token
    /// <c>--omni-tile-min-width</c>. The value is written by the interop script as a CSS custom property (never
    /// as a style attribute), which also watches the grid to tell when it has a single column; a grid rendered
    /// without interactivity shows one column, whatever the width (with <see cref="UniformSize"/>, the columns
    /// of the token).
    /// </summary>
    /// <exception cref="ArgumentException">The value is not a positive number followed by px, rem, em, ch, vw or %.</exception>
    [Parameter]
    public string? MinColumnWidth { get; set; }

    /// <summary>
    /// Applies the common tile size rule of the application to this grid: false, the default, changes nothing.
    /// True makes every tile of the grid of roughly the same width and exactly the same height, whatever it
    /// holds. The columns follow the width of the grid itself, never that of the window: the largest number that
    /// keeps each one at least <c>--omni-tile-min-width</c> wide (18rem by default, a token a host retunes once;
    /// <see cref="MinColumnWidth"/> replaces it for this grid), at most <see cref="Columns"/>, at least one. Every
    /// row is as tall as the tallest tile of the grid and never shorter than <c>--omni-tile-height</c>, a
    /// token of the density (10rem comfortable). A <c>DoubleWidth</c> tile is two tiles side by side where the
    /// grid has two columns or more, else one. The first paint needs no script, but a grid holding a double
    /// tile loads the interop script to tell how many columns it has.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <see cref="EqualHeights"/> is false: a uniform grid has one height for all its tiles.
    /// </exception>
    [Parameter]
    public bool UniformSize { get; set; }

    private string?[] TileGridClasses =>
    [
        "omni-tile-grid",
        $"omni-tile-grid--columns-{Columns}",
        $"omni-tile-grid--gap-{OmniSpacingTokens.Suffix(Gap)}",
        EqualHeights ? null : "omni-tile-grid--natural-heights",
        MinColumnWidth is null ? null : "omni-tile-grid--fluid",
        UniformSize ? "omni-tile-grid--uniform" : null
    ];

    // The class attribute exactly as rendered: a new string replaces the one the script marked, so it is part of
    // what the script has to be told again (the host's own Class too).
    private string GridClasses => Css(TileGridClasses);

    /// <summary>Creates the share of the grid with its tiles and with the interop script.</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _scope = new OmniTileGridScope(JavaScript, () => _ = InvokeAsync(StateHasChanged));
    }

    /// <summary>Rejects a number of columns outside 1 to 6, a minimum width that is not a CSS length, and a uniform grid of natural heights.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Columns"/> is not between 1 and 6.</exception>
    /// <exception cref="ArgumentException"><see cref="MinColumnWidth"/> is not a positive CSS width, or <see cref="UniformSize"/> comes with <c>EqualHeights="false"</c>.</exception>
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

        if (UniformSize && !EqualHeights)
        {
            throw new ArgumentException($"{nameof(OmniTileGrid)} takes either {nameof(UniformSize)} (every tile the same height) or {nameof(EqualHeights)}=\"false\" (every tile its natural height), not both.", nameof(UniformSize));
        }
    }

    /// <summary>
    /// Hands the interop script what it has to know once the grid is drawn: a <see cref="MinColumnWidth"/>, and
    /// the watch of the columns while a tile is <c>DoubleWidth</c> in a grid whose columns follow its own width.
    /// The script is loaded only then; a grid with neither option loads none. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>The task of the write.</returns>
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _scope!.SyncAsync(_grid, MinColumnWidth, columnsFollowGrid: UniformSize || MinColumnWidth is not null, GridClasses);

    /// <summary>Stops the script watching the grid and releases the interop script, if it was loaded.</summary>
    /// <returns>The task of the release.</returns>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _scope?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
