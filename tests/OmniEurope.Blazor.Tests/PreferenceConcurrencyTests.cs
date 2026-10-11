using System.Text.Json;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniPreferences"/> under concurrency: the host store is never called twice at once (a store
/// over a scoped <c>DbContext</c> would fail), a write made while a synchronization runs is not replaced by
/// the older store value, a slow <see cref="OmniPreferences.Changed"/> handler does not hold the next
/// synchronization, and an opening that failed after the import is released and tried again.
/// </summary>
public sealed class PreferenceConcurrencyTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private readonly PreferenceBrowser _browser = new();
    private readonly PreferenceClock _time = new(Start);
    private readonly RecordingLogger _logger = new();
    private readonly List<OmniPreferenceChangedEventArgs> _changes = [];
    private readonly List<OmniPreferences> _created = [];

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string Key(string key) => $"omni:pref:u-7f3a:{key}";

    private static string Entry(string? value, DateTimeOffset updatedAt, bool dirty = false) =>
        JsonSerializer.Serialize(new PreferenceCacheEntry(value, updatedAt, dirty));

    private PreferenceCacheEntry? Cached(string key) =>
        _browser.Storage.TryGetValue(Key(key), out var json) ? JsonSerializer.Deserialize<PreferenceCacheEntry>(json) : null;

    private OmniPreferences Create(IOmniPreferenceStore store, TimeSpan? interval = null)
    {
        var preferences = new OmniPreferences(_browser, store,
            new OmniPreferencesOptions { SyncInterval = interval ?? Timeout.InfiniteTimeSpan }, _logger, _time);
        preferences.Changed += change =>
        {
            lock (_changes)
            {
                _changes.Add(change);
            }

            return Task.CompletedTask;
        };
        _created.Add(preferences);
        return preferences;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(elapsed.Elapsed < Patience, "The condition was not met in time.");
            await Task.Delay(5, Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var preferences in _created)
        {
            await preferences.DisposeAsync();
        }
    }

    [Fact]
    public async Task TheStore_IsNeverCalledTwiceAtOnce_WhenSynchronizationsRunDuringWritesAndReads()
    {
        var memory = new MemoryPreferenceStore();
        memory.Put("density", "1", Start.AddHours(-1));
        var store = new ExclusivePreferenceStore(memory);
        var preferences = Create(store, TimeSpan.FromMinutes(1));
        await preferences.GetAsync<string>("theme", Token);
        await preferences.WhenIdleAsync();

        Task[] work =
        [
            Task.Run(() => preferences.SyncAsync(Token), Token),
            Task.Run(() => preferences.SetAsync("theme", "relief", Token), Token),
            Task.Run(() => preferences.SetAsync("width", "wide", Token), Token),
            Task.Run(() => preferences.GetAsync<int>("density", Token), Token),
            Task.Run(() => _time.Timers[0].Fire(), Token),
        ];
        await Task.WhenAll(work).WaitAsync(Patience, Token);
        await preferences.WhenIdleAsync().WaitAsync(Patience, Token);

        Assert.Equal(0, store.Overlaps);
        Assert.Empty(_logger.Entries);
        Assert.Equal("\"relief\"", memory["theme"]?.Value);
        Assert.Equal("\"wide\"", memory["width"]?.Value);
        Assert.Equal(1, await preferences.GetAsync<int>("density", Token));
    }

    [Fact]
    public async Task AWriteMadeWhileASynchronizationRuns_IsNotReplacedByTheOlderStoreValue()
    {
        var memory = new MemoryPreferenceStore();
        memory.Put("theme", "\"givre\"", Start.AddHours(-1));
        _browser.Storage[Key("theme")] = Entry("\"aplat\"", Start.AddHours(-2));
        // An unconfirmed entry: the synchronization reads the store for it after taking its snapshot.
        _browser.Storage[Key("width")] = Entry("\"wide\"", Start.AddHours(-3), dirty: true);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ExclusivePreferenceStore(memory)
        {
            BeforeGet = async key =>
            {
                if (key == "width")
                {
                    entered.TrySetResult();
                    await hold.Task;
                }
            },
        };
        var preferences = Create(store);

        var sync = preferences.SyncAsync(Token);
        await entered.Task.WaitAsync(Patience, Token);
        var write = preferences.SetAsync("theme", "relief", Token);
        await WaitUntilAsync(() => Cached("theme")?.Value == "\"relief\"");
        hold.SetResult();
        await Task.WhenAll(sync, write).WaitAsync(Patience, Token);

        Assert.Equal("\"relief\"", Cached("theme")!.Value);
        Assert.Equal("\"relief\"", memory["theme"]!.Value);
        Assert.Equal("\"wide\"", memory["width"]!.Value);
        Assert.DoesNotContain(_changes, change => change.Origin == OmniPreferenceChangeOrigin.Store);
        Assert.Equal(0, store.Overlaps);
    }

    [Fact]
    public async Task ASlowChangedHandler_DoesNotMakeTheNextSynchronizationSkip()
    {
        var memory = new MemoryPreferenceStore();
        memory.Put("grid", "1", Start);
        var preferences = Create(memory, TimeSpan.FromMinutes(1));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        preferences.Changed += async change =>
        {
            if (change.Key == "grid")
            {
                entered.TrySetResult();
                await release.Task;
            }
        };

        var first = preferences.SyncAsync(Token);
        try
        {
            await entered.Task.WaitAsync(Patience, Token);
            memory.Put("theme", "\"givre\"", Start.AddMinutes(1));

            _time.Timers[0].Fire();
            await preferences.WhenIdleAsync().WaitAsync(Patience, Token);

            Assert.Equal("\"givre\"", Cached("theme")?.Value);
            Assert.Contains(_changes, change => change.Key == "theme");
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(Patience, Token);
    }

    [Fact]
    public async Task AnOpeningThatFailsAfterTheImport_IsReleased_AndTheNextCallOpensAgain()
    {
        var memory = new MemoryPreferenceStore();
        var preferences = Create(memory, TimeSpan.FromMinutes(1));
        _browser.Failures["watch"] = new JSException("watch failed");

        await Assert.ThrowsAsync<JSException>(() => preferences.GetAsync<string>("theme", Token));

        Assert.Equal(1, _browser.Disposals);
        Assert.Empty(_time.Timers);

        _browser.Failures.Remove("watch");
        await preferences.SetAsync("theme", "relief", Token);

        Assert.Equal(2, _browser.Imports);
        Assert.Equal("relief", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal("\"relief\"", memory["theme"]!.Value);
        Assert.Single(_time.Timers);
    }
}
