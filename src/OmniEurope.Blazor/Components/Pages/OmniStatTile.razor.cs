namespace OmniEurope.Blazor.Components;

/// <summary>
/// A key figure on a tile: an icon, the value, what it measures and an optional detail line. Given
/// <see cref="OnClick"/>, the whole tile becomes one button that opens the figure's detail; given
/// <see cref="Href"/>, one link to it. Either keeps the tile's place in an <see cref="OmniTileGrid"/> (same
/// size as its neighbours, <see cref="DoubleWidth"/> honoured). The tile is built of spans, valid in a
/// button and in a link; it must never hold an interactive element of its own.
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
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Makes the tile a link to this path or address (relative, http, https, mailto or tel; any other
    /// scheme is refused), named like the button of <see cref="OnClick"/> and drawn the same way. Null, the
    /// default, leaves a plain block. Cannot be combined with <see cref="OnClick"/>.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

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
    /// Attaches the script that keeps the label on one line: smaller when it does not fit, then cut with
    /// an ellipsis and its whole text in the hover tooltip. The tile is a button with <see cref="OnClick"/>,
    /// a link with <see cref="Href"/> and a plain block without either, three different elements: when one of them comes or goes, the
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

    private string AccessibleName => string.IsNullOrWhiteSpace(Value) ? Title : Localize("LabelValuePair", Title, Value);
}
