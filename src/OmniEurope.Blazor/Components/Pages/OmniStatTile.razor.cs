namespace OmniEurope.Blazor.Components;

/// <summary>
/// A key figure on a tile: an icon, the value, what it measures and an optional detail line. Given
/// <see cref="OnClick"/>, the whole tile becomes one button that opens the figure's detail; given
/// <see cref="Href"/>, one link to it. Either keeps the tile's place in an <see cref="OmniTileGrid"/> (same
/// size as its neighbours, <see cref="DoubleWidth"/> honoured). The tile is built of spans, valid in a
/// button and in a link; it never holds an interactive element of its own, with one supported exception:
/// <see cref="Help"/>. A help mark is focusable, so it cannot sit inside the button or the link; with
/// <see cref="Help"/> together with <see cref="OnClick"/> or <see cref="Href"/>, the root of the tile is a
/// <c>div</c> (same classes, <c>id</c> and attributes) holding the real <c>button</c> or <c>a</c>, stretched
/// over the whole tile, and the mark in the corner, outside it.
/// </summary>
public partial class OmniStatTile
{
    private ElementReference _tile;
    private IJSObjectReference? _module;
    private ElementReference? _attached;
    private bool _disposed;
    private string? _safeHref;
    private OmniTileGridScope? _countedIn;

    [CascadingParameter]
    private OmniTileGridScope? TileGrid { get; set; }

    /// <summary>A decorative icon on a tinted square before the text. None when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The figure itself, already formatted: "12,4 k", "83 %".</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>What the figure measures ("Tokens today"), the visible caption written under the value; with <see cref="OnClick"/>, the start of the accessible name of the button.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>An optional muted line under the title: a reset time, a comparison.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>
    /// Makes the tile a button, named by <see cref="Title"/> followed by <see cref="Value"/>, as the
    /// tile reads them. Cannot be combined with <see cref="Href"/>. Never put an interactive element in
    /// <see cref="Icon"/>: nesting it in a button is invalid and is not detected.
    /// The one supported interactive part is the <see cref="Help"/> mark: with it the button is no longer the
    /// root but sits inside a <c>div.omni-stat-tile</c>, stretched over the whole tile, and a click on the mark
    /// does not raise this event.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Makes the tile a link to this path or address (relative, http, https, mailto or tel; any other
    /// scheme is refused), named like the button of <see cref="OnClick"/> and drawn the same way. Null, the
    /// default, leaves a plain block. Cannot be combined with <see cref="OnClick"/>. With <see cref="Help"/>, the
    /// link sits inside a <c>div.omni-stat-tile</c>, stretched over the whole tile, and a click on the help mark
    /// does not follow it.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// A help text for the tile, behind a "?" in its top inline-end corner, drawn and behaving as
    /// <see cref="OmniHeading.Help"/> and <see cref="OmniFormField.Help"/>: it shows on hover and on keyboard
    /// focus (the mark is in the Tab order) and is the mark's accessible description, never a part of the name
    /// of the tile. The value keeps clear of the mark. Given an <see cref="OmniComponentBase.Id"/>, the mark has
    /// the id <c>{Id}-help</c> and the text <c>{Id}-help-content</c>. With <see cref="OnClick"/> or
    /// <see cref="Href"/>, the tile is a <c>div</c> holding the button or the link (see the class remarks), and a
    /// click or Enter on the mark never raises <see cref="OnClick"/> nor follows <see cref="Href"/>. Null or
    /// blank, the default, renders no mark and leaves the markup unchanged.
    /// </summary>
    [Parameter]
    public string? Help { get; set; }

    /// <summary>
    /// In an <see cref="OmniTileGrid"/>, the tile spans two columns instead of one where the grid has two or
    /// more. Ignored elsewhere. False by default.
    /// </summary>
    [Parameter]
    public bool DoubleWidth { get; set; }

