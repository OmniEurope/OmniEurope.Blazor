using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// How often <see cref="OmniPreferences"/> calls the store's change feed (<see cref="IOmniPreferenceStore.GetChangedSinceAsync"/>)
/// on its own: a scripted five-minute session of 30 navigations and 40 tab switches must stay within one
/// synchronization per <see cref="OmniPreferencesOptions.MinSyncGap"/> plus the periodic one, while an explicit
/// <see cref="OmniPreferences.SyncAsync"/> always reaches the store. The browser, the store and the clock are
/// the fakes of <see cref="PreferenceFakes"/>; the real HTTP and browser path is not exercised here.
/// </summary>
public sealed class PreferenceSyncRateTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Session = TimeSpan.FromMinutes(5);
    private static readonly string[] LayoutKeys = ["appearance", "density", "language"];

    private readonly PreferenceBrowser _browser = new();
    private readonly MemoryPreferenceStore _store = new();
    private readonly PreferenceClock _time = new(Start);

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string Entry(string value) =>
        JsonSerializer.Serialize(new PreferenceCacheEntry(JsonSerializer.Serialize(value), Start.AddHours(-2), false));

    private OmniPreferences Open(OmniPreferencesOptions? options = null) =>
        new(_browser, _store, options ?? new OmniPreferencesOptions { SyncInterval = TimeSpan.FromMinutes(5) },
            NullLogger<OmniPreferences>.Instance, _time);

    private void SeedCache()
    {
        foreach (var key in LayoutKeys.Concat(Enumerable.Range(0, 8).Select(page => $"grid:{page}")))
        {
            _browser.Storage[$"omni:pref:u-7f3a:{key}"] = Entry("before");
        }
    }

    /// <summary>The events of the session in order: 30 navigations (every 10 s) and 40 tab switches (every 7.5 s).</summary>
    private static IEnumerable<(TimeSpan At, bool Navigation, int Index)> Timeline() =>
        Enumerable.Range(1, 30).Select(n => (TimeSpan.FromSeconds(10 * n), true, n))
            .Concat(Enumerable.Range(1, 40).Select(n => (TimeSpan.FromSeconds(7.5 * n), false, n)))
            .OrderBy(item => item.Item1).ThenBy(item => item.Item2);

    private async Task ReadPageAsync(OmniPreferences preferences, int page)
    {
        foreach (var key in LayoutKeys)
        {
            await preferences.GetAsync<string>(key, Token);
        }

        await preferences.GetAsync<string>($"grid:{page % 8}", Token);
        await preferences.WhenIdleAsync();
    }

    /// <summary>A single-page application: one service instance for the whole session, navigations are client-side.</summary>
    private async Task<OmniPreferences> RunClientNavigationAsync(OmniPreferencesOptions? options = null)
    {
        SeedCache();
        var preferences = Open(options);
        await ReadPageAsync(preferences, 0);
        var elapsed = TimeSpan.Zero;
        foreach (var (at, navigation, index) in Timeline())
        {
            _time.Advance(at - elapsed);
            elapsed = at;
            if (navigation)
            {
                await ReadPageAsync(preferences, index);
            }
            else
            {
                await _browser.Bridge!.OnVisible();
            }
        }

        // The periodic synchronization of the five-minute interval.
        _time.Advance(Session - elapsed);
        _time.Timers.Single().Fire();
        await preferences.WhenIdleAsync();
        return preferences;
    }

    [Fact]
    public async Task ClientNavigationAndTabSwitches_StayWithinOneSynchronizationPerGap()
    {
        await using var preferences = await RunClientNavigationAsync();

        // Bound: session / MinSyncGap (30 s) = 10, plus the periodic one.
        Assert.InRange(_store.ChangedSinceCalls.Count, 1, 11);
    }

    [Fact]
    public async Task FullPageLoadNavigationAndTabSwitches_StayWithinOneSynchronizationPerGap_AndAskTheStoreFewKeys()
    {
        // Every navigation is a new document: a new service instance, the browser cache is the only memory.
        SeedCache();
        var preferences = Open();
        await ReadPageAsync(preferences, 0);
        var elapsed = TimeSpan.Zero;
        foreach (var (at, navigation, index) in Timeline())
        {
            _time.Advance(at - elapsed);
            elapsed = at;
            if (navigation)
            {
                await preferences.DisposeAsync();
                preferences = Open();
                await ReadPageAsync(preferences, index);
            }
            else
            {
                await _browser.Bridge!.OnVisible();
            }
        }

        await preferences.DisposeAsync();

        // The first page asks the store for its four keys; the 30 pages that follow load within a gap of a synchronization.
        Assert.InRange(_store.Reads, 4, 4);
        Assert.InRange(_store.ChangedSinceCalls.Count, 1, 11);
    }

    [Fact]
    public async Task AnExplicitSync_AlwaysReachesTheStore_EvenRightAfterASynchronization()
    {
        await using var preferences = await RunClientNavigationAsync();
        var before = _store.ChangedSinceCalls.Count;

        // The periodic synchronization just ran: a tab switch is skipped, an explicit call is not.
        await _browser.Bridge!.OnVisible();
        Assert.Equal(before, _store.ChangedSinceCalls.Count);
        await preferences.SyncAsync(Token);
        await preferences.SyncAsync(Token);

        Assert.Equal(before + 2, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task ThePeriodicSynchronization_IsNotLimited()
    {
        SeedCache();
        await using var preferences = Open(new OmniPreferencesOptions { SyncInterval = TimeSpan.FromSeconds(5) });
        await preferences.GetAsync<string>("appearance", Token);
        var timer = _time.Timers.Single();

        timer.Fire();
        await preferences.WhenIdleAsync();
        timer.Fire();
        await preferences.WhenIdleAsync();

        // Each tick synchronizes whatever MinSyncGap says: the interval is the host's own choice.
        Assert.Equal(2, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task ATabSwitch_SynchronizesAgainOnceTheGapHasPassed_NotBefore()
    {
        SeedCache();
        await using var preferences = Open();
        await preferences.GetAsync<string>("appearance", Token);

        await _browser.Bridge!.OnVisible();
        _time.Advance(TimeSpan.FromSeconds(29));
        await _browser.Bridge.OnVisible();
        Assert.Single(_store.ChangedSinceCalls);

        _time.Advance(TimeSpan.FromSeconds(1));
        await _browser.Bridge.OnVisible();
        Assert.Equal(2, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task AZeroGap_RemovesTheLimit()
    {
        SeedCache();
        await using var preferences = Open(new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan, MinSyncGap = TimeSpan.Zero });
        await preferences.GetAsync<string>("appearance", Token);

        for (var switches = 0; switches < 40; switches++)
        {
            await _browser.Bridge!.OnVisible();
        }

        Assert.Equal(40, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task TriggersThatComeWhileASynchronizationRuns_ShareIt_WhateverTheGap()
    {
        SeedCache();
        _store.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var preferences = Open(new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan, MinSyncGap = TimeSpan.Zero });
        await preferences.GetAsync<string>("appearance", Token);

        var first = _browser.Bridge!.OnVisible();
        while (_store.ChangedSinceCalls.IsEmpty)
        {
            await Task.Delay(1, Token);
        }

        // The store is busy answering the first: the four that follow end at once, without a call of their own.
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _browser.Bridge.OnVisible()));
        _store.Hold.SetResult();
        await first;

        Assert.Single(_store.ChangedSinceCalls);
    }

    [Fact]
    public async Task AFailedSynchronization_IsNotRemembered_SoTheNextTabSwitchTriesAgain()
    {
        SeedCache();
        await using var preferences = Open();
        await preferences.GetAsync<string>("appearance", Token);
        _store.FailReads = true;

        await _browser.Bridge!.OnVisible();
        _store.FailReads = false;
        await _browser.Bridge.OnVisible();
        await _browser.Bridge.OnVisible();

        // The first failed, the second went through, the third found it done.
        Assert.Equal(2, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task AClockSetBack_DoesNotMakeTheLastSynchronizationLookRecent()
    {
        SeedCache();
        await using var preferences = Open();
        await preferences.GetAsync<string>("appearance", Token);

        await _browser.Bridge!.OnVisible();
        _time.Advance(TimeSpan.FromHours(-1));
        await _browser.Bridge.OnVisible();

        Assert.Equal(2, _store.ChangedSinceCalls.Count);
    }

    [Fact]
    public async Task AKeyFirstRead_SkipsTheStore_WhileTheCacheSynchronizedWithinTheGap_AndAsksItAfter()
    {
        SeedCache();
        await using var first = Open();
        await first.SyncAsync(Token);
        await first.GetAsync<string>("appearance", Token);
        await first.WhenIdleAsync();
        Assert.Equal(0, _store.Reads);

        _time.Advance(TimeSpan.FromSeconds(31));
        await using var later = Open();
        await later.GetAsync<string>("appearance", Token);
        await later.WhenIdleAsync();

        Assert.Equal(1, _store.Reads);
    }

    [Fact]
    public async Task AKeyNothingCaches_IsReadFromTheStoreWhateverTheGap()
    {
        SeedCache();
        await using var preferences = Open();
        await preferences.SyncAsync(Token);
        _store.Put("never-cached", "\"stored\"", Start);

        Assert.Equal("stored", await preferences.GetAsync<string>("never-cached", Token));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30000)]
    public void ANegativeGap_IsRefused(int milliseconds)
    {
        var options = new OmniPreferencesOptions { MinSyncGap = TimeSpan.FromMilliseconds(milliseconds) };

        Assert.Throws<ArgumentOutOfRangeException>(() => Open(options));
    }

    [Fact]
    public void TheDefaultGap_IsThirtySeconds_AndZeroIsAccepted()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), new OmniPreferencesOptions().MinSyncGap);
        Assert.NotNull(Open(new OmniPreferencesOptions { MinSyncGap = TimeSpan.Zero }));
    }
}
