using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Placed once in a layout, it replaces the browser's tooltip of every element carrying a title
/// attribute with the package tooltip: at most 12rem wide, shown after a short delay at the pointer,
/// with a chevron toward it, and kept inside the window. It renders nothing itself.
/// </summary>
public sealed class OmniTitleTooltips : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = Internal.OmniModules.Tooltip;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private IJSObjectReference? _module;
    private bool _loading;
    private bool _installed;
    private bool _disposed;

    /// <summary>
    /// On the first render, loads the tooltip script and installs the document listeners that replace
    /// title tooltips. The script installs them once per page, however many instances are placed, and
    /// removes them with the last one. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the listeners are installed.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _disposed)
        {
            return;
        }

        _loading = true;
        try
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
            if (!_disposed)
            {
                await _module.InvokeVoidAsync("installTitleTooltips");
                _installed = true;
            }
        }
        catch (JSDisconnectedException)
        {
        }
        finally
        {
            _loading = false;
        }

        // Disposed while the script loaded or installed: DisposeAsync left the release to this method.
        if (_disposed)
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Uninstalls this instance's share of the document listeners, then releases the script module.
    /// Once the last instance on the page is gone, the listeners are removed and the browser's own
    /// title tooltips come back. A lost circuit is ignored.
    /// </summary>
    /// <returns>A task that completes once the module is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_loading)
        {
            await ReleaseAsync();
        }
    }

    private async Task ReleaseAsync()
    {
        if (_module is null)
        {
            return;
        }

        var module = _module;
        _module = null;
        try
        {
            if (_installed)
            {
                _installed = false;
                await module.InvokeVoidAsync("uninstallTitleTooltips");
            }

            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
