using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A request of a client application (recette R1-66): a store with no signed-in user yet answers
/// <see cref="OmniPreferences.NoUserScope"/> and the preferences read as their defaults, quietly; a change of
/// user is taken without reloading the page by <see cref="OmniPreferences.ResetUserAsync"/>, which reads every
/// preference of the circuit again for the new user and raises it, so the appearance state follows.
/// </summary>
public sealed class PreferenceUserTests : OmniBunitContext
{
    private static readonly DateTimeOffset Start = new(2026, 10, 13, 8, 0, 0, TimeSpan.Zero);

    private readonly PreferenceBrowser _browser = new();
    private readonly UserStore _store = new();
    private readonly RecordingLogger _logger = new();
    private readonly List<OmniPreferenceChangedEventArgs> _changes = [];

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private OmniPreferences Create()
    {
        Services.AddSingleton<IJSRuntime>(_browser);
        Services.AddSingleton<TimeProvider>(new PreferenceClock(Start));
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniPreferences>>(_logger);
        Services.AddSingleton<IOmniPreferenceStore>(_store);
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
    public async Task WithNoUserSignedIn_PreferencesReadAsDefaults_WithoutCallingTheStoreOrLogging()
    {
        _store.User = null;
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        var preferences = Create();

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        await preferences.SyncAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Equal(0, _store.Calls);
        Assert.Empty(_logger.Entries);
        Assert.Empty(_browser.Storage);
    }

    [Fact]
    public async Task AValueWrittenWithNoUser_StaysInTheCircuitOnly()
    {
        _store.User = null;
        var preferences = Create();

        await preferences.SetAsync("theme", "aplat", Token);
        await preferences.SetAsync("theme", "aplat", Token);

        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        Assert.Single(_changes, change => change.Origin == OmniPreferenceChangeOrigin.Local);
        Assert.Empty(_browser.Storage);
        Assert.Equal(0, _store.Calls);
    }

    [Fact]
    public async Task SigningIn_ReadsEveryPreferenceAgain_ForTheNewUser_AndDropsTheSignedOutValues()
    {
        _store.User = null;
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        var preferences = Create();
        await preferences.SetAsync("theme", "aplat", Token);
        Assert.Null(await preferences.GetAsync<string>("density", Token));

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);

        var raised = _changes.Where(change => change.Origin == OmniPreferenceChangeOrigin.UserChanged).OrderBy(change => change.Key, StringComparer.Ordinal).ToArray();
        Assert.Equal(["density", "theme"], raised.Select(change => change.Key));
        Assert.Null(raised[0].Value);
        Assert.Equal("\"givre\"", raised[1].Value);
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        // The signed-out value is not carried into the user's preferences.
        Assert.DoesNotContain(_store.Of("anne").Saved, saved => saved.Value == "\"aplat\"");
    }

