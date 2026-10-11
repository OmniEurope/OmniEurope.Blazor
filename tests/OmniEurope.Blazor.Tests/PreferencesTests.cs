using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-019 lot 1: <see cref="OmniPreferences"/> answers from the browser cache, follows the host's
/// <see cref="IOmniPreferenceStore"/> (the store wins), keeps a write the store refused and replays it,
/// synchronizes removals, works without a store and while prerendering. The script itself
/// (<c>omni-preferences.js</c>) is played by <see cref="PreferenceBrowser"/>; its listeners and its
/// memory fallback need a browser.
/// </summary>
public sealed class PreferencesTests : OmniBunitContext
{
    private static readonly DateTimeOffset Start = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

    private readonly PreferenceBrowser _browser = new();
    private readonly MemoryPreferenceStore _store = new();
    private readonly PreferenceClock _time = new(Start);
    private readonly List<OmniPreferenceChangedEventArgs> _changes = [];
    private readonly RecordingLogger _logger = new();

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string Key(string scope, string key) => $"omni:pref:{scope}:{key}";

    private static string Entry(string? value, DateTimeOffset updatedAt, bool dirty = false) =>
        JsonSerializer.Serialize(new PreferenceCacheEntry(value, updatedAt, dirty));

    private PreferenceCacheEntry? Cached(string key, string scope = "u-7f3a") =>
        _browser.Storage.TryGetValue(Key(scope, key), out var json) ? JsonSerializer.Deserialize<PreferenceCacheEntry>(json) : null;

    private OmniPreferences Create(bool withStore = true, Action<OmniPreferencesOptions>? configure = null)
    {
        Services.AddSingleton<IJSRuntime>(_browser);
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniPreferences>>(_logger);
        if (withStore)
        {
            Services.AddSingleton<IOmniPreferenceStore>(_store);
        }

        if (configure is not null)
        {
            Services.Configure(configure);
        }

        var preferences = Services.GetRequiredService<OmniPreferences>();
        preferences.Changed += change =>
        {
            lock (_changes)
            {
                _changes.Add(change);
            }

            return Task.CompletedTask;
        };
        return preferences;
    }

