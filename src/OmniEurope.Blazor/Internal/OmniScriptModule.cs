namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Imports a JavaScript module for a component that may be disposed while the import is still pending.
/// </summary>
internal static class OmniScriptModule
{
    /// <summary>
    /// Imports the module at <paramref name="path"/>. A component disposed meanwhile (<paramref name="isDisposed"/>
    /// is true on arrival) releases the module at once rather than keeping a reference nothing will dispose, and
    /// the result is null so the caller stops. A lost circuit while releasing it is ignored.
    /// </summary>
    internal static async Task<IJSObjectReference?> ImportAsync(IJSRuntime javaScript, string path, Func<bool> isDisposed)
    {
        var module = await javaScript.InvokeAsync<IJSObjectReference>("import", path);
        if (!isDisposed())
        {
            return module;
        }

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone, and the module with it.
        }

        return null;
    }
}
