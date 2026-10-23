using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A tile of facts about one thing (a run's source, its status and timing): a title, then rows of icon, label
/// and value that line up, from one row to the next and from one tile to the next in an <see cref="OmniTileGrid"/>
/// (the icon and the label columns have a width of their own, the value takes the rest). Sibling of
/// <see cref="OmniStatTile"/>: given <see cref="OnClick"/>, the whole tile becomes one button; given
/// <see cref="Href"/>, one link; either keeps the tile's place in the grid (same size as its neighbours,
/// <see cref="DoubleWidth"/> honoured). A row's label and value stay on one line, cut with an ellipsis and read whole
/// in the tooltip. The tile is built of spans, valid in a button and in a link; it must never hold an interactive
/// element of its own beyond a row's <see cref="OmniFactTileRow.Href"/>, which a clickable tile refuses.
/// </summary>
public partial class OmniFactTile : IAsyncDisposable
{
    private readonly string _titleId = $"omni-fact-tile-{Guid.NewGuid():N}";
    private readonly OmniFactTileContext _context = new();
    private OmniPackageTooltips? _tooltips;
    private string? _safeHref;

    /// <summary>The rows, <see cref="OmniFactTileRow"/>. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// What the tile is about ("Source", "Run"), the visible title above the rows and the start of the accessible
    /// name of the tile. Wraps rather than overflows when it is longer than the tile.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>A decorative icon before the title, in general an <see cref="OmniIcon"/>. None when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Where the title sits across the tile: <see cref="OmniAlignment.Center"/> by default. <see cref="OmniAlignment.Stretch"/>
    /// has no meaning for a line of text and reads as <see cref="OmniAlignment.Start"/>.
    /// </summary>
    [Parameter]
    public OmniAlignment TitleAlign { get; set; } = OmniAlignment.Center;

    /// <summary>
    /// Makes the tile a button, named by its content. Cannot be combined with <see cref="Href"/>. A row of the tile cannot
    /// then have an <see cref="OmniFactTileRow.Href"/>, and no interactive element may sit in its rows or its
    /// <see cref="Icon"/>: nesting it in a button is invalid and is not detected.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Makes the tile a link to this path or address (relative, http, https, mailto or tel; any other scheme is
    /// refused), named and drawn like the button of <see cref="OnClick"/>. Null, the default, leaves a plain group.
    /// Cannot be combined with <see cref="OnClick"/>; a row of the tile cannot then have an <see cref="OmniFactTileRow.Href"/>.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// In an <see cref="OmniTileGrid"/>, the tile spans two columns instead of one. Ignored elsewhere.
    /// False by default.
    /// </summary>
    [Parameter]
    public bool DoubleWidth { get; set; }

    private string?[] TileClasses =>
    [
        "omni-fact-tile",
        $"omni-fact-tile--title-{TitleAlignName}",
        OnClick.HasDelegate || _safeHref is not null ? "omni-fact-tile--action" : null,
        DoubleWidth ? "omni-tile--double" : null
    ];

    private string TitleAlignName => TitleAlign switch
    {
        OmniAlignment.Center => "center",
        OmniAlignment.End => "end",
        _ => "start"
    };

    /// <summary>Checks that <see cref="Href"/> and <see cref="OnClick"/> are not both given and that the address is safe.</summary>
    /// <exception cref="ArgumentException"><see cref="Href"/> and <see cref="OnClick"/> are both given.</exception>
    /// <exception cref="InvalidOperationException"><see cref="Href"/> uses a scheme that is not allowed, or is a relative address that names another host (`//host`).</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _safeHref = OmniTileAction.SafeHref(nameof(OmniFactTile), Href, OnClick.HasDelegate);
        _context.IsAction = OnClick.HasDelegate || _safeHref is not null;
    }

    /// <summary>
    /// Turns the package tooltips on once, so a label or a value cut by its ellipsis is read whole on hover and
    /// on keyboard focus. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the tooltips are on.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _tooltips = new OmniPackageTooltips(JavaScript);
            await _tooltips.InstallAsync();
        }
    }

    /// <summary>Turns this tile's share of the package tooltips off.</summary>
    /// <returns>A task that completes once the script no longer counts the tile.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_tooltips is not null)
        {
            await _tooltips.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
