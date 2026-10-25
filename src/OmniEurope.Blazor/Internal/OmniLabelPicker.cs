using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns on the focus module's answer to a click on the label of a native <c>select</c>: the list opens
/// (<c>showPicker()</c>, called by the script on the click itself, which a .NET round trip would be too late
/// for). The module keeps one listener on the document for every select, so asking again changes nothing; a
/// lost circuit is ignored.
/// </summary>
internal static class OmniLabelPicker
{
    internal static async Task EnableAsync(IJSRuntime javaScript)
    {
        try
        {
            var module = await OmniScriptModule.ImportAsync(javaScript, OmniModules.Focus);
            await using (module.ConfigureAwait(true))
            {
                await module.InvokeVoidAsync("enableLabelPicker");
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }
}
