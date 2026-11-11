using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A request of a client application (2026-11-07): two changes of user that overlap never fail with
/// <see cref="ObjectDisposedException"/>, whether both are explicit <see cref="OmniPreferences.ResetUserAsync"/> calls or
/// one is the reset a reader starts by itself when the store already names the new user. The browser is played over the
/// real <see cref="JSRuntime"/>, whose module references refuse a call once disposed, as the framework's do (the bUnit
/// double and <see cref="PreferenceBrowser"/> do not); the store is an in-memory fake per account.
/// </summary>
public sealed class PreferenceUserSwitchConcurrencyTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);
    private const string VisitorPrefix = "omni:pref:visitor:";

    private readonly FrameworkBrowser _browser = new();
    private readonly AccountStore _store = new();

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private OmniPreferences Open() =>
        new(_browser, _store, new OmniPreferencesOptions { SignedOut = OmniSignedOutPreferences.Browser, SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance, TimeProvider.System);

    [Fact]
    public async Task TwoResets_TheSecondEndingWhileTheFirstStillReads_FailNeither()
    {
        await using var preferences = Open();
        await preferences.SetAsync("theme", "aplat", Token);

        // The first reset reads the preference again for the account and is held inside that opening, while the
        // visitor's entries are merged into the account (a slow browser round trip).
        _store.User = "anne";
        var merging = _browser.Hold("entries", prefix => prefix == VisitorPrefix);
        var first = preferences.ResetUserAsync(Token);
        await merging.WhenReached.WaitAsync(Patience, Token);

        // A second reset comes meanwhile and ends: it releases the opening the first one still reads.
        await preferences.ResetUserAsync(Token).WaitAsync(Patience, Token);

        merging.Release();
        await first.WaitAsync(Patience, Token);

        Assert.Equal("aplat", await preferences.GetAsync<string>("theme", Token));
        Assert.Equal(1, _browser.LiveModules);
    }

    [Fact]
    public async Task AReaderThatNoticesTheNewUser_AndAnExplicitReset_FailNeither()
    {
        await using var preferences = Open();
        await preferences.SetAsync("theme", "aplat", Token);

        // The store names the account already; a reader notices it, takes the account itself and is held inside
        // the opening of the account's cache.
        _store.User = "anne";
        var merging = _browser.Hold("entries", prefix => prefix == VisitorPrefix);
        var reader = preferences.GetAsync<string>("theme", Token);
        await merging.WhenReached.WaitAsync(Patience, Token);

        // The host's own reset of the sign-in comes meanwhile and ends.
        await preferences.ResetUserAsync(Token).WaitAsync(Patience, Token);

        merging.Release();
        Assert.Equal("aplat", await reader.WaitAsync(Patience, Token));
        Assert.Equal(1, _browser.LiveModules);
    }

    /// <summary>
    /// A reader that notices the account is held while the visitor's entries are merged into the account's first opening:
    /// any call made now waits for that merge, then takes the opening it was given, which a reset may release meanwhile.
    /// </summary>
    private async Task<(Task<string?> Reader, FrameworkBrowser.Held Merging)> HoldTheAccountsFirstMergeAsync(OmniPreferences preferences)
    {
        await preferences.SetAsync("theme", "aplat", Token);
        _store.User = "anne";
        var merging = _browser.Hold("entries", prefix => prefix == VisitorPrefix);
        var reader = preferences.GetAsync<string>("theme", Token);
        await merging.WhenReached.WaitAsync(Patience, Token);
        return (reader, merging);
    }

    [Fact]
    public async Task ASynchronization_WaitingForAnOpeningThatAResetReleasesMeanwhile_RunsOnTheCurrentOpeningInstead()
    {
        await using var preferences = Open();
        var (reader, merging) = await HoldTheAccountsFirstMergeAsync(preferences);

        // The synchronization takes the account's opening and waits for its merge, then a reset replaces the opening.
        var synchronizing = preferences.SyncAsync(Token);
        await preferences.ResetUserAsync(Token).WaitAsync(Patience, Token);

        merging.Release();
        await synchronizing.WaitAsync(Patience, Token);
        Assert.Equal("aplat", await reader.WaitAsync(Patience, Token));

        Assert.Equal("\"aplat\"", _store.Of("anne")["theme"]?.Value);
        Assert.Equal(1, _browser.LiveModules);
    }

    [Fact]
    public async Task AWrite_WaitingForAnOpeningThatAResetReleasesMeanwhile_IsCachedAndSavedOnTheCurrentOpeningInstead()
    {
        await using var preferences = Open();
        var (reader, merging) = await HoldTheAccountsFirstMergeAsync(preferences);

        var writing = preferences.SetAsync("size", "grand", Token);
        await preferences.ResetUserAsync(Token).WaitAsync(Patience, Token);

        merging.Release();
        await writing.WaitAsync(Patience, Token);
        Assert.Equal("aplat", await reader.WaitAsync(Patience, Token));
        await preferences.WhenIdleAsync();

        Assert.Equal("grand", await preferences.GetAsync<string>("size", Token));
        Assert.Equal("\"grand\"", _store.Of("anne")["size"]?.Value);
        Assert.Contains("grand", _browser.Stored("omni:pref:anne:size"), StringComparison.Ordinal);
        Assert.Equal(1, _browser.LiveModules);
    }

    [Fact]
    public async Task AWrite_WhoseOpeningIsReleasedWhileTheStoreSavesIt_CompletesAndIsNotSavedAgain()
    {
        await using var preferences = Open();
        _store.User = "anne";
        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        await preferences.WhenIdleAsync();

        // The value is cached; the store is busy saving it when a reset releases the opening the write was made on.
        var saving = _store.PauseNextSave();
        var writing = preferences.SetAsync("size", "grand", Token);
        await saving.WhenReached.WaitAsync(Patience, Token);
        var released = _browser.NextDisposal();
        var resetting = preferences.ResetUserAsync(Token);
        await released.WaitAsync(Patience, Token);

        saving.Release();
        await writing.WaitAsync(Patience, Token);
        await resetting.WaitAsync(Patience, Token);
        await preferences.WhenIdleAsync();

        Assert.Equal("grand", await preferences.GetAsync<string>("size", Token));
        var sent = Assert.Single(_store.Of("anne").Saved, saved => saved.Key == "size");
        Assert.Equal("\"grand\"", sent.Value);
        Assert.Equal(1, _browser.LiveModules);
    }

    [Fact]
    public async Task AWrite_WhoseOpeningIsReleasedByTheDisposalWhileTheStoreSavesIt_FailsAsDisposed()
    {
        var preferences = Open();
        _store.User = "anne";
        Assert.Null(await preferences.GetAsync<string>("theme", Token));
        await preferences.WhenIdleAsync();

        var saving = _store.PauseNextSave();
        var writing = preferences.SetAsync("size", "grand", Token);
        await saving.WhenReached.WaitAsync(Patience, Token);
        await preferences.DisposeAsync();

        saving.Release();

        // The service is gone: nothing takes over the write, so the caller is told, as for any use after disposal.
        await Assert.ThrowsAsync<ObjectDisposedException>(() => writing.WaitAsync(Patience, Token));
        Assert.Equal(0, _browser.LiveModules);
    }

    /// <summary>
    /// <c>omni-preferences.js</c> over the real <see cref="JSRuntime"/>: the framework makes the module a
    /// <c>JSObjectReference</c> and refuses a call on it once disposed. Every answer comes from another thread, as a
    /// real round trip does; a test can hold one call already issued (<c>Hold</c>).
    /// The control surface (<c>Stored</c>, <c>NextDisposal</c>, <c>Hold</c>) is kept apart in <c>FrameworkBrowserControl</c>.
    /// </summary>
    internal sealed class FrameworkBrowser : JSRuntime
    {
        internal readonly Lock Gate = new();
        internal readonly Dictionary<string, string> Storage = new(StringComparer.Ordinal);
        internal readonly List<Held> HeldCalls = [];
        internal long NextModule;
        internal int Live;
        internal TaskCompletionSource Disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>How many modules are imported and not yet disposed by their owner.</summary>
        public int LiveModules
        {
            get
            {
                lock (Gate)
                {
                    return Live;
                }
            }
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            if (identifier.Contains("disposeJSObjectReferenceById", StringComparison.Ordinal))
            {
                lock (Gate)
                {
                    Live--;
                    Disposed.TrySetResult();
                }

                if (taskId == 0)
                {
                    return;
                }
            }

            var arguments = argsJson is null ? [] : JsonSerializer.Deserialize<JsonElement[]>(argsJson)!;
            var first = arguments.Length > 0 && arguments[0].ValueKind == JsonValueKind.String ? arguments[0].GetString() : null;
            Held? held;
            lock (Gate)
            {
                held = HeldCalls.FirstOrDefault(candidate => !candidate.Taken && candidate.Identifier == identifier && candidate.FirstArgument(first));
                held?.Take();
            }

            _ = Task.Run(async () =>
            {
                await Task.Yield();
                if (held is not null)
                {
                    held.Reach();
                    await held.Released;
                }

                DotNetDispatcher.EndInvokeJS(this, $"[{taskId},true,{FrameworkBrowserControl.Answer(this, identifier, arguments, first)}]");
            });
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }

        internal sealed class Held(string identifier, Func<string?, bool> firstArgument)
        {
            private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal string Identifier { get; } = identifier;

            internal Func<string?, bool> FirstArgument { get; } = firstArgument;

            internal bool Taken { get; private set; }

            public Task WhenReached => _reached.Task;

            internal Task Released => _released.Task;

            internal void Take() => Taken = true;

            internal void Reach() => _reached.TrySetResult();

            public void Release() => _released.TrySetResult();
        }
    }

    /// <summary>One in-memory store per account; the scope is the signed-in user, or <see cref="OmniPreferences.NoUserScope"/>.</summary>
    private sealed class AccountStore : IOmniPreferenceStore
    {
        private readonly ConcurrentDictionary<string, MemoryPreferenceStore> _accounts = new(StringComparer.Ordinal);

        private Pause? _pause;

        public volatile string? User;

        public MemoryPreferenceStore Of(string user) => _accounts.GetOrAdd(user, name => new MemoryPreferenceStore { Scope = name });

        /// <summary>The next save waits, inside the store, until the test releases it.</summary>
        public Pause PauseNextSave()
        {
            var pause = new Pause();
            _pause = pause;
            return pause;
        }

        private MemoryPreferenceStore Current() =>
            _accounts.GetOrAdd(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."), name => new MemoryPreferenceStore { Scope = name });

        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => Current().GetAsync(key, cancellationToken);

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Current().GetChangedSinceAsync(since, cancellationToken);

        public async Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _pause, null) is { } pause)
            {
                await pause.WaitAsync();
            }

            return await Current().SaveAsync(preference, cancellationToken);
        }

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(User ?? OmniPreferences.NoUserScope);
    }

    /// <summary>A call held by the test: it says when it was reached, and waits to be released.</summary>
    private sealed class Pause
    {
        private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WhenReached => _reached.Task;

        public void Release() => _released.TrySetResult();

        internal Task WaitAsync()
        {
            _reached.TrySetResult();
            return _released.Task;
        }
    }
}