    /// <summary>
    /// Lets the <see cref="Title"/> wrap onto as many lines as it needs, at the normal caption size, instead of
    /// keeping it on one line that shrinks, then is cut with an ellipsis. For a long caption such as
    /// "Longest streak of correct answers". The tile grows taller, and in an <see cref="OmniTileGrid"/> with
    /// equal heights (the default) every tile of the grid takes the height of the tallest, as with a detail
    /// line; with <c>EqualHeights="false"</c> only the tiles of that row do. Long words break rather than
    /// overflow. False by default: the caption stays on one line.
    /// </summary>
    [Parameter]
    public bool WrapTitle { get; set; }

    /// <summary>
    /// The size of the tile: <see cref="OmniControlSize.Small"/> is the compact tile (tighter padding, a
    /// smaller value), <see cref="OmniControlSize.Large"/> a roomier one (wider padding, a larger value),
    /// <see cref="OmniControlSize.Medium"/>, the default, the standard tile. It sets the padding and the
    /// size the value starts from; the value still shrinks to fit the tile width, then wraps, whatever the
    /// size. The icon square and the caption keep their size. Written as the modifier class
    /// <c>omni-stat-tile--small</c> or <c>omni-stat-tile--large</c> (none for Medium).
    /// </summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Checks that <see cref="Href"/> and <see cref="OnClick"/> are not both given and that the address is safe.</summary>
    /// <exception cref="ArgumentException"><see cref="Href"/> and <see cref="OnClick"/> are both given.</exception>
    /// <exception cref="InvalidOperationException"><see cref="Href"/> uses a scheme that is not allowed, or is a relative address that names another host (`//host`).</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _safeHref = Internal.OmniTileAction.SafeHref(nameof(OmniStatTile), Href, OnClick.HasDelegate);
        OmniTileGridScope.Track(ref _countedIn, TileGrid, DoubleWidth);
    }

    /// <summary>
    /// Attaches the script that keeps the label on one line (smaller when it does not fit, then cut with
    /// an ellipsis and its whole text in the hover tooltip) and fits the value to the tile (smaller in up to
    /// three steps, then wrapped, never cut). The tile is a button with <see cref="OnClick"/>,
    /// a link with <see cref="Href"/> and a plain block without either (a block holding the button or the link, with <see cref="Help"/>), different elements: when one of them comes or goes, the
    /// script leaves the former element and follows the new one. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script follows the element drawn.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                var module = await Internal.OmniScriptModule.ImportAsync(JavaScript, Internal.OmniModules.StatTile);
                if (_disposed)
                {
                    await module.DisposeAsync();
                    return;
                }

                _module = module;
            }

            // On Blazor Server a later render can run while the first one still awaits the module: it
            // leaves the attachment to the first render, which attaches the element drawn by then.
            if (_module is null || _attached?.Id == _tile.Id)
            {
                return;
            }

            if (_attached is { } former)
            {
                await _module.InvokeVoidAsync("detach", former);
            }

            await _module.InvokeVoidAsync("attach", _tile);
            _attached = _tile;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the tile with it.
        }
    }

    /// <summary>Detaches the script: its observers are released.</summary>
    /// <returns>A task that completes once the script is detached; a lost circuit is ignored.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        OmniTileGridScope.Track(ref _countedIn, null, false);
        if (_module is not null)
        {
            try
            {
                if (_attached is { } attached)
                {
                    await _module.InvokeVoidAsync("detach", attached);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    private string? SizeClass => Size switch
    {
        OmniControlSize.Small => "omni-stat-tile--small",
        OmniControlSize.Large => "omni-stat-tile--large",
        _ => null,
    };

    private bool HasHelp => !string.IsNullOrWhiteSpace(Help);

    private string? HelpId => string.IsNullOrWhiteSpace(Id) ? null : $"{Id}-help";

    private string AccessibleName => string.IsNullOrWhiteSpace(Value) ? Title : Localize("LabelValuePair", Title, Value);
}
