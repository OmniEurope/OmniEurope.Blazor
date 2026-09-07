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
    public async Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
        => await javaScript.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);

    public async Task SaveAsync(string key, string state, CancellationToken cancellationToken = default)
        => await javaScript.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, state);
}
