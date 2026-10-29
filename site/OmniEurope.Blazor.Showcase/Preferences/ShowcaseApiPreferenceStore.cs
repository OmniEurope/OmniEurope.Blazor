using System.Net;
using System.Net.Http.Json;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Preferences;

/// <summary>The <see cref="HttpClient"/> of the showcase API, apart from the one that reads the site's own files.</summary>
public sealed class ShowcaseApiHttpClient(HttpClient http) : IDisposable
{
    public HttpClient Http { get; } = http ?? throw new ArgumentNullException(nameof(http));

    public void Dispose() => Http.Dispose();
}

/// <summary>
/// The <see cref="IOmniPreferenceStore"/> of the deployed showcase: the preferences of this browser's
/// anonymous visitor (<see cref="ShowcaseVisitor"/>), kept by the showcase API in PostgreSQL. Every
/// failure (the API unreachable, a refusal) is an exception, as the contract asks: OmniPreferences then
/// logs it, keeps the value in the browser and tries again at the next synchronization, so the showcase
/// works on with browser-only preferences while the API is away.
/// </summary>
public sealed class ShowcaseApiPreferenceStore(ShowcaseApiHttpClient api, ShowcaseVisitor visitor) : IOmniPreferenceStore
{
    /// <inheritdoc />
    public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = await RequestAsync(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/{Uri.EscapeDataString(key)}", cancellationToken);
        using var response = await api.Http.SendAsync(request, cancellationToken);
        // 204: the visitor never stored this key. A 404 is not that answer: it means a wrong base address.
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var preference = await response.Content.ReadFromJsonAsync<VisitorPreferenceDto>(cancellationToken)
            ?? throw new InvalidDataException("The preference API answered an empty preference.");
        return new OmniPreference(preference.Key, preference.Value, preference.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var path = since is { } from
            ? $"{VisitorPreferenceApi.Route}?since={Uri.EscapeDataString(from.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture))}"
            : VisitorPreferenceApi.Route;
        using var request = await RequestAsync(HttpMethod.Get, path, cancellationToken);
        using var response = await api.Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var changed = await response.Content.ReadFromJsonAsync<VisitorPreferenceDto[]>(cancellationToken)
            ?? throw new InvalidDataException("The preference API answered an empty list.");
        return [.. changed.Select(preference => new OmniPreference(preference.Key, preference.Value, preference.UpdatedAt))];
    }

    /// <inheritdoc />
    /// <remarks>The API answers the preference it now holds, with the date it kept (a future browser date becomes the server's time).</remarks>
    public async Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        using var request = await RequestAsync(HttpMethod.Put, $"{VisitorPreferenceApi.Route}/{Uri.EscapeDataString(preference.Key)}", cancellationToken);
        request.Content = JsonContent.Create(new SaveVisitorPreferenceRequest(preference.Value, preference.UpdatedAt));
        using var response = await api.Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var stored = await response.Content.ReadFromJsonAsync<VisitorPreferenceDto>(cancellationToken)
            ?? throw new InvalidDataException("The preference API answered a write without the stored preference.");
        return new OmniPreference(stored.Key, stored.Value, stored.UpdatedAt);
    }

    /// <inheritdoc />
    /// <remarks>Read from the browser alone, never from the API: the cache must open while the API is away.</remarks>
    public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => visitor.GetScopeAsync(cancellationToken);

    private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(VisitorPreferenceApi.VisitorHeader, await visitor.GetIdAsync(cancellationToken));
        return request;
    }
}
