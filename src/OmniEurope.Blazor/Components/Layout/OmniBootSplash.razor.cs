namespace OmniEurope.Blazor.Components;

/// <summary>
/// Removes the boot splash of the page once the application has rendered: place it once, in the
/// layout or the root component. It renders nothing.
/// </summary>
/// <remarks>
/// The splash is written by the host in its page, outside the element Blazor renders into, so it is
/// on screen from the first paint, before any script of the application has run:
/// <code>
/// &lt;div id="omni-boot-splash" class="omni-boot-splash" role="status"&gt;
///     &lt;span class="omni-boot-splash__spinner" aria-hidden="true"&gt;&lt;/span&gt;
///     &lt;span class="omni-visually-hidden"&gt;Loading&lt;/span&gt;
/// &lt;/div&gt;
/// </code>
/// It is drawn by the library's stylesheet alone, over the page, in the colours of the appearance
/// <c>omni-boot.js</c> applied. This component fades it out after the first render and removes it;
/// a page without a splash is left as it is.
/// </remarks>
public partial class OmniBootSplash
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";

    private IJSObjectReference? _module;
    private bool _disposed;

    /// <summary>The id of the splash element in the page; <c>omni-boot-splash</c> by default.</summary>
    [Parameter]
    public string SplashId { get; set; } = "omni-boot-splash";

    /// <summary>Raised after the first render with whether a splash was found and removed.</summary>
    [Parameter]
    public EventCallback<bool> OnHidden { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);

            // The splash is removed even when this component went away meanwhile: nothing else would.
            var hidden = await module.InvokeAsync<bool>("hideBootSplash", SplashId);
            if (_disposed)
            {
                await module.DisposeAsync();
                return;
            }

            _module = module;
            await OnHidden.InvokeAsync(hidden);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone before the splash could be removed; so is the page.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}