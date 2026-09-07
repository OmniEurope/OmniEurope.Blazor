using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Installs the shared tooltip listeners once per circuit. Scoped rather than static because a
/// Blazor Server process holds many circuits at once, and a static flag would let the first one
/// decide that every later circuit already had its listeners.
/// </summary>
internal sealed class OmniTooltipInterop(IJSRuntime javaScript) : IAsyncDisposable
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-tooltip.js";

    private IJSObjectReference? _module;
    private Task? _install;

    /// <summary>
    /// The task is kept rather than a boolean so that the tooltips rendered in the same batch, which
    /// all reach this at once, await the single import instead of each starting one of their own.
    /// </summary>
    public Task EnsureInstalledAsync() => _install ??= InstallAsync();

    private async Task InstallAsync()
    {
        _module = await javaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        await _module.InvokeVoidAsync("install");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone; there is no module left to release.
        }
    }
}
