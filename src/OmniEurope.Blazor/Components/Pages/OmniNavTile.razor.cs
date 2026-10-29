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

    // Checked even when disabled, so a disabled tile cannot hide an unsafe address that enabling it would use.
    private string? LinkTarget => SafeHref is { } safe && !Disabled ? safe : null;

    /// <summary>The name of the destination, the accessible name of the link with the description.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>An optional muted line under the title: what is found there.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the tile leads nowhere for now (nothing to review, a right the user lacks): it renders no
    /// <c>href</c>, so it is neither a working link nor reachable by Tab, carries <c>aria-disabled="true"</c>
    /// and is drawn attenuated, with no hover lift and the same size in an <see cref="OmniTileGrid"/>.
    /// <see cref="Href"/> is still checked. False by default.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

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

    /// <summary>
    /// Fits the title on one line: its font size shrinks with the tile's width (container query units, no
    /// script) until the whole title fits, a word is never broken, and the title and the description are
    /// left-aligned instead of centred. The size is computed from the length of <see cref="Title"/> (rounded
    /// up to a step of 8, 12, 16, 20, 24, 32 or 48 characters, so the markup carries a class, never an inline
    /// style) and stops at 0.75rem; under that size the title is cut with an ellipsis. For a tile used
    /// in a narrow column, such as six per row. False by default: the title wraps and is centred.
    /// </summary>
    [Parameter]
    public bool FitTitle { get; set; }

    private static readonly int[] FitBuckets = [8, 12, 16, 20, 24, 32, 48];

    /// <summary>The smallest length step that holds the title, 48 for anything longer.</summary>
    private int FitBucket
    {
        get
        {
            var length = Title?.Length ?? 0;
            foreach (var bucket in FitBuckets)
            {
                if (length <= bucket)
                {
                    return bucket;
                }
            }

            return FitBuckets[^1];
        }
    }

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
