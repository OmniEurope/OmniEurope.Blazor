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

    internal async Task ApplyAsync(ElementReference details, bool closeOnOutsideClick, bool closeOnItem)
    {
        if (string.IsNullOrEmpty(details.Id)
            || (details.Id == _configuredId && closeOnOutsideClick == _closeOnOutsideClick && closeOnItem == _closeOnItem))
        {
            return;
        }

        _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
        await _module.InvokeVoidAsync("configureDisclosure", details, closeOnOutsideClick, closeOnItem);
        _element = details;
        _configuredId = details.Id;
        _closeOnOutsideClick = closeOnOutsideClick;
        _closeOnItem = closeOnItem;
    }

    public async ValueTask DisposeAsync()
    {
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
}
