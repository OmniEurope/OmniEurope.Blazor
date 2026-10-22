using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Installs the shared tooltip listeners once per circuit. Scoped rather than static because a
/// Blazor Server process holds many circuits at once, and a static flag would let the first one
/// decide that every later circuit already had its listeners.
/// </summary>
internal sealed class OmniTooltipInterop(IJSRuntime javaScript) : IAsyncDisposable
{
    private const string ModulePath = Internal.OmniModules.Tooltip;

    private IJSObjectReference? _module;
    private Task? _install;
    private bool _disposed;

    /// <summary>
    /// The task is kept rather than a boolean so that the tooltips rendered in the same batch, which
    /// all reach this at once, await the single import instead of each starting one of their own. Only
    /// a success is kept: after a failure (a lost network, a circuit that went) the next tooltip tries
    /// again, and the callers that were waiting see the failure.
    /// </summary>
    public async Task EnsureInstalledAsync()
    {
        var install = _install ??= InstallAsync();
        try
        {
            await install;
        }
        catch
        {
            if (ReferenceEquals(_install, install))
            {
                _install = null;
            }

            throw;
        }
    }

    private async Task InstallAsync()
    {
        var module = await javaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        if (_disposed)
        {
            // The circuit's scope ended while the script loaded: nothing is installed, nothing stays held.
            await ReleaseAsync(module);
            return;
        }

        _module = module;
        try
        {
            await module.InvokeVoidAsync("install");
        }
        catch
        {
            _module = null;
            await ReleaseAsync(module);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is { } module)
        {
            _module = null;
            await ReleaseAsync(module);
        }
    }

    private static async Task ReleaseAsync(IJSObjectReference module)
    {
        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone; there is no module left to release.
        }
    }
}
