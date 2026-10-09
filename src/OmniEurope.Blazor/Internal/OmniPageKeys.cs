using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns on the keys the focus module answers for the whole page (the arrows of a radio group marked
/// <c>data-omni-roving</c>, Enter and Space on an element marked <c>data-omni-key-button</c>), for a
/// component that loads that module for nothing else. Once per page is enough, the module keeps one
/// listener; a lost circuit is ignored.
/// </summary>
internal static class OmniPageKeys
{
    internal static async Task EnableAsync(IJSRuntime javaScript)
    {
        try
        {
            var module = await javaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Focus);
            await using (module.ConfigureAwait(true))
            {
                await module.InvokeVoidAsync("enablePageKeys");
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }
}
