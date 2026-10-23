namespace OmniEurope.Blazor.Internal;

/// <summary>
/// One component's share of the package tooltips (the floating box that reads the whole text of a cut cell,
/// <c>omni-tooltip.js</c>): turned on once after the first render and turned off when the component goes away. The
/// script counts the components that asked, so the listeners live exactly as long as one of them is on the page.
/// A lost circuit is ignored.
/// </summary>
internal sealed class OmniPackageTooltips(IJSRuntime javaScript) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _installed;
    private bool _disposed;

    /// <summary>Turns the package tooltips on for this component, once.</summary>
    /// <returns>A task that completes once the script counts this component.</returns>
    internal async Task InstallAsync()
    {
        if (_installed || _disposed)
        {
            return;
        }

        try
        {
            var module = await javaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Tooltip);
            if (_disposed)
            {
                await module.DisposeAsync();
                return;
            }

            _module = module;
            await module.InvokeVoidAsync("installPackageTooltips");
            _installed = true;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the component with it.
        }
    }

    /// <summary>Turns this component's share off and releases the module.</summary>
    /// <returns>A task that completes once the script no longer counts this component.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is null)
        {
            return;
        }

        try
        {
            if (_installed)
            {
                await _module.InvokeVoidAsync("uninstallPackageTooltips");
            }

            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
