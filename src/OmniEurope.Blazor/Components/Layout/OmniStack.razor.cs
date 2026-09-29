using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A flex container that lays its children out in one direction with a gap from the spacing scale; a
/// horizontal stack can also wrap, scroll or collapse when it runs out of room (<see cref="Overflow"/>).
/// </summary>
public partial class OmniStack
{
    private ElementReference _strip;
    private IJSObjectReference? _module;
    private bool _scrollConfigured;

    /// <summary>The items of the stack. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The direction of the stack; <see cref="OmniStackOrientation.Vertical"/> by default.</summary>
    [Parameter]
    public OmniStackOrientation Orientation { get; set; } = OmniStackOrientation.Vertical;

    /// <summary>The space between the items; <see cref="OmniSpacing.Medium"/> by default.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    /// <summary>How the items sit across the direction of the stack; <see cref="OmniAlignment.Stretch"/> by default.</summary>
    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    /// <summary>How the items are spread along the direction of the stack; <see cref="OmniJustification.Start"/> by default.</summary>
    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    /// <summary>
    /// What a horizontal stack does when it runs out of room. Only meaningful horizontally: a
    /// vertical stack overflows down the page, which the page already handles.
    /// </summary>
    [Parameter]
    public OmniStackOverflow Overflow { get; set; } = OmniStackOverflow.None;

    /// <summary>
    /// Lets the items flow onto further lines (<c>flex-wrap: wrap</c>) instead of staying on one, the gap
    /// separating the lines as well as the items: a row of badges or actions that must never overflow
    /// its container. Off by default. Ignored when <see cref="Overflow"/> is set, which keeps the items
    /// on one line by definition.
    /// </summary>
    [Parameter]
    public bool Wrap { get; set; }

    private string?[] StackClasses =>
    [
        "omni-stack",
        $"omni-stack--{Orientation.ToString().ToLowerInvariant()}",
        $"omni-stack--gap-{OmniSpacingTokens.Suffix(Gap)}",
        $"omni-stack--align-{Align.ToString().ToLowerInvariant()}",
        $"omni-stack--justify-{Justify.ToString().ToLowerInvariant()}",
        Wrap && Overflow == OmniStackOverflow.None ? "omni-stack--wrap" : null,
        Overflow == OmniStackOverflow.None ? null : $"omni-stack--overflow-{Overflow.ToString().ToLowerInvariant()}"
    ];

    /// <summary>The scrolling row inside the wrapper; the host's own class stays on the wrapper.</summary>
    private string ViewportClass => CssClassBuilder.Combine([.. StackClasses, "omni-stack-scroll__viewport"]);

    /// <summary>
    /// The chevrons follow the scroll position, which only the browser knows, so the shared overflow
    /// script owns them, as it does a tab strip's. Configured once the wrapper exists, released when a
    /// change of mode takes it away. A lost circuit is ignored.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var scrolling = Overflow == OmniStackOverflow.Scroll;
        if (scrolling == _scrollConfigured)
        {
            return;
        }

        try
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Focus);
            await _module.InvokeVoidAsync(scrolling ? "configureScrollOverflow" : "disposeScrollOverflow", _strip);
            _scrollConfigured = scrolling;
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Releases the scroll chevrons, if they were configured, and the interop module.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            if (_scrollConfigured)
            {
                await _module.InvokeVoidAsync("disposeScrollOverflow", _strip);
            }

            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