    [Fact]
    public async Task Get_AnswersFromTheCacheFirst_ThenALaterStoreValueReplacesItAndRaisesChanged()
    {
        _browser.Storage[Key("u-7f3a", "theme")] = Entry("\"aplat\"", Start.AddHours(-2));
        _store.Put("theme", "\"givre\"", Start.AddHours(-1));
        var preferences = Create();

        var first = await preferences.GetAsync<string>("theme", Token);
        await preferences.WhenIdleAsync();

        Assert.Equal("aplat", first);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new OmniPreferenceChangedEventArgs("theme", "\"givre\"", OmniPreferenceChangeOrigin.Store), Assert.Single(_changes));
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start.AddHours(-1), false), Cached("theme"));
        // The store is asked once per key and circuit, not on every read.
        Assert.Equal(1, _store.Reads);
    }

    [Fact]
    public async Task Get_WhenTheStoreIsNotLater_KeepsTheCacheAndRaisesNothing()
    {
        _browser.Storage[Key("u-7f3a", "density")] = Entry("2", Start);
        _store.Put("density", "1", Start.AddHours(-1));
        var preferences = Create();

        Assert.Equal(2, await preferences.GetAsync<int>("density", Token));
        await preferences.WhenIdleAsync();

        Assert.Empty(_changes);
        Assert.Equal(2, await preferences.GetAsync<int>("density", Token));
    }

    [Fact]
    public async Task Get_WithNothingCached_ReadsTheStoreAtOnceAndCachesIt()
    {
        _store.Put("width", "\"wide\"", Start.AddDays(-3));
        var preferences = Create();

        Assert.Equal("wide", await preferences.GetAsync<string>("width", Token));
        Assert.Equal(new PreferenceCacheEntry("\"wide\"", Start.AddDays(-3), false), Cached("width"));
        Assert.Null(await preferences.GetAsync<string>("never-set", Token));
    }

    [Fact]
    public async Task Get_AValueThatNoLongerReadsAsTheType_GivesTheDefault()
    {
        _browser.Storage[Key("local", "layout")] = Entry("{\"columns\":3}", Start);
        _browser.Storage[Key("local", "broken")] = "not json";
        var preferences = Create(withStore: false);

        Assert.Equal(0, await preferences.GetAsync<int>("layout", Token));
        Assert.Null(await preferences.GetAsync<string>("broken", Token));
    }

    [Fact]
    public async Task Set_WritesTheCacheThenTheStore_DatedByTheHostClock()
    {
        var preferences = Create();

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Equal(new OmniPreference("theme", "\"relief\"", Start), Assert.Single(_store.Saved));
        Assert.Equal(new PreferenceCacheEntry("\"relief\"", Start, false), Cached("theme"));
        Assert.Equal(new OmniPreferenceChangedEventArgs("theme", "\"relief\"", OmniPreferenceChangeOrigin.Local), Assert.Single(_changes));
    }

    [Fact]
    public async Task Set_TheCurrentValue_WritesNothing()
    {
        var preferences = Create();
        await preferences.SetAsync("theme", "relief", Token);
        _time.Advance(TimeSpan.FromMinutes(5));

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Single(_store.Saved);
        Assert.Single(_changes);
        Assert.Equal(Start, Cached("theme")!.UpdatedAt);
    }

    [Fact]
    public async Task Set_WhenTheStoreFails_KeepsTheValueLocalAndDirty_AndTheNextSyncWritesIt()
    {
        var preferences = Create();
        _store.FailWrites = true;

        await preferences.SetAsync("theme", "octet", Token);

        Assert.Empty(_store.Saved);
        Assert.Equal(new PreferenceCacheEntry("\"octet\"", Start, true), Cached("theme"));
        Assert.Equal("octet", await preferences.GetAsync<string>("theme", Token));

        _store.FailWrites = false;
        await preferences.SyncAsync(Token);

        Assert.Equal(new OmniPreference("theme", "\"octet\"", Start), Assert.Single(_store.Saved));
        Assert.False(Cached("theme")!.Dirty);
    }

    [Fact]
    public async Task Sync_ADirtyValueOlderThanTheStoreOne_GivesWayToTheStore()
    {
        _browser.Storage[Key("u-7f3a", "theme")] = Entry("\"octet\"", Start.AddHours(-2), dirty: true);
        _store.Put("theme", "\"givre\"", Start.AddHours(-1));
        var preferences = Create();

        await preferences.SyncAsync(Token);

        Assert.Empty(_store.Saved);
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start.AddHours(-1), false), Cached("theme"));
        Assert.Equal(OmniPreferenceChangeOrigin.Store, Assert.Single(_changes).Origin);
    }

    [Fact]
    public async Task Sync_ADirtyValueTheStoreDidNotReportAsChanged_IsComparedWithTheStoreValueFirst()
    {
        // The store value predates the last synchronization, so it is not in the changes; it is still
        // later than the unconfirmed one and wins.
        _browser.Storage["omni:pref-sync:u-7f3a"] = Start.AddMinutes(-30).ToString("O");
        _browser.Storage[Key("u-7f3a", "theme")] = Entry("\"octet\"", Start.AddHours(-3), dirty: true);
        _store.Put("theme", "\"givre\"", Start.AddHours(-1));
        var preferences = Create();

        await preferences.SyncAsync(Token);

        Assert.Equal(Start.AddMinutes(-30), Assert.Single(_store.ChangedSinceCalls));
        Assert.Empty(_store.Saved);
        Assert.Equal("\"givre\"", Cached("theme")!.Value);
    }

    [Fact]
    public async Task Sync_PullsTheStoreChanges_AndRemembersTheLatestStoreDate()
    {
        _browser.Storage[Key("u-7f3a", "theme")] = Entry("\"aplat\"", Start.AddDays(-1));
        _browser.Storage[Key("u-7f3a", "density")] = Entry("2", Start);
        _store.Put("theme", "\"relief\"", Start.AddHours(-2));
        _store.Put("density", "1", Start.AddHours(-3));
        _store.Put("font", "\"inter\"", Start.AddHours(-4));
        var preferences = Create();

        await preferences.SyncAsync(Token);
        await preferences.SyncAsync(Token);

        Assert.Equal("relief", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(2, await preferences.GetAsync<int>("density", Token));
        Assert.Equal("inter", await preferences.GetAsync<string>("font", Token));
        Assert.Equal(["font", "theme"], _changes.Select(change => change.Key).Order(StringComparer.Ordinal));
        Assert.Equal([null, Start.AddHours(-2)], _store.ChangedSinceCalls);
        Assert.Equal(Start.AddHours(-2).ToString("O"), _browser.Storage["omni:pref-sync:u-7f3a"]);
    }

    [Fact]
    public async Task Sync_WhenTheStoreCannotListItsChanges_FailsAndKeepsTheUnconfirmedValues()
    {
        var preferences = Create();
        _store.FailWrites = true;
        await preferences.SetAsync("theme", "octet", Token);
        _store.FailReads = true;

        await Assert.ThrowsAsync<TimeoutException>(() => preferences.SyncAsync(Token));

        Assert.True(Cached("theme")!.Dirty);
    }

    [Fact]
    public async Task Remove_WritesATombstoneOnBothSides()
    {
        var preferences = Create();
        await preferences.SetAsync("theme", "relief", Token);
        _time.Advance(TimeSpan.FromMinutes(1));

        await preferences.RemoveAsync("theme", Token);

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new OmniPreference("theme", null, Start.AddMinutes(1)), _store["theme"]);
        Assert.Equal(new PreferenceCacheEntry(null, Start.AddMinutes(1), false), Cached("theme"));
        Assert.Equal(new OmniPreferenceChangedEventArgs("theme", null, OmniPreferenceChangeOrigin.Local), _changes[^1]);
    }

    [Fact]
    public async Task Sync_ATombstoneFromTheStore_RemovesTheCachedValue()
    {
        _browser.Storage[Key("u-7f3a", "theme")] = Entry("\"relief\"", Start.AddHours(-2));
        _store.Put("theme", null, Start.AddHours(-1));
        var preferences = Create();

        await preferences.SyncAsync(Token);

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new OmniPreferenceChangedEventArgs("theme", null, OmniPreferenceChangeOrigin.Store), Assert.Single(_changes));
    }

    [Fact]
    public async Task WithoutAStore_TheBrowserAloneKeepsThePreferences_UnderTheLocalScope()
    {
        var preferences = Create(withStore: false);

        await preferences.SetAsync("theme", "givre", Token);
        await preferences.SyncAsync(Token);

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start, false), Cached("theme", "local"));
        Assert.Equal(["omni:pref:local:"], _browser.WatchedPrefixes);
        Assert.Empty(_time.Timers);
    }

    [Fact]
    public async Task WhilePrerendering_ReadsAndWritesTheStore_AndLeavesTheBrowserAlone()
    {
        _browser.ImportFailure = new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
        _store.Put("theme", "\"givre\"", Start.AddHours(-1));
        var preferences = Create();

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        await preferences.SetAsync("density", 1, Token);
        await preferences.SyncAsync(Token);

        Assert.Equal(new OmniPreference("density", "1", Start), Assert.Single(_store.Saved));
        Assert.Empty(_browser.Storage);
        Assert.Empty(_store.ChangedSinceCalls);

        // Once JavaScript answers, the cache opens.
        _browser.ImportFailure = null;
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.NotNull(Cached("theme"));
    }

    [Fact]
    public async Task WhilePrerenderingWithoutAStore_ReadsNothing()
    {
        _browser.ImportFailure = new InvalidOperationException("prerendering");
        var preferences = Create(withStore: false);

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        await preferences.SetAsync("theme", "givre", Token);
        Assert.Empty(_browser.Storage);
    }

    [Fact]
    public async Task TheStoreScope_PartitionsTheBrowserCache()
    {
        _browser.Storage[Key("u-other", "theme")] = Entry("\"aplat\"", Start);
        var preferences = Create();

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Equal("\"aplat\"", JsonSerializer.Deserialize<PreferenceCacheEntry>(_browser.Storage[Key("u-other", "theme")])!.Value);
        Assert.Equal("\"relief\"", Cached("theme")!.Value);
        Assert.Equal(["omni:pref:u-7f3a:"], _browser.WatchedPrefixes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a:b")]
    public async Task AScopeThatIsBlankOrHoldsAColon_IsRefused(string scope)
    {
        _store.Scope = scope;
        var preferences = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task AStoreThatCannotGiveItsScope_LeavesTheStoreAlone()
    {
        var store = new ScopelessStore(_store);
        Services.AddSingleton<IOmniPreferenceStore>(store);
        _store.Put("theme", "\"givre\"", Start);
        var preferences = Create(withStore: false);

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_browser.Storage);
    }

    [Fact]
    public async Task TheTimer_SynchronizesEveryInterval()
    {
        var preferences = Create(configure: options => options.SyncInterval = TimeSpan.FromSeconds(30));
        await preferences.GetAsync<string>("theme", Token);
        var timer = Assert.Single(_time.Timers);
        Assert.Equal(TimeSpan.FromSeconds(30), timer.Period);
        _store.Put("theme", "\"givre\"", Start);

        timer.Fire();
        await preferences.WhenIdleAsync();

        Assert.Single(_store.ChangedSinceCalls);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(OmniPreferenceChangeOrigin.Store, Assert.Single(_changes).Origin);
    }

    [Fact]
    public async Task AnInfiniteInterval_StartsNoTimer_AndANonPositiveOneIsRefused()
    {
        var preferences = Create(configure: options => options.SyncInterval = Timeout.InfiniteTimeSpan);
        await preferences.GetAsync<string>("theme", Token);
        Assert.Empty(_time.Timers);

        var services = new ServiceCollection().AddSingleton<IJSRuntime>(_browser).AddOmniEuropeBlazor();
        services.Configure<OmniPreferencesOptions>(options => options.SyncInterval = TimeSpan.Zero);
        await using var provider = services.BuildServiceProvider();
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<OmniPreferences>());
    }

    [Fact]
    public async Task TheTabBecomingVisible_Synchronizes()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);
        _store.Put("theme", "\"givre\"", Start);

        await _browser.Bridge!.OnVisible();

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Single(_store.ChangedSinceCalls);
    }

    [Fact]
    public async Task AnotherTabWritingAPreferenceOfTheScope_RaisesChanged()
    {
        var preferences = Create(withStore: false);
        await preferences.GetAsync<string>("theme", Token);
        _browser.Storage[Key("local", "theme")] = Entry("\"relief\"", Start);

        await _browser.Bridge!.OnStored(Key("local", "theme"));
        await _browser.Bridge.OnStored(Key("other", "theme"));

        Assert.Equal(new OmniPreferenceChangedEventArgs("theme", "\"relief\"", OmniPreferenceChangeOrigin.OtherTab), Assert.Single(_changes));
    }

    [Fact]
    public async Task AFailingHandler_DoesNotStopTheOthersNorTheWrite()
    {
        var preferences = Create();
        preferences.Changed += _ => throw new InvalidOperationException("handler bug");
        var reached = false;
        preferences.Changed += _ =>
        {
            reached = true;
            return Task.CompletedTask;
        };

        await preferences.SetAsync("theme", "relief", Token);

        Assert.True(reached);
        Assert.Single(_store.Saved);
    }

    [Fact]
    public async Task Dispose_StopsTheTimerAndTheListeners()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);

        await preferences.DisposeAsync();
        await preferences.DisposeAsync();

        Assert.True(Assert.Single(_time.Timers).Disposed);
        Assert.Equal([7], _browser.Unwatched);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ABlankKey_IsRefused()
    {
        var preferences = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => preferences.GetAsync<string>(" ", Token));
        await Assert.ThrowsAsync<ArgumentException>(() => preferences.SetAsync("", 1, Token));
    }

    [Fact]
    public async Task AddOmniEuropePreferences_RegistersTheHostStoreScopedAndItsOptions()
    {
        var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(_browser)
            .AddOmniEuropePreferences<MemoryPreferenceStore>(options => options.SyncInterval = TimeSpan.FromMinutes(5));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        Assert.Equal(ServiceLifetime.Scoped, services.Single(service => service.ServiceType == typeof(IOmniPreferenceStore)).Lifetime);
        Assert.IsType<MemoryPreferenceStore>(scope.ServiceProvider.GetRequiredService<IOmniPreferenceStore>());
        Assert.Equal(TimeSpan.FromMinutes(5), provider.GetRequiredService<IOptions<OmniPreferencesOptions>>().Value.SyncInterval);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OmniPreferences>());
        Assert.Equal(TimeSpan.FromMinutes(1), new OmniPreferencesOptions().SyncInterval);
    }

    [Fact]
    public async Task Set_WhenTheStoreFails_LogsAWarningNamingTheKey()
    {
        var preferences = Create();
        _store.FailWrites = true;

        await preferences.SetAsync("theme", "octet", Token);

        Assert.Contains(_logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning && entry.Message.Contains("theme", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_WhenTheStoreCannotBeRead_LogsAndAnswersTheDefault()
    {
        var preferences = Create();
        _store.FailReads = true;

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        Assert.Contains(_logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning && entry.Message.Contains("theme", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WhilePrerendering_AKeyTheStoreNeverHeld_ReadsNothing()
    {
        _browser.ImportFailure = new InvalidOperationException("prerendering");
        var preferences = Create();

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ALostCircuit_LeavesTheBrowserCacheClosed()
    {
        _browser.ImportFailure = new JSDisconnectedException("gone");
        _store.Put("theme", "\"givre\"", Start);
        var preferences = Create();

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_browser.Storage);
    }

    [Fact]
    public async Task Sync_SkipsMalformedRows_AndKeepsTheLatestOfDuplicates()
    {
        _store.ExtraChanges.Add(null!);
        _store.ExtraChanges.Add(new OmniPreference(" ", "1", Start));
        _store.ExtraChanges.Add(new OmniPreference("theme", "\"aplat\"", Start.AddHours(-3)));
        _store.ExtraChanges.Add(new OmniPreference("theme", "\"relief\"", Start.AddHours(-1)));
        _store.ExtraChanges.Add(new OmniPreference("theme", "\"octet\"", Start.AddHours(-2)));
        var preferences = Create();

        await preferences.SyncAsync(Token);

        Assert.Equal("relief", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(Start.AddHours(-1).ToString("O"), _browser.Storage["omni:pref-sync:u-7f3a"]);
        Assert.DoesNotContain(_browser.Storage.Keys, key => key == Key("u-7f3a", " "));
    }

    [Fact]
    public async Task Set_AWriteReplacedWhileTheStoreSaved_StaysPending()
    {
        var preferences = Create();
        _store.OnSave = saved => _browser.Storage[Key("u-7f3a", saved.Key)] = Entry("\"later\"", Start.AddSeconds(1), dirty: true);

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Equal(new PreferenceCacheEntry("\"later\"", Start.AddSeconds(1), true), Cached("theme"));
    }

    [Fact]
    public async Task Set_AnEntryClearedWhileTheStoreSaved_IsNotWrittenBack()
    {
        var preferences = Create();
        _store.OnSave = saved => _browser.Storage.TryRemove(Key("u-7f3a", saved.Key), out _);

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Null(Cached("theme"));
        Assert.Single(_store.Saved);
    }

    [Fact]
    public async Task TheTimer_WhileASynchronizationRuns_StartsNoSecondOne()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);
        _store.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = preferences.SyncAsync(Token);

        _time.Timers[0].Fire();
        await preferences.WhenIdleAsync();
        _store.Hold.SetResult();
        await running;

        Assert.Single(_store.ChangedSinceCalls);
    }

    [Fact]
    public async Task TheTimer_WhenTheStoreFails_LogsAndKeepsRunning()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);
        _store.FailReads = true;

        _time.Timers[0].Fire();
        await preferences.WhenIdleAsync();
        _store.FailReads = false;
        _store.Put("theme", "\"givre\"", Start);
        _time.Timers[0].Fire();
        await preferences.WhenIdleAsync();

        Assert.Contains(_logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task Dispose_CancelsTheSynchronizationInFlight_WithoutLogging()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);
        _store.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _time.Timers[0].Fire();
        while (_store.ChangedSinceCalls.IsEmpty)
        {
            await Task.Delay(5, Token);
        }

        var idle = preferences.WhenIdleAsync();
        await preferences.DisposeAsync();
        await idle.WaitAsync(TimeSpan.FromSeconds(5), Token);

        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task AnotherTabNotification_OnALostCircuit_IsDropped()
    {
        var preferences = Create(withStore: false);
        await preferences.GetAsync<string>("theme", Token);
        _browser.Failures["get"] = new JSDisconnectedException("gone");

        await _browser.Bridge!.OnStored(Key("local", "theme"));
        await _browser.Bridge.OnStored("omni:pref:local:");

        Assert.Empty(_changes);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Dispose_OnALostCircuit_StillCompletes()
    {
        var preferences = Create();
        await preferences.GetAsync<string>("theme", Token);
        _browser.Failures["unwatch"] = new JSDisconnectedException("gone");

        await preferences.DisposeAsync();

        Assert.True(_time.Timers[0].Disposed);
    }

    [Fact]
    public async Task WithoutHandlers_AWriteRaisesNothingAndStillReachesTheStore()
    {
        Services.AddSingleton<IJSRuntime>(_browser);
        Services.AddSingleton<IOmniPreferenceStore>(_store);
        var preferences = Services.GetRequiredService<OmniPreferences>();

        await preferences.SetAsync("theme", "relief", Token);

        Assert.Single(_store.Saved);
    }

    [Fact]
    public void AddOmniEuropePreferences_WithoutConfiguration_KeepsTheDefaultInterval()
    {
        var services = new ServiceCollection().AddOmniEuropePreferences<MemoryPreferenceStore>();
        using var provider = services.BuildServiceProvider();

        Assert.Equal(TimeSpan.FromMinutes(1), provider.GetRequiredService<IOptions<OmniPreferencesOptions>>().Value.SyncInterval);
    }

    /// <summary>A store whose scope lookup fails, as one whose database is down at the first call.</summary>
    private sealed class ScopelessStore(MemoryPreferenceStore inner) : IOmniPreferenceStore
    {
        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => inner.GetAsync(key, cancellationToken);

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            inner.GetChangedSinceAsync(since, cancellationToken);

        public Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => inner.SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromException<string>(new TimeoutException("database down"));
    }
}
