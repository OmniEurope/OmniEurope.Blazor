using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The main landmark (<c>main</c>) of an <see cref="OmniLayout"/>, beside the sidebar in the
/// <see cref="OmniBody"/>: the page content, optionally narrowed to a centred column and optionally the
/// scroll container of the page.
/// </summary>
public partial class OmniMain
{
    private const string InteropModulePath = Internal.OmniModules.Interop;

    private ElementReference _main;
    private IJSObjectReference? _module;
    private bool _watching;
    private bool _disposed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The page content. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the landmark can receive focus from script (<c>tabindex="-1"</c>), so a skip link or a
    /// navigation can move focus to the content without adding it to the tab order. On by default.
    /// </summary>
    [Parameter]
    public bool FocusTarget { get; set; } = true;

    /// <summary>The id of the element that names the landmark (<c>aria-labelledby</c>), such as the page title; none by default.</summary>
    [Parameter]
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// How wide the content runs inside the main area: the whole of it, or a centred column capped
    /// at 90rem (<see cref="OmniLayoutWidth.Wide"/>) or 72rem (<see cref="OmniLayoutWidth.Content"/>).
    /// Only the content narrows; the header and the sidebar keep their width. The two caps are read from
    /// <c>--omni-layout-wide-width</c> and <c>--omni-layout-content-width</c>, so a host can set its own.
    /// </summary>
    [Parameter]
    public OmniLayoutWidth ContentWidth { get; set; } = OmniLayoutWidth.Full;

    /// <summary>
    /// Makes the main area the scroll container of the page, filling the height its
    /// <see cref="OmniBody"/> leaves free. It scrolls across the whole width of the area, so the
    /// scrollbar sits at the window's edge even when <see cref="ContentWidth"/> centres the content,
    /// and the content keeps a gutter of <c>--omni-main-gutter</c> (by default the medium spacing).
    /// The shell has to be bounded in height for anything to scroll here: an <see cref="OmniLayout"/>
    /// whose body holds a scrollable main becomes a column, and the host gives it its height, for
    /// example the viewport's.
    /// </summary>
    [Parameter]
    public bool Scrollable { get; set; }

    /// <summary>
    /// With <see cref="Scrollable"/>, hides the page scrollbar until the page moves: it shows while the
    /// user scrolls and fades out shortly after, like an overlay scrollbar. Off by default.
    /// </summary>
    [Parameter]
    public bool AutoHideScrollbar { get; set; }

    /// <summary>
    /// Whenever <see cref="Scrollable"/> and <see cref="AutoHideScrollbar"/> become both on, loads the
    /// interop module (once) and starts watching the scrolling that shows the scrollbar; when either
    /// turns off, stops watching. A lost circuit is ignored.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var wanted = Scrollable && AutoHideScrollbar;
        if (wanted == _watching || _disposed)
        {
            return;
        }

        _watching = wanted;
        try
        {
            if (!wanted)
            {
                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("unwatchScrolling", _main);
                }

                return;
            }

            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
            if (!_disposed)
            {
                await _module.InvokeVoidAsync("watchScrolling", _main, "omni-main--scrolling");
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nothing left to watch.
        }
    }

    /// <summary>Stops watching the scrolling and releases the interop module, if it was loaded.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("unwatchScrolling", _main);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private string? WidthClass => ContentWidth == OmniLayoutWidth.Full
        ? null
        : $"omni-main--{ContentWidth.ToString().ToLowerInvariant()}";
}
