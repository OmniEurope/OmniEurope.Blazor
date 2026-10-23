namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniFactTile"/> hands down to its rows: whether the whole tile is already one link or
/// one button, in which case a row cannot be a link of its own (a link inside a link or a button is invalid).
/// </summary>
internal sealed class OmniFactTileContext
{
    /// <summary>True when the tile has an <c>Href</c> or an <c>OnClick</c>.</summary>
    internal bool IsAction { get; set; }
}
