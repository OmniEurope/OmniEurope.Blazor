using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The <see cref="OmniPreferences"/> a grid with a <c>StateKey</c> keeps its state in, over the
/// <see cref="PreferenceBrowser"/> (its <c>localStorage</c>) and, when asked, a <see cref="MemoryPreferenceStore"/>.
/// The grid's own script and the legacy <c>localStorage</c> calls stay on bUnit's runtime.
/// </summary>
internal sealed class GridPreferenceHarness
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public GridPreferenceHarness(IServiceCollection services, bool withStore = false)
    {
        Store = withStore ? new MemoryPreferenceStore() : null;
        services.AddScoped(_ => Preferences = new OmniPreferences(
            Browser,
            Store,
            new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance,
            TimeProvider.System));
    }

    public PreferenceBrowser Browser { get; } = new();

    public MemoryPreferenceStore? Store { get; }

    public OmniPreferences? Preferences { get; private set; }

    public string Scope => Store?.Scope ?? OmniPreferences.LocalScope;

    public static string PreferenceKey(string stateKey) => "omni.grid." + stateKey;

    public string StorageKey(string stateKey) => $"omni:pref:{Scope}:{PreferenceKey(stateKey)}";

    /// <summary>Puts a saved state in the browser cache, as a previous visit left it.</summary>
    public void Seed(string stateKey, string json, DateTimeOffset? updatedAt = null) =>
        Browser.Storage[StorageKey(stateKey)] = JsonSerializer.Serialize(new PreferenceCacheEntry(json, updatedAt ?? DateTimeOffset.UtcNow.AddMinutes(-5), false));

    /// <summary>The state cached in the browser, or null.</summary>
    public JsonElement? Saved(string stateKey) =>
        Browser.Storage.TryGetValue(StorageKey(stateKey), out var entry)
        && JsonSerializer.Deserialize<PreferenceCacheEntry>(entry)?.Value is { } json
            ? JsonDocument.Parse(json).RootElement
            : null;

    public static string Json(object value) => JsonSerializer.Serialize(value, Web);
}
