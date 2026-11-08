using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The send of the entries a reload left unconfirmed (<see cref="PreferenceSynchronizer.SendPendingAsync"/>), driven
/// directly: through <see cref="OmniPreferences"/> the read made at opening may apply the store's value first, so
/// whether the send itself reports the change depends on which task runs first.
/// </summary>
public sealed class PreferencePendingSendTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 8, 0, 0, TimeSpan.Zero);
    private const string CacheKey = "omni:pref:u-7f3a:textSize";

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task APendingValueOlderThanTheStoreOne_GivesWayToIt_AndTheChangeIsReported()
    {
        var browser = new PreferenceBrowser();
        var pending = new PreferenceCacheEntry("6", Start.AddMinutes(-10), Dirty: true);
        browser.Storage[CacheKey] = JsonSerializer.Serialize(pending);
        var store = new MemoryPreferenceStore();
        store.Put("textSize", "4", Start.AddMinutes(-5));
        var synchronizer = new PreferenceSynchronizer(store, new PreferenceStoreAccess(store, NullLogger.Instance), new SemaphoreSlim(1, 1), new PreferenceClock(Start), NullLogger.Instance);
        var changes = new List<OmniPreferenceChangedEventArgs>();

        await synchronizer.SendPendingAsync(new PreferenceCache(browser, "u-7f3a"), new() { ["textSize"] = pending }, changes, Token);

        var change = Assert.Single(changes);
        Assert.Equal(("textSize", "4", OmniPreferenceChangeOrigin.Store), (change.Key, change.Value, change.Origin));
        Assert.Empty(store.Saved);
        Assert.Equal(new PreferenceCacheEntry("4", Start.AddMinutes(-5), Dirty: false), JsonSerializer.Deserialize<PreferenceCacheEntry>(browser.Storage[CacheKey]));
    }
}
