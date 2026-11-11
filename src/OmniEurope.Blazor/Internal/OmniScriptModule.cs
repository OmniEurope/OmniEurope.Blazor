namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The one place that imports a JavaScript module for a component (<c>import</c> through the JS runtime).
/// A guard test refuses any other <c>"import"</c> call in the package.
/// </summary>
/// <remarks>
/// What the loader adds to a plain import is a single new attempt after a <see cref="JSException"/> (a module
/// request that failed on the way, a transient network or script error). Everything else reaches the caller
/// unchanged, so each call site keeps the handling it always had: <see cref="JSDisconnectedException"/> (the
/// circuit is gone) and <see cref="OperationCanceledException"/> (the component's own disposal) are never
/// retried, and a second <see cref="JSException"/> is thrown as it came, for the caller to degrade or surface.
/// </remarks>
internal static class OmniScriptModule
{
    /// <summary>
    /// Imports the module at <paramref name="path"/>, once more after a <see cref="JSException"/>. The second
    /// failure and a lost circuit propagate untouched.
    /// </summary>
    internal static ValueTask<IJSObjectReference> ImportAsync(IJSRuntime javaScript, string path)
        => ImportAsync(javaScript, path, CancellationToken.None);

    /// <summary>
    /// Imports the module at <paramref name="path"/> under <paramref name="cancellationToken"/> (the owner's
    /// disposal), once more after a <see cref="JSException"/>. The second failure, a lost circuit and a
    /// cancellation propagate untouched, and a token cancelled meanwhile is never retried.
    /// </summary>
    internal static async ValueTask<IJSObjectReference> ImportAsync(
        IJSRuntime javaScript, string path, CancellationToken cancellationToken)
    {
        try
        {
            return await InvokeImportAsync(javaScript, path, cancellationToken);
        }
        catch (JSException) when (!cancellationToken.IsCancellationRequested)
        {
            return await InvokeImportAsync(javaScript, path, cancellationToken);
        }
    }

    private static ValueTask<IJSObjectReference> InvokeImportAsync(IJSRuntime javaScript, string path, CancellationToken cancellationToken)
        => cancellationToken.CanBeCanceled
            ? javaScript.InvokeAsync<IJSObjectReference>("import", cancellationToken, path)
            : javaScript.InvokeAsync<IJSObjectReference>("import", path);

    /// <summary>
    /// Imports the module at <paramref name="path"/> like <see cref="ImportAsync(IJSRuntime, string, CancellationToken)"/>.
    /// A component disposed meanwhile (<paramref name="isDisposed"/> is true on arrival) releases the module at
    /// once rather than keeping a reference nothing will dispose, and the result is null so the caller stops. A
    /// lost circuit while releasing it is ignored.
    /// </summary>
    internal static async Task<IJSObjectReference?> ImportAsync(IJSRuntime javaScript, string path, Func<bool> isDisposed)
    {
        var module = await ImportAsync(javaScript, path);
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