    [Fact]
    public async Task ChangingAccount_ReadsTheOtherUsersPreferences_AndLeavesTheFormerCacheAlone()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        _store.Of("bruno").Put("theme", "\"neon\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        await preferences.WhenIdleAsync();
        var watched = _browser.WatchedPrefixes.Count;

        _store.User = "bruno";
        await preferences.ResetUserAsync(Token);

        Assert.Equal("\"neon\"", Assert.Single(_changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged).Value);
        Assert.Equal("neon", await preferences.GetAsync<string>("theme", Token));
        Assert.Single(_browser.Unwatched);
        Assert.Equal(watched + 1, _browser.WatchedPrefixes.Count);
        Assert.Contains("omni:pref:bruno:", _browser.WatchedPrefixes[^1], StringComparison.Ordinal);
        Assert.Contains(_browser.Storage.Keys, key => key.StartsWith("omni:pref:anne:", StringComparison.Ordinal));
        Assert.Contains(_browser.Storage.Keys, key => key.StartsWith("omni:pref:bruno:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SigningOut_RaisesTheDefaults_ForEveryPreferenceOfTheCircuit()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));

        _store.User = null;
        await preferences.ResetUserAsync(Token);

        Assert.Null(Assert.Single(_changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged).Value);
        Assert.Null(await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ABlankScopeThatIsNotEmpty_IsStillRefused()
    {
        _store.User = "   ";
        var preferences = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ResetUser_OnADisposedService_Throws()
    {
        var preferences = Create();
        await preferences.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => preferences.ResetUserAsync(Token));
    }

    [Fact]
    public async Task TheAppearanceState_FollowsTheUserWhoSignsIn()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _store.User = "anne";
        var preferences = Create();
        var state = Services.GetRequiredService<OmniAppearanceState>();
        await state.LoadAsync(Token);
        await state.SetAppearanceAsync(OmniAppearance.Dark);
        Assert.NotNull(_store.Of("anne")[OmniAppearanceState.PreferenceKey]);

        _store.User = "bruno";
        await preferences.ResetUserAsync(Token);
        Assert.Equal(OmniAppearance.System, state.Appearance);

        _store.User = "anne";
        await preferences.ResetUserAsync(Token);
        Assert.Equal(OmniAppearance.Dark, state.Appearance);
    }

    [Fact]
    public async Task AWriteReplacedWhileItWaitsForTheStore_IsNotSavedOverTheNewerOne()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.HoldReads = held;
        var preferences = Create();

        // A first read holds the store: both writes below wait for it, the first one replaced meanwhile.
        var read = preferences.GetAsync<string>("other", Token);
        var first = preferences.SetAsync("theme", "aplat", Token);
        var second = preferences.SetAsync("theme", "givre", Token);
        held.SetResult();
        await Task.WhenAll(read, first, second);

        Assert.Equal(["\"givre\""], _store.Of("anne").Saved.Select(saved => saved.Value));
    }

    [Fact]
    public async Task AUserWhoSignsInWithoutResetUser_NeverReceivesTheFormerUsersUnconfirmedValues()
    {
        _store.User = "anne";
        _store.Of("anne").FailWrites = true;
        var preferences = Create();
        await preferences.SetAsync("theme", "aplat", Token);
        Assert.Contains(_browser.Storage, pair => pair.Key == "omni:pref:anne:theme");

        // The host signs anne out and bruno in, and never calls ResetUserAsync.
        _store.User = "bruno";
        await preferences.SyncAsync(Token);
        await preferences.WhenIdleAsync();

        Assert.Empty(_store.Of("bruno").Saved);
        Assert.Null(_store.Of("bruno")["theme"]);
        Assert.Contains(_changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged);
    }

    [Fact]
    public async Task AValueWrittenAfterAChangeOfUserWithoutResetUser_GoesToTheNewUsersPartition()
    {
        _store.User = "anne";
        var preferences = Create();
        await preferences.SetAsync("theme", "aplat", Token);
        var anneKeys = _browser.Storage.Keys.Where(key => key.StartsWith("omni:pref:anne:", StringComparison.Ordinal)).Order().ToArray();
        var anneValues = anneKeys.ToDictionary(key => key, key => _browser.Storage[key]);

        _store.User = "bruno";
        await preferences.SetAsync("density", "dense", Token);

        Assert.Equal(anneValues, _browser.Storage.Where(pair => pair.Key.StartsWith("omni:pref:anne:", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value));
        Assert.Contains(_browser.Storage.Keys, key => key == "omni:pref:bruno:density");
        Assert.Equal("dense", Assert.Single(_store.Of("bruno").Saved).Value is { } value ? System.Text.Json.JsonSerializer.Deserialize<string>(value) : null);
    }

    [Fact]
    public async Task AValueReadAfterAChangeOfUserWithoutResetUser_IsTheNewUsersNotTheFormerCachedOne()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));

        _store.User = "bruno";

        Assert.Null(await preferences.GetAsync<string>("theme", Token));
    }

    [Fact]
    public async Task ResetUser_WhileTheCacheIsStillOpening_ReleasesTheOpeningOfTheFormerUser()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.User = "anne";
        _store.HoldScope = held;
        var preferences = Create();

        var read = preferences.GetAsync<string>("theme", Token);
        _store.User = "bruno";
        var reset = preferences.ResetUserAsync(Token);
        held.SetResult();
        await Task.WhenAll(read, reset);
        await preferences.DisposeAsync();

