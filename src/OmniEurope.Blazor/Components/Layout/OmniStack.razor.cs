using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniStack
{
    private ElementReference _strip;
    private IJSObjectReference? _module;
    private bool _scrollConfigured;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniStackOrientation Orientation { get; set; } = OmniStackOrientation.Vertical;

    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    /// <summary>
    /// What a horizontal stack does when it runs out of room. Only meaningful horizontally: a
    /// vertical stack overflows down the page, which the page already handles.
    /// </summary>
    [Parameter]
    public OmniStackOverflow Overflow { get; set; } = OmniStackOverflow.None;

    private string?[] StackClasses =>
    [
        "omni-stack",
        $"omni-stack--{Orientation.ToString().ToLowerInvariant()}",
        $"omni-stack--gap-{Gap.ToString().ToLowerInvariant()}",
        $"omni-stack--align-{Align.ToString().ToLowerInvariant()}",
        $"omni-stack--justify-{Justify.ToString().ToLowerInvariant()}",
        Overflow == OmniStackOverflow.None ? null : $"omni-stack--overflow-{Overflow.ToString().ToLowerInvariant()}"
    ];

    /// <summary>The scrolling row inside the wrapper; the host's own class stays on the wrapper.</summary>
    private string ViewportClass => CssClassBuilder.Combine([.. StackClasses, "omni-stack-scroll__viewport"]);

    /// <summary>
    /// The chevrons follow the scroll position, which only the browser knows, so the shared overflow
    /// script owns them, as it does a tab strip's. Configured once the wrapper exists, released when a
    /// change of mode takes it away.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var scrolling = Overflow == OmniStackOverflow.Scroll;
        if (scrolling == _scrollConfigured)
        {
            return;
        }

        _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
        await _module.InvokeVoidAsync(scrolling ? "configureScrollOverflow" : "disposeScrollOverflow", _strip);
        _scrollConfigured = scrolling;
    }

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
