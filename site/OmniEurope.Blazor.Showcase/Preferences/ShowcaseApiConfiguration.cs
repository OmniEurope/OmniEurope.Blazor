using Microsoft.Extensions.Configuration;

namespace OmniEurope.Blazor.Showcase.Preferences;

/// <summary>
/// STD-CFGOVERRIDE: the one chain that resolves the API base address. <c>wwwroot/appsettings.json</c>
/// holds the deployed value (the front container writes it from <c>API_BASE_URL</c>; empty in the
/// repository), and <c>wwwroot/appsettings.Local.json</c>, written by the local launcher and never
/// committed or published, overrides it. <see cref="BaseAddress"/> is the only reader.
/// </summary>
public static class ShowcaseApiConfiguration
{
    /// <summary>The configuration key of the API base address.</summary>
    public const string ApiBaseUrlKey = "ApiBaseUrl";

    /// <summary>The local override the launcher writes beside <c>appsettings.json</c>.</summary>
    public const string LocalSettingsFile = "appsettings.Local.json";

    /// <summary>How long the start-up waits for the local override before running without it.</summary>
    public static readonly TimeSpan LocalSettingsTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// STD-BOOTFETCH: adds the local override when the site serves one. Called in development only (a
    /// deployment never has the file); there too a 404, no network or a slow answer lets the start-up go
    /// on without it.
    /// </summary>
    /// <param name="configuration">The configuration of the WebAssembly host builder.</param>
    /// <param name="siteBaseAddress">The address the site is served from.</param>
    public static async Task AddLocalOverrideAsync(IConfigurationBuilder configuration, Uri siteBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        using var client = new HttpClient { BaseAddress = siteBaseAddress, Timeout = LocalSettingsTimeout };
        try
        {
            using var response = await client.GetAsync(LocalSettingsFile);
            // A host that answers every path with index.html (an SPA fallback) does not serve the file.
            if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                configuration.AddJsonStream(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            }
        }
        catch (HttpRequestException exception) when (exception.StatusCode is null)
        {
            Console.Out.WriteLine($"{LocalSettingsFile} could not be fetched ({exception.Message}); the site runs on appsettings.json.");
        }
        catch (TaskCanceledException)
        {
            Console.Out.WriteLine($"{LocalSettingsFile} did not answer within {LocalSettingsTimeout.TotalSeconds} s; the site runs on appsettings.json.");
        }
    }

    /// <summary>
    /// The API base address, with a trailing slash, or null when none is configured: the preferences then
    /// stay in the browser, as on a static copy of the site. A value that is not an absolute http(s) address
    /// is a misconfiguration, reported on the console as an error rather than taken for an absent API.
    /// </summary>
    /// <param name="configuration">The built configuration.</param>
    public static Uri? BaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration[ApiBaseUrlKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value.EndsWith('/') ? value : value + "/", UriKind.Absolute, out var address)
            || (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp))
        {
            Console.Error.WriteLine($"{ApiBaseUrlKey} '{value}' is not an absolute http(s) address: the preferences stay in this browser.");
            return null;
        }

        return address;
    }
}
