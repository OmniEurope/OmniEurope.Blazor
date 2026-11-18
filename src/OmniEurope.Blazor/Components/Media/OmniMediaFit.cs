namespace OmniEurope.Blazor.Components;

/// <summary>How the picture of an <see cref="OmniMediaTile"/> fills the tile (the CSS <c>object-fit</c> of its video).</summary>
public enum OmniMediaFit
{
    /// <summary>
    /// The whole picture is shown, scaled to fit, with bands of the tile background on the sides or above and below
    /// when its ratio differs from the tile's. The drawing layer covers the picture only, not the bands.
    /// </summary>
    Contain,

    /// <summary>
    /// The picture fills the whole tile and the part that does not fit is cut off. The drawing layer covers the whole
    /// picture, including what is cut off, so its strokes are the same as on a tile that contains it.
    /// </summary>
    Cover
}