        // Every script the openings started is stopped: none outlives the service, none is overwritten unreleased.
        Assert.Equal(_browser.WatchedPrefixes.Count, _browser.Unwatched.Count);
        Assert.Equal(_browser.Imports, _browser.Disposals);
    }

    [Fact]
    public async Task AUserWhoSignsInWhileASynchronizationRuns_IsTakenAtOnce_ByAnExplicitSynchronization()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        _store.Of("bruno").Put("theme", "\"neon\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        _store.WhenChangedRead = () => _store.User = "bruno";

        await preferences.SyncAsync(Token);

        Assert.Contains(_changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged && change.Value == "\"neon\"");
        Assert.Equal("neon", await preferences.GetAsync<string>("theme", Token));
        Assert.Empty(_store.Of("bruno").Saved);
    }

    [Fact]
    public async Task AUserWhoSignsInWhileTheBackgroundSynchronizationRuns_IsTakenAtOnce()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        _store.Of("bruno").Put("theme", "\"neon\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        await preferences.WhenIdleAsync();
        _store.WhenChangedRead = () => _store.User = "bruno";

        await _browser.Bridge!.OnVisible();
        await preferences.WhenIdleAsync();

        Assert.Contains(_changes, change => change.Origin == OmniPreferenceChangeOrigin.UserChanged && change.Value == "\"neon\"");
        Assert.Empty(_store.Of("bruno").Saved);
    }

    [Fact]
    public async Task AStoreThatCannotNameItsUserNow_IsUsedAlone_AndTheFailureIsLogged()
    {
        _store.User = "anne";
        _store.Of("anne").Put("theme", "\"givre\"", Start);
        var preferences = Create();
        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));
        _store.FailScope = true;

        Assert.Equal("givre", await preferences.GetAsync<string>("theme", Token));

        Assert.Contains(_logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning
            && entry.Message.Contains("could not give the scope", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResetUser_WhileTheFormerOpeningFails_CompletesAndLeavesNothingStarted()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.User = "anne";
        _store.HoldScope = held;
        var preferences = Create();
        _browser.FailuresOnce["watch"] = new InvalidOperationException("The script could not watch the cache.");

        var read = preferences.GetAsync<string>("theme", Token);
        _store.User = "bruno";
        var reset = preferences.ResetUserAsync(Token);
        held.SetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => read);
        await reset;
        await preferences.DisposeAsync();

        Assert.Equal(_browser.WatchedPrefixes.Count, _browser.Unwatched.Count);
        Assert.Equal(_browser.Imports, _browser.Disposals);
    }

    /// <summary>A store per user, as a host's table filtered by the signed-in user; no user answers <see cref="OmniPreferences.NoUserScope"/>.</summary>
    private sealed class UserStore : IOmniPreferenceStore
    {
        private readonly Dictionary<string, MemoryPreferenceStore> _users = new(StringComparer.Ordinal);

        public string? User { get; set; } = "anne";

        public int Calls;

        /// <summary>When set, a read waits for it before answering, holding the store's one call at a time.</summary>
        public TaskCompletionSource? HoldReads { get; set; }

        /// <summary>Run once the store has answered a read of what changed, as a user who signs in meanwhile.</summary>
        public Action? WhenChangedRead { get; set; }

        /// <summary>When set, the store cannot name its user.</summary>
        public bool FailScope { get; set; }

        /// <summary>When set, the first scope request waits for it, as a store that is slow to name its user.</summary>
        public TaskCompletionSource? HoldScope { get => _holdScope; set => _holdScope = value; }

        private TaskCompletionSource? _holdScope;

        public MemoryPreferenceStore Of(string user)
        {
            if (!_users.TryGetValue(user, out var store))
            {
                store = new MemoryPreferenceStore { Scope = user };
                _users[user] = store;
            }

            return store;
        }

        private MemoryPreferenceStore Current()
        {
            Interlocked.Increment(ref Calls);
            return Of(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."));
        }

        public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var store = Current();
            if (HoldReads is { } hold)
            {
                await hold.Task.WaitAsync(cancellationToken);
            }

            return await store.GetAsync(key, cancellationToken);
        }

        public async Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
        {
            var changed = await Current().GetChangedSinceAsync(since, cancellationToken);
            WhenChangedRead?.Invoke();
            return changed;
        }

        public Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Current().SaveAsync(preference, cancellationToken);

        public async ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default)
        {
            if (FailScope)
            {
                throw new InvalidOperationException("The store cannot name its user.");
            }

            var scope = User ?? OmniPreferences.NoUserScope;
            if (Interlocked.Exchange(ref _holdScope, null) is { } hold)
            {
                await hold.Task.WaitAsync(cancellationToken);
            }

            return scope;
        }
    }
}
