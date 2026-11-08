namespace OmniEurope.Blazor.Components;

/// <summary>
/// A navigation tile, the home of an administration area or a hub of sections (recette R-036):
/// an icon on a square tinted with <see cref="Tone"/>, a title and an optional description, the whole
/// tile one link to <see cref="Href"/>. Hovered, its border, background and shadow take its tone; it never moves. Place several
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
    private string? LinkTarget => SafeHref is var safe && !Disabled ? safe : null;

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

    /// <summary>
    /// The tone of the square and its icon; <see cref="OmniTone.Accent"/> by default. Inside an <see cref="OmniTileGrid"/>,
    /// a tile given no tone takes one of six hues of its own instead, in the order of the tiles of the grid (amber, blue,
    /// red, cyan, magenta, green, then again), so that no tile shares the hue of the one before it. The hues are the bright
    /// ANSI colours of the theme (<c>--omni-ansi-9</c> to <c>--omni-ansi-14</c>, which a host may replace with
    /// <c>--omni-tile-hue-1</c> to <c>--omni-tile-hue-6</c>): they read at 4.5:1 on the surface and on the 12 % tint of
    /// their own square, in light and dark mode. A tone set explicitly, <see cref="OmniTone.Accent"/> included, wins.
    /// </summary>
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
    /// Inside an <see cref="OmniTileGrid"/> the tiles that fit their title share one size instead: the grid
    /// takes the longest of their titles (its exact length up to 32 characters, then 40 and 48), computes the size
    /// from the real width of a tile, and every one of them uses it, so a short title is never smaller than a
    /// longer one while there is room. The title shrinks only when the longest one would touch the edge of the
    /// tile, down to the same floor.
    /// </summary>
    [Parameter]
    public bool FitTitle { get; set; }

    /// <summary>
    /// Where the icon sits across the tile. Null by default: centred, or at the start with <see cref="FitTitle"/>.
    /// <see cref="OmniAlignment.Stretch"/> has no meaning for the icon square and reads as <see cref="OmniAlignment.Start"/>.
    /// </summary>
    [Parameter]
    public OmniAlignment? IconAlign { get; set; }

    /// <summary>
    /// Where the title and the description sit across the tile, and how their lines align. Null by default: centred,
    /// or at the start with <see cref="FitTitle"/>, whose title keeps fitting its length whatever the alignment.
    /// <see cref="OmniAlignment.Stretch"/> has no meaning for a line of text and reads as <see cref="OmniAlignment.Start"/>.
    /// </summary>
    [Parameter]
    public OmniAlignment? TitleAlign { get; set; }

    private static string? AlignClass(string part, OmniAlignment? align) => align switch
    {
        null => null,
        OmniAlignment.Center => $"omni-nav-tile--{part}-center",
        OmniAlignment.End => $"omni-nav-tile--{part}-end",
        _ => $"omni-nav-tile--{part}-start"
    };

    private bool _toneSet;

    /// <summary>The class of the tone: the hue of its rank in a tile grid when no tone was given, the tone's own otherwise.</summary>
    private string ToneClass => TileGrid is not null && !_toneSet
        ? "omni-nav-tile--auto"
        : $"omni-nav-tile--{Tone.ToString().ToLowerInvariant()}";

    /// <summary>Notes whether <see cref="Tone"/> is among the parameters given, then sets them.</summary>
    /// <param name="parameters">The parameters supplied by the parent.</param>
    /// <returns>The task of the base implementation.</returns>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _toneSet = parameters.TryGetValue<OmniTone>(nameof(Tone), out _);
        return base.SetParametersAsync(parameters);
    }

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

    /// <summary>Tells the tile grid around the tile whether it is double width, and the length of its title when it fits it.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        OmniTileGridScope.Track(ref _countedIn, TileGrid, DoubleWidth);
        if (FitTitle)
        {
            TileGrid?.SetFitTitle(this, Title?.Length ?? 0);
        }
        else
        {
            TileGrid?.ClearFitTitle(this);
        }
    }

    /// <summary>Tells the tile grid around the tile that it no longer holds a double-width tile nor a title to fit.</summary>
    public void Dispose()
    {
        OmniTileGridScope.Track(ref _countedIn, null, false);
        TileGrid?.ClearFitTitle(this);
        GC.SuppressFinalize(this);
    }
}
