namespace OmniEurope.Blazor.Components;

/// <summary>
/// A navigation tile, the home of an administration area or a hub of sections (Astraia recette R-036):
/// an icon on a square tinted with <see cref="Tone"/>, a title and an optional description, the whole
/// tile one link to <see cref="Href"/>. Hovered, only its shadow changes; it never moves. Place several
/// in an <see cref="OmniRow"/> of <see cref="OmniColumn"/>: a tile fills the height of its column, so a
/// row of tiles lines up whatever their descriptions.
/// </summary>
public partial class OmniNavTile
{
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
}
