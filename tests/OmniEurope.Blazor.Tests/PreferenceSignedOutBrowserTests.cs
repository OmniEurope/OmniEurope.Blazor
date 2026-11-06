using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A request of a client application (owner decision of 2026-11-06): with
/// <see cref="OmniSignedOutPreferences.Browser"/>, a signed-out visitor's preferences live in the browser alone,
/// are merged into the account at sign-in (the account's value wins), and the account's values stay in the
/// browser after sign-out. The browser is <c>omni-preferences.js</c> played over a dictionary and the store an
/// in-memory fake per account (the real HTTP and PostgreSQL path is covered by the showcase back tests).
/// <see cref="OmniSignedOutPreferences.Defaults"/> keeps its behavior.
/// </summary>
public sealed class PreferenceSignedOutBrowserTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 6, 8, 0, 0, TimeSpan.Zero);

    private readonly PreferenceBrowser _browser = new();
    private readonly PreferenceClock _time = new(Start);
    private readonly AccountStore _store = new();

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private OmniPreferences Open(OmniSignedOutPreferences mode, List<OmniPreferenceChangedEventArgs>? changes = null)
    {
        var preferences = new OmniPreferences(_browser, _store,
            new OmniPreferencesOptions { SignedOut = mode, SyncInterval = Timeout.InfiniteTimeSpan }, NullLogger<OmniPreferences>.Instance, _time);
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

    [Fact]
    public async Task SignedOut_AWriteSurvivesAReload_AndTheStoreIsNeverCalled()
    {
        _store.User = null;
        await using (var first = Open(OmniSignedOutPreferences.Browser))
        {
            await first.SetAsync("theme", "aplat", Token);
            Assert.Equal("aplat", await first.GetAsync<string>("theme", Token));
        }

        await using var reloaded = Open(OmniSignedOutPreferences.Browser);
        Assert.Equal("aplat", await reloaded.GetAsync<string>("theme", Token));
        await reloaded.SyncAsync(Token);
        await reloaded.WhenIdleAsync();

        Assert.Equal(0, _store.Calls);
        Assert.Contains(_browser.Storage.Keys, key => key.StartsWith("omni:pref:visitor:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SigningIn_WithAnEmptyAccount_SavesTheVisitorsValueToTheStore()
    {
        _store.User = null;
        await using var preferences = Open(OmniSignedOutPreferences.Browser);
        await preferences.SetAsync("theme", "aplat", Token);
        Assert.Equal(0, _store.Calls);

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);

        var saved = Assert.Single(_store.Of("anne").Saved);
        Assert.Equal(("theme", "\"aplat\""), (saved.Key, saved.Value));
        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task SigningInByAPageLoad_SavesTheVisitorsValueBeforeTheFirstRead()
    {
        _store.User = null;
        await using (var visitor = Open(OmniSignedOutPreferences.Browser))
        {
            await visitor.SetAsync("theme", "aplat", Token);
        }

        _store.User = "anne";
        await using var signedIn = Open(OmniSignedOutPreferences.Browser);

        Assert.Equal("aplat", await signedIn.GetAsync<string>("theme", Token));
        Assert.Equal("\"aplat\"", _store.Of("anne")["theme"]?.Value);
    }

    [Fact]
    public async Task SigningIn_WithAFilledAccount_TheAccountsValueReplacesTheVisitorsAndIsRaised()
    {
        _store.Of("anne").Put("theme", "\"givre\"", Start.AddDays(-1));
        _store.User = null;
        var changes = new List<OmniPreferenceChangedEventArgs>();
        await using var preferences = Open(OmniSignedOutPreferences.Browser, changes);
        await preferences.SetAsync("theme", "aplat", Token);
        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        changes.Clear();

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);

        var raised = Assert.Single(changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged);
        Assert.Equal(("theme", "\"givre\""), (raised.Key, raised.Value));
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_store.Of("anne").Saved);
        // The account's value also replaced the visitor's in this browser.
        Assert.Contains("givre", _browser.Storage["omni:pref:visitor:theme"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task SigningOut_TheAccountsValueStaysReadableLocally_AndLaterChangesStayLocal()
    {
        _store.Of("anne").Put("theme", "\"givre\"", Start.AddDays(-1));
        _store.User = "anne";
        var changes = new List<OmniPreferenceChangedEventArgs>();
        await using (var preferences = Open(OmniSignedOutPreferences.Browser, changes))
        {
            Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
            await preferences.WhenIdleAsync();
            var calls = _store.Calls;
            changes.Clear();

            _store.User = null;
            await preferences.ResetUserAsync(Token);

            Assert.Equal("\"givre\"", Assert.Single(changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged).Value);
            Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
            await preferences.SetAsync("theme", "aplat", Token);
            Assert.Equal(calls, _store.Calls);
            Assert.Equal("\"givre\"", _store.Of("anne")["theme"]?.Value);
        }

        // A reload of the signed-out page reads the same.
        await using var reloaded = Open(OmniSignedOutPreferences.Browser);
        Assert.Equal("aplat", await reloaded.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task AValueCopiedFromOneAccount_IsNeverSavedToAnotherAccountSigningInLater()
    {
        _store.Of("anne").Put("theme", "\"givre\"", Start.AddDays(-1));
        _store.User = "anne";
        await using var preferences = Open(OmniSignedOutPreferences.Browser);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        _store.User = null;
        await preferences.ResetUserAsync(Token);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));

        _store.User = "bruno";
        await preferences.ResetUserAsync(Token);

        Assert.Empty(_store.Of("bruno").Saved);
        Assert.Null(await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ADefaultsStore_KeepsNothingForTheVisitor_AndSavesNothingAtSignIn()
    {
        _store.User = null;
        await using var preferences = Open(OmniSignedOutPreferences.Defaults);
        await preferences.SetAsync("theme", "aplat", Token);
        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_browser.Storage);
        Assert.Equal(0, _store.Calls);

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_store.Of("anne").Saved);
        await using var reloaded = Open(OmniSignedOutPreferences.Defaults);
        _store.User = null;
        Assert.Null(await reloaded.GetAsync<string>("theme", Token));
    }

    [Fact]
    public void AnUndefinedMode_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Open((OmniSignedOutPreferences)42));

    [Fact]
    public async Task AUserScopeThatIsTheVisitorsOwn_IsRefused_InBrowserMode()
    {
        _store.Of("visitor").Put("theme", "\"x\"", Start);
        _store.User = "visitor";
        await using var preferences = Open(OmniSignedOutPreferences.Browser);

        await Assert.ThrowsAsync<InvalidOperationException>(() => preferences.GetAsync<string>("theme", Token));
    }

    /// <summary>A store per account; no user answers <see cref="OmniPreferences.NoUserScope"/> and any call made then fails the test.</summary>
    private sealed class AccountStore : IOmniPreferenceStore
    {
        private readonly ConcurrentDictionary<string, MemoryPreferenceStore> _accounts = new(StringComparer.Ordinal);

        public string? User { get; set; } = "anne";

        public int Calls;

        public MemoryPreferenceStore Of(string user) => _accounts.GetOrAdd(user, name => new MemoryPreferenceStore { Scope = name });

        private MemoryPreferenceStore Current()
        {
            Interlocked.Increment(ref Calls);
            return Of(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."));
        }

        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => Current().GetAsync(key, cancellationToken);

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Current().GetChangedSinceAsync(since, cancellationToken);

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Current().SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(User ?? OmniPreferences.NoUserScope);
    }
}
