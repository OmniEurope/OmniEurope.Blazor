using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Makes a native <c>details</c> dropdown close on Escape and on a press outside it, which the
/// element does not do by itself and which Blazor cannot observe without a document listener. The
/// listener is <c>omni-focus.js</c>'s; this only configures it once per rendered element and again
/// when an option changes.
/// </summary>
internal sealed class OmniDisclosureDismissal(IJSRuntime javaScript) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private ElementReference _element;
    private string? _configuredId;
    private bool _closeOnOutsideClick;
    private bool _closeOnItem;
    private bool _disposed;

    internal async Task ApplyAsync(ElementReference details, bool closeOnOutsideClick, bool closeOnItem)
    {
        if (_disposed
            || string.IsNullOrEmpty(details.Id)
            || (details.Id == _configuredId && closeOnOutsideClick == _closeOnOutsideClick && closeOnItem == _closeOnItem))
        {
            return;
        }

        if (_module is null)
        {
            var module = await javaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Focus);
            if (_disposed)
            {
                // The owner left the page during the import: configure nothing, release the module.
                await ReleaseAsync(module);
                return;
            }

            _module = module;
        }

        // Recorded before the call, so that a disposal during it releases this element.
        _element = details;
        await _module.InvokeVoidAsync("configureDisclosure", details, closeOnOutsideClick, closeOnItem);
        _configuredId = details.Id;
        _closeOnOutsideClick = closeOnOutsideClick;
        _closeOnItem = closeOnItem;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("disposeDisclosure", _element);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
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
        }
    }
}
