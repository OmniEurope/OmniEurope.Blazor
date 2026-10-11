using System.Text.Json;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Showcase.Preferences;

/// <summary>
/// DEMONSTRATION ONLY: an <see cref="IOmniPreferenceStore"/> that stands in for a host database on the
/// static showcase, which has no server. It is not a production store and must not be copied as one: a
/// real store keeps its rows in the application's database, per signed-in user (see
/// <c>docs/preferences.md</c>).
/// </summary>
/// <remarks>
/// Registered by <c>Program.cs</c> only when the page is opened with <c>?store=demo</c>, so the default
/// showcase keeps its preferences in the browser alone. Its rows live in <c>sessionStorage</c> under
/// their own prefix (<c>wwwroot/showcase-demo-store.js</c>), outside the <c>localStorage</c> cache of
/// <see cref="OmniPreferences"/>: a probe writes a row there to act as another device, and the package
/// must let the newer row win over its cache. Every visitor of the tab shares the one scope
/// <see cref="Scope"/>; there is no user.
/// </remarks>
public sealed class DemoPreferenceStore(IJSRuntime javaScript) : IOmniPreferenceStore
{
    /// <summary>The cache partition of the demonstration: its entries are <c>omni:pref:demo:&lt;key&gt;</c>.</summary>
    public const string Scope = "demo";

    /// <summary>
    /// What the address asks for, read before the host is built: null without <c>?store=demo</c>; with
    /// it, the periodic synchronization interval (<c>&amp;sync=&lt;seconds&gt;</c>), or
    /// <see cref="Timeout.InfiniteTimeSpan"/> when none is given. A missing or failing script means no
    /// demonstration store.
    /// </summary>
    /// <param name="javaScript">The in-process runtime of the WebAssembly page.</param>
    /// <returns>The synchronization interval to register the store with, or null to register none.</returns>
    public static TimeSpan? Requested(IJSInProcessRuntime javaScript)
    {
        ArgumentNullException.ThrowIfNull(javaScript);
        int seconds;
        try
        {
            seconds = javaScript.Invoke<int>("omniShowcaseDemoStore.request");
        }
        catch (JSException)
        {
            return null;
        }

        return seconds switch
        {
            < 0 => null,
            0 => Timeout.InfiniteTimeSpan,
            _ => TimeSpan.FromSeconds(seconds),
        };
    }

    /// <inheritdoc />
    public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Parse(await javaScript.InvokeAsync<string?>("omniShowcaseDemoStore.get", cancellationToken, key));

    /// <inheritdoc />
    public async Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var rows = await javaScript.InvokeAsync<string?[]>("omniShowcaseDemoStore.all", cancellationToken);
        return [.. rows.Select(Parse).OfType<OmniPreference>().Where(row => since is not { } from || row.UpdatedAt >= from)];
    }

    /// <inheritdoc />
    public async Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        await javaScript.InvokeVoidAsync("omniShowcaseDemoStore.save", cancellationToken, preference.Key, JsonSerializer.Serialize(preference));
    }

    /// <inheritdoc />
    public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(Scope);

    // A row is the JSON of an OmniPreference ({"Key","Value","UpdatedAt"}); one that does not read is ignored.
    private static OmniPreference? Parse(string? row)
    {
        if (string.IsNullOrEmpty(row))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OmniPreference>(row) is { Key.Length: > 0 } preference ? preference : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
