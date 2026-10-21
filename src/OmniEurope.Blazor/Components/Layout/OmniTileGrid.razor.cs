using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The grid every tile of the package sits in (recette R1-20): <see cref="OmniCard"/>,
/// <see cref="OmniSelectableCard"/>, <see cref="OmniStatTile"/> and <see cref="OmniNavTile"/> placed
/// directly inside have the same width (<see cref="Columns"/> equal columns), exactly the same height (the
/// height of the tallest tile, on every row), and never spill: a long word wraps or is cut inside its tile
/// and the body of a card scrolls. A tile with <c>DoubleWidth</c> spans two columns. From a window 40rem wide
/// the grid has at most two columns, from 64rem all of <see cref="Columns"/>, as the columns of an
/// <see cref="OmniRow"/>; narrower, one column, where a double-width tile is as wide as the others.
/// </summary>
public partial class OmniTileGrid
{
    /// <summary>The largest number of columns the grid takes.</summary>
    internal const int MaxColumns = 6;

    /// <summary>The tiles of the grid. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The number of equal columns of a wide window, from 1 to 6; 4 by default. A grid of one column is a
    /// stack of tiles of the same height.
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 4;

    /// <summary>The space between the tiles; <see cref="OmniSpacing.Medium"/> by default.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    private string?[] TileGridClasses =>
    [
        "omni-tile-grid",
        $"omni-tile-grid--columns-{Columns}",
        $"omni-tile-grid--gap-{OmniSpacingTokens.Suffix(Gap)}"
    ];

    /// <summary>Rejects a number of columns outside 1 to 6 with an <see cref="ArgumentOutOfRangeException"/>.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Columns is < 1 or > MaxColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(Columns), Columns, "A tile grid has between 1 and 6 columns.");
        }
    }
}
