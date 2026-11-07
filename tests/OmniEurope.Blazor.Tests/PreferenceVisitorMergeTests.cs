using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The merge of a signed-out visitor's browser preferences into the account at sign-in
/// (<see cref="OmniSignedOutPreferences.Browser"/>), in the cases the sign-in tests do not reach: a merge the store
/// fails, a visitor entry nothing is saved for, a store that answers a save with a later value, a visitor entry another tab
/// changes while it is merged; and a store that fails with <see cref="ObjectDisposedException"/> on its own account.
/// </summary>
public sealed class PreferenceVisitorMergeTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 8, 0, 0, TimeSpan.Zero);
    private const string VisitorKey = "omni:pref:visitor:theme";
    private const string AccountKey = "omni:pref:anne:theme";

    private readonly PreferenceBrowser _browser = new();
    private readonly PreferenceClock _time = new(Start);
    private readonly AccountStore _store = new();

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private OmniPreferences Open(ILogger<OmniPreferences>? logger = null, List<OmniPreferenceChangedEventArgs>? changes = null)
    {
        var preferences = new OmniPreferences(_browser, _store,
            new OmniPreferencesOptions { SignedOut = OmniSignedOutPreferences.Browser, SyncInterval = Timeout.InfiniteTimeSpan },
            logger ?? NullLogger<OmniPreferences>.Instance, _time);
        if (changes is not null)
        {
            preferences.Changed += change =>
            {
                lock (changes)
                {
                    changes.Add(change);
                }

                return Task.CompletedTask;
            };
        }

        return preferences;
    }

    private PreferenceCacheEntry Entry(string storageKey) => JsonSerializer.Deserialize<PreferenceCacheEntry>(_browser.Storage[storageKey])!;

    private static string Serialized(string? value, DateTimeOffset updatedAt, bool dirty) =>
        JsonSerializer.Serialize(new PreferenceCacheEntry(value, updatedAt, dirty));

    /// <summary>The visitor sets the theme, then the store names the account: nothing is merged yet.</summary>
    private async Task VisitorSetsThemeThenAccountSignsInAsync(OmniPreferences preferences)
    {
        _store.User = null;
        await preferences.SetAsync("theme", "aplat", Token);
        _store.User = "anne";
    }

    [Fact]
    public async Task AMergeThatTheStoreFails_IsLogged_KeepsTheVisitorsEntry_AndIsTriedAgainByTheNextCall()
    {
        var logger = new RecordingLogger();
        await using var preferences = Open(logger);
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);
        _store.Of("anne").FailReads = true;

        await preferences.ResetUserAsync(Token);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("could not be synchronized", StringComparison.Ordinal));
        Assert.Empty(_store.Of("anne").Saved);
        Assert.True(Entry(VisitorKey).Dirty);

        _store.Of("anne").FailReads = false;
        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        await preferences.WhenIdleAsync();

        var saved = Assert.Single(_store.Of("anne").Saved);
        Assert.Equal(("theme", "\"aplat\""), (saved.Key, saved.Value));
        Assert.False(Entry(VisitorKey).Dirty);
    }

    [Fact]
    public async Task AMergeIsOnlyMadeFromACacheThatHasAVisitorMirror()
    {
        _browser.Storage[VisitorKey] = Serialized("\"aplat\"", Start, dirty: true);
        var store = new MemoryPreferenceStore { Scope = "anne" };
        var synchronizer = new PreferenceSynchronizer(store, new PreferenceStoreAccess(store, NullLogger.Instance), new SemaphoreSlim(1, 1), _time, NullLogger.Instance);
        var changes = new List<OmniPreferenceChangedEventArgs>();

        await synchronizer.MergeVisitorAsync(new PreferenceCache(_browser, "anne"), changes, Token);

        Assert.Empty(changes);
        Assert.Equal(0, store.Reads);
        Assert.Empty(store.Saved);
        Assert.Equal(0, _browser.Writes);
        Assert.True(Entry(VisitorKey).Dirty);
    }

    [Fact]
    public async Task ARemovalTheVisitorMade_WhichTheAccountNeverHeld_SavesNothingAndIsConfirmed()
    {
        await using var preferences = Open();
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);
        _store.User = null;
        await preferences.RemoveAsync("theme", Token);
        Assert.True(Entry(VisitorKey).Dirty);

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Empty(_store.Of("anne").Saved);
        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        var visitor = Entry(VisitorKey);
        Assert.Null(visitor.Value);
        Assert.False(visitor.Dirty);
    }

    [Fact]
    public async Task AnAccountCacheThatIsLaterThanTheVisitorsValue_KeepsItsEntry_AndTheVisitorsIsConfirmedWithoutASave()
    {
        _browser.Storage[AccountKey] = Serialized("\"givre\"", Start.AddHours(1), dirty: false);
        await using var preferences = Open();
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);

        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Empty(_store.Of("anne").Saved);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start.AddHours(1), Dirty: false), Entry(AccountKey));
        Assert.Equal(new PreferenceCacheEntry("\"aplat\"", Start, Dirty: false), Entry(VisitorKey));
    }

    [Fact]
    public async Task AnAccountCacheThatIsOlderThanTheVisitorsValue_GivesWayToIt_AndTheValueIsSaved()
    {
        _browser.Storage[AccountKey] = Serialized("\"givre\"", Start.AddHours(-1), dirty: false);
        await using var preferences = Open();
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);

        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        var saved = Assert.Single(_store.Of("anne").Saved);
        Assert.Equal(("theme", "\"aplat\"", Start), (saved.Key, saved.Value, saved.UpdatedAt));
        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new PreferenceCacheEntry("\"aplat\"", Start, Dirty: false), Entry(AccountKey));
    }

    [Fact]
    public async Task ASaveOfTheVisitorsValue_TheStoreAnswersWithALaterOne_RaisesThatValueFromTheStore()
    {
        // Another device wrote the theme to the account between the merge's read (empty) and its save.
        var account = _store.Of("anne");
        account.KeepNewer = true;
        account.OnSave = saving => account.Put(saving.Key, "\"givre\"", Start.AddHours(1));
        var changes = new List<OmniPreferenceChangedEventArgs>();
        await using var preferences = Open(changes: changes);
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);

        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Contains(changes, change => change is { Key: "theme", Value: "\"givre\"", Origin: OmniPreferenceChangeOrigin.Store });
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start.AddHours(1), Dirty: false), Entry(AccountKey));
    }

    [Fact]
    public async Task AVisitorEntryAnotherTabChangedWhileItWasMerged_IsNotConfirmed()
    {
        _browser.Storage[AccountKey] = Serialized("\"givre\"", Start.AddHours(1), dirty: false);
        var changedByAnotherTab = Serialized("\"noir\"", Start.AddHours(2), dirty: true);
        var once = 0;
        _store.AfterGet = _ =>
        {
            if (Interlocked.Increment(ref once) == 1)
            {
                _browser.Storage[VisitorKey] = changedByAnotherTab;
            }
        };
        await using var preferences = Open();
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);

        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Equal(changedByAnotherTab, _browser.Storage[VisitorKey]);
        Assert.Empty(_store.Of("anne").Saved);
    }

    [Fact]
    public async Task AVisitorEntryAnotherTabRemovedWhileItWasMerged_IsNotWrittenBack()
    {
        _browser.Storage[AccountKey] = Serialized("\"givre\"", Start.AddHours(1), dirty: false);
        var once = 0;
        _store.AfterGet = _ =>
        {
            if (Interlocked.Increment(ref once) == 1)
            {
                _browser.Storage.TryRemove(VisitorKey, out var _);
            }
        };
        await using var preferences = Open();
        await VisitorSetsThemeThenAccountSignsInAsync(preferences);

        await preferences.ResetUserAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.DoesNotContain(VisitorKey, _browser.Storage.Keys);
        Assert.Empty(_store.Of("anne").Saved);
    }

    [Fact]
    public async Task AStoreWhoseContextIsDisposed_FailsTheSynchronizationWithItsOwnException_ItIsNotAnotherOpeningsRelease()
    {
        await using var preferences = new OmniPreferences(_browser, new DisposedContextStore(),
            new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan }, NullLogger<OmniPreferences>.Instance, _time);

        var failure = await Assert.ThrowsAsync<ObjectDisposedException>(() => preferences.SyncAsync(Token));

        Assert.Equal("DbContext", failure.ObjectName);
        Assert.Equal(1, _browser.Imports);
    }

    /// <summary>A store per account; no user answers <see cref="OmniPreferences.NoUserScope"/> and any call made then fails the test.</summary>
    private sealed class AccountStore : IOmniPreferenceStore
    {
        private readonly ConcurrentDictionary<string, MemoryPreferenceStore> _accounts = new(StringComparer.Ordinal);

        public string? User { get; set; } = "anne";

        /// <summary>Run after a read of the store answered, with the key read.</summary>
        public Action<string>? AfterGet { get; set; }

        public MemoryPreferenceStore Of(string user) => _accounts.GetOrAdd(user, name => new MemoryPreferenceStore { Scope = name });

        private MemoryPreferenceStore Current() =>
            Of(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."));

        public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var found = await Current().GetAsync(key, cancellationToken);
            AfterGet?.Invoke(key);
            return found;
        }

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Current().GetChangedSinceAsync(since, cancellationToken);

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Current().SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(User ?? OmniPreferences.NoUserScope);
    }

    /// <summary>A store over a scoped context that is already disposed when the synchronization lists its changes.</summary>
    private sealed class DisposedContextStore : IOmniPreferenceStore
    {
        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<OmniPreference?>(null);

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<OmniPreference>>(new ObjectDisposedException("DbContext"));

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Task.FromResult(preference);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("anne");
    }
}
