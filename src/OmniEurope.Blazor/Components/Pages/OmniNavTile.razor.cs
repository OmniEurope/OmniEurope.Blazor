namespace OmniEurope.Blazor.Components;

/// <summary>
/// A navigation tile, the home of an administration area or a hub of sections (recette R-036):
/// an icon on a square tinted with <see cref="Tone"/>, a title and an optional description, the whole
/// tile one link to <see cref="Href"/>. Hovered, only its shadow changes; it never moves. Place several
/// in an <see cref="OmniRow"/> of <see cref="OmniColumn"/>: a tile fills the height of its column, so a
/// row of tiles lines up whatever their descriptions.
/// </summary>
public partial class OmniNavTile
{
    private OmniTileGridScope? _countedIn;

    [CascadingParameter]
    private OmniTileGridScope? TileGrid { get; set; }

    /// <summary>Where the tile leads: a path of the site or an address.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = string.Empty;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));

    /// <summary>The name of the destination, the accessible name of the link with the description.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>An optional muted line under the title: what is found there.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The icon, usually an <see cref="OmniIcon"/>, drawn on the tinted square; decorative. None when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The tone of the square and its icon; <see cref="OmniTone.Accent"/> by default.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>
    /// In an <see cref="OmniTileGrid"/>, the tile spans two columns instead of one where the grid has two or
    /// more. Ignored elsewhere. False by default.
    /// </summary>
    [Parameter]
    public bool DoubleWidth { get; set; }

    /// <summary>Tells the tile grid around the tile whether it is double width.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        OmniTileGridScope.Track(ref _countedIn, TileGrid, DoubleWidth);
    }

    /// <summary>Tells the tile grid around the tile that it no longer holds a double-width tile.</summary>
    public void Dispose()
    {
        OmniTileGridScope.Track(ref _countedIn, null, false);
        GC.SuppressFinalize(this);
    }
}
