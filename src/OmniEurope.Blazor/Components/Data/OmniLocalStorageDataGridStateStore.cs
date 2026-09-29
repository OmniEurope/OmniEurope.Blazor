using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Default <see cref="IOmniDataGridStateStore"/>: reads and writes through the browser's
/// <c>localStorage</c>, so state is per device and never leaves it. Registered by
/// <c>AddOmniEuropeBlazor</c>; a host wanting a database-backed store instead registers its own
/// <see cref="IOmniDataGridStateStore"/> implementation in its place.
/// </summary>
public sealed class OmniLocalStorageDataGridStateStore(IJSRuntime javaScript) : IOmniDataGridStateStore
{
    /// <summary>Reads <paramref name="key"/> from <c>localStorage</c>, or returns <c>null</c> when it is not set.</summary>
    /// <param name="key">Storage key of the grid state.</param>
    /// <param name="cancellationToken">Cancels the JavaScript call.</param>
    /// <returns>The saved state, or <c>null</c>.</returns>
    public async Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
        => await javaScript.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);

    /// <summary>Writes <paramref name="state"/> under <paramref name="key"/> in <c>localStorage</c>, replacing any previous value.</summary>
    /// <param name="key">Storage key of the grid state.</param>
    /// <param name="state">Serialized grid state.</param>
    /// <param name="cancellationToken">Cancels the JavaScript call.</param>
    public async Task SaveAsync(string key, string state, CancellationToken cancellationToken = default)
        => await javaScript.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, state);
}