/// <summary>The control surface and the answers of the framework browser, kept apart from its <see cref="IJSRuntime"/> surface.</summary>
file static class FrameworkBrowserControl
{
    /// <summary>The text of a storage entry, as the script holds it.</summary>
    internal static string Stored(this PreferenceUserSwitchConcurrencyTests.FrameworkBrowser browser, string key)
    {
        lock (browser.Gate)
        {
            return browser.Storage[key];
        }
    }

    /// <summary>Completes when the next module is disposed by its owner: from then on it refuses every call.</summary>
    internal static Task NextDisposal(this PreferenceUserSwitchConcurrencyTests.FrameworkBrowser browser)
    {
        lock (browser.Gate)
        {
            browser.Disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return browser.Disposed.Task;
        }
    }

    internal static PreferenceUserSwitchConcurrencyTests.FrameworkBrowser.Held Hold(this PreferenceUserSwitchConcurrencyTests.FrameworkBrowser browser, string identifier, Func<string?, bool> firstArgument)
    {
        var held = new PreferenceUserSwitchConcurrencyTests.FrameworkBrowser.Held(identifier, firstArgument);
        lock (browser.Gate)
        {
            browser.HeldCalls.Add(held);
        }

        return held;
    }

    internal static string Answer(PreferenceUserSwitchConcurrencyTests.FrameworkBrowser browser, string identifier, JsonElement[] arguments, string? first)
    {
        lock (browser.Gate)
        {
            switch (identifier)
            {
                case "import":
                    browser.Live++;
                    return $$"""{"__jsObjectId":{{++browser.NextModule}}}""";
                case "get":
                    return browser.Storage.TryGetValue(first!, out var value) ? JsonSerializer.Serialize(value) : "null";
                case "set":
                    browser.Storage[first!] = arguments[1].GetString()!;
                    return "null";
                case "entries":
                    return JsonSerializer.Serialize(browser.Storage.Where(pair => pair.Key.StartsWith(first!, StringComparison.Ordinal)).Select(pair => new[] { pair.Key, pair.Value }));
                case "watch":
                    return "1";
                default:
                    return "null";
            }
        }
    }
}
