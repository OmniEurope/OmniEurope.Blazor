using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The "back to top" button: a square icon button fixed in the end corner of the window, which appears once the
/// content has scrolled past <see cref="Threshold"/> and brings it back to its top. Place it once in the layout or
/// in a long page.
/// </summary>
/// <remarks>
/// The content is, by default, the content scroller of the OE layout, the <see cref="OmniMain"/> with
/// <see cref="OmniMain.Scrollable"/> that holds the button (or else the first of the page), and the document when
/// no main area scrolls; <see cref="TargetId"/> names another element. Going to the top is smooth, and instant
/// when the reader asks for reduced motion (<c>prefers-reduced-motion</c>). The focus then moves to the top of the
/// content, so the next Tab starts there rather than on a button that hides: the content itself (an
/// <see cref="OmniMain"/> takes the focus from script by default), or the first <c>main</c> landmark for the
/// document. The script (<c>omni-back-to-top.js</c>) tells the button to show; until it runs (prerendering, a static
/// page, a lost circuit) the button stays hidden, and nothing is thrown.
/// </remarks>
public partial class OmniBackToTop : IAsyncDisposable
{
    private ElementReference _frame;
    private IJSObjectReference? _module;
    // What the script follows. A threshold of -1 is never a valid one (Threshold is refused below 0): nothing attached yet.
    private (string? TargetId, int Threshold) _attached = (null, -1);
    private bool _disposed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>
    /// How far, in CSS pixels, the content must have scrolled before the button appears; 400 by default, about half a
    /// screen. Zero shows it as soon as the content leaves its top. A negative value is refused.
    /// </summary>
    [Parameter]
    public int Threshold { get; set; } = 400;

    /// <summary>The accessible name of the button, repeated by its tooltip; null or blank takes the localized "Back to top".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The id of the element that scrolls the content; null, the default, takes the content scroller of the OE layout
    /// (a scrollable <see cref="OmniMain"/>), or the document when no main area scrolls. While the id names no
    /// element, the button stays hidden.
    /// </summary>
    [Parameter]
    public string? TargetId { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "BackToTopLabel");

    /// <summary>Refuses a negative <see cref="Threshold"/>.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentOutOfRangeException.ThrowIfNegative(Threshold);
    }

    /// <summary>
    /// Loads the script on the first interactive render and starts following the scroll; a later render passes a new
    /// <see cref="TargetId"/> or <see cref="Threshold"/> to it. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script follows the current target and threshold.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (_module is null)
            {
                // A later render that runs while the first one still awaits the module leaves the attachment to it.
                if (!firstRender)
                {
                    return;
                }

                _module = await Internal.OmniScriptModule.ImportAsync(JavaScript, Internal.OmniModules.BackToTop, () => _disposed);
                if (_module is null)
                {
                    return;
                }
            }

            var wanted = (TargetId, Threshold);
            if (_disposed || _attached == wanted)
            {
                return;
            }

            // Recorded before the call: a render that runs while it is in flight must not attach a second time.
            _attached = wanted;
            await _module.InvokeVoidAsync("attach", _frame, TargetId, Threshold);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }

    // The button only shows once the script runs, so a click without the module (none can happen) does nothing.
    private async Task ScrollToTopAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeAsync<bool>("scrollToTop", _frame);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }

    /// <summary>Stops following the scroll and releases the script.</summary>
    /// <returns>A task that completes once the script is released; a lost circuit is ignored.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _frame);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, and the script with it.
            }
        }

        GC.SuppressFinalize(this);
    }
}
