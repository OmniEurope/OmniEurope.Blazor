namespace OmniEurope.Blazor.Components;

/// <summary>
/// One fact of an <see cref="OmniFactTile"/>: an icon, a label and a value, in three columns that every
/// row of the tile (and every tile of an <see cref="OmniTileGrid"/>) shares. The value is the content; given
/// <see cref="Href"/>, it is a link.
/// </summary>
public partial class OmniFactTileRow
{
    [CascadingParameter]
    private OmniFactTileContext? Tile { get; set; }

    /// <summary>An icon before the label, in general an <see cref="OmniIcon"/>; decorative, the label names the fact. The cell stays when none is given, so the labels keep their column.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>What the fact is ("Branch"). One line: a label wider than its column is cut with an ellipsis and its whole text is in the tooltip.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The value: text, a link, a badge. One line: a value wider than its column is cut with an ellipsis and its whole text
    /// is in the tooltip. An <see cref="OmniBadge"/> in it is drawn small. Never put an interactive element in it when the
    /// tile is a link or a button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Makes the value a link to this path or address (relative, http, https, mailto or tel; any other scheme is
    /// refused). Null, the default, leaves it as it is. Cannot be used in a tile that has its own
    /// <see cref="OmniFactTile.Href"/> or <see cref="OmniFactTile.OnClick"/>: the whole tile is already the link.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Checks that a link row does not sit in a tile that is itself a link or a button.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Href"/> is given in a tile that has an <c>Href</c> or an <c>OnClick</c>.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.IsNullOrWhiteSpace(Href) && Tile is { IsAction: true })
        {
            throw new InvalidOperationException("An OmniFactTileRow cannot have an Href inside an OmniFactTile that has its own Href or OnClick: a link cannot sit in a link or a button.");
        }
    }
}
