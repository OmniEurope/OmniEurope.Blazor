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

        // The first reset reads the preference again for the account and is held inside that opening, while the
        // visitor's entries are merged into the account (a slow browser round trip).
        var (first, merging) = await HoldTheAccountsFirstMergeAsync(preferences, () => preferences.ResetUserAsync(Token));

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

        // The store names the account already; a reader notices it, takes the account itself and is held inside
        // the opening of the account's cache.
        var (reader, merging) = await HoldTheAccountsFirstMergeAsync(preferences);

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
    private Task<(Task<string?> Reader, FrameworkBrowser.Held Merging)> HoldTheAccountsFirstMergeAsync(OmniPreferences preferences) =>
        HoldTheAccountsFirstMergeAsync(preferences, () => preferences.GetAsync<string>("theme", Token));

    /// <summary>
    /// The same hold, the call that meets the account's first opening being <paramref name="start"/> instead of a reader.
    /// </summary>
    private async Task<(TCall Call, FrameworkBrowser.Held Merging)> HoldTheAccountsFirstMergeAsync<TCall>(OmniPreferences preferences, Func<TCall> start)
        where TCall : Task
    {
        await preferences.SetAsync("theme", "aplat", Token);
        _store.User = "anne";
        var merging = _browser.Hold("entries", prefix => prefix == VisitorPrefix);
        var call = start();
        await merging.WhenReached.WaitAsync(Patience, Token);
        return (call, merging);
    }

    /// <summary>Signs anne in and reads the theme once, which opens her cache; answers the theme read.</summary>
    private async Task<string?> SignInAnneAndReadTheThemeAsync(OmniPreferences preferences)
    {
        _store.User = "anne";
        var theme = await preferences.GetAsync<string>("theme", Token);
        await preferences.WhenIdleAsync();
        return theme;
    }

    /// <summary>Starts a write of size the store holds in its save, and waits until the store is saving it.</summary>
    private async Task<(Task Writing, Pause Saving)> StartAWriteTheStoreHoldsAsync(OmniPreferences preferences)
    {
        var saving = _store.PauseNextSave();
        var writing = preferences.SetAsync("size", "grand", Token);
        await saving.WhenReached.WaitAsync(Patience, Token);
        return (writing, saving);
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
    public async Task ABootCopy_WaitingForAnOpeningThatAResetReleasesMeanwhile_IsAttachedOnTheCurrentOpeningInstead()
    {
        await using var preferences = Open();
        var (reader, merging) = await HoldTheAccountsFirstMergeAsync(preferences);

        var attaching = preferences.AttachBootCopyAsync("theme", "{\"boot\":1}", Token);
        await preferences.ResetUserAsync(Token).WaitAsync(Patience, Token);

        merging.Release();
        await attaching.WaitAsync(Patience, Token);
        Assert.Equal("aplat", await reader.WaitAsync(Patience, Token));

        // The call made on the released opening was refused, and made again for the user signed in now: attached once.
        Assert.Equal(("omni:pref:anne:theme", "{\"boot\":1}"), Assert.Single(_browser.Attached));
        Assert.Equal(1, _browser.LiveModules);
    }

    [Fact]
    public async Task AWrite_WhoseOpeningIsReleasedWhileTheStoreSavesIt_CompletesAndIsNotSavedAgain()
    {
        await using var preferences = Open();
        Assert.Null(await SignInAnneAndReadTheThemeAsync(preferences));

        // The value is cached; the store is busy saving it when a reset releases the opening the write was made on.
        var (writing, saving) = await StartAWriteTheStoreHoldsAsync(preferences);
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
        Assert.Null(await SignInAnneAndReadTheThemeAsync(preferences));

        var (writing, saving) = await StartAWriteTheStoreHoldsAsync(preferences);
        await preferences.DisposeAsync();

        saving.Release();

        // The service is gone: nothing takes over the write, so the caller is told, as for any use after disposal.
        await Assert.ThrowsAsync<ObjectDisposedException>(() => writing.WaitAsync(Patience, Token));
        Assert.Equal(0, _browser.LiveModules);
    }

    /// <summary>
    /// <c>omni-preferences.js</c> over the real <see cref="JSRuntime"/>: the framework makes the module a
    /// <c>JSObjectReference</c> and refuses a call on it once disposed. Every answer comes from another thread, as a
    /// real round trip does; a test can hold one call already issued (<see cref="Hold"/>).
    /// </summary>
    private sealed class FrameworkBrowser : JSRuntime
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);
        private readonly List<Held> _held = [];
        private readonly List<(string Key, string BootCopy)> _attached = [];
        private long _nextModule;
        private int _live;
        private TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>How many modules are imported and not yet disposed by their owner.</summary>
        public int LiveModules
        {
            get
            {
                lock (_gate)
                {
                    return _live;
                }
            }
        }

        /// <summary>The boot copies the script was told to attach, by storage key, in call order.</summary>
        public IReadOnlyList<(string Key, string BootCopy)> Attached
        {
            get
            {
                lock (_gate)
                {
                    return [.. _attached];
                }
            }
        }

        /// <summary>The text of a storage entry, as the script holds it.</summary>
        public string Stored(string key)
        {
            lock (_gate)
            {
                return _storage[key];
            }
        }

        /// <summary>Completes when the next module is disposed by its owner: from then on it refuses every call.</summary>
        public Task NextDisposal()
        {
            lock (_gate)
            {
                _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
                return _disposed.Task;
            }
        }

        public Held Hold(string identifier, Func<string?, bool> firstArgument)
        {
            var held = new Held(identifier, firstArgument);
            lock (_gate)
            {
                _held.Add(held);
            }

            return held;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            if (identifier.Contains("disposeJSObjectReferenceById", StringComparison.Ordinal))
            {
                lock (_gate)
                {
                    _live--;
                    _disposed.TrySetResult();
                }

                if (taskId == 0)
                {
                    return;
                }
            }

            var arguments = argsJson is null ? [] : JsonSerializer.Deserialize<JsonElement[]>(argsJson)!;
            var first = arguments.Length > 0 && arguments[0].ValueKind == JsonValueKind.String ? arguments[0].GetString() : null;
            Held? held;
            lock (_gate)
            {
                held = _held.FirstOrDefault(candidate => !candidate.Taken && candidate.Identifier == identifier && candidate.FirstArgument(first));
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

                DotNetDispatcher.EndInvokeJS(this, $"[{taskId},true,{Answer(identifier, arguments, first)}]");
            });
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }

        private string Answer(string identifier, JsonElement[] arguments, string? first)
        {
            lock (_gate)
            {
                switch (identifier)
                {
                    case "import":
                        _live++;
                        return $$"""{"__jsObjectId":{{++_nextModule}}}""";
                    case "get":
                        return _storage.TryGetValue(first!, out var value) ? JsonSerializer.Serialize(value) : "null";
                    case "set":
                        _storage[first!] = arguments[1].GetString()!;
                        return "null";
                    case "attach":
                        _attached.Add((first!, arguments[1].GetString()!));
                        return "null";
                    case "entries":
                        return JsonSerializer.Serialize(_storage.Where(pair => pair.Key.StartsWith(first!, StringComparison.Ordinal)).Select(pair => new[] { pair.Key, pair.Value }));
                    case "watch":
                        return "1";
                    default:
                        return "null";
                }
            }
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
    private sealed class AccountStore() : AccountPreferenceStore(null)
    {
        private Pause? _pause;

        /// <summary>The next save waits, inside the store, until the test releases it.</summary>
        public Pause PauseNextSave()
        {
            var pause = new Pause();
            _pause = pause;
            return pause;
        }

        public override async Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _pause, null) is { } pause)
            {
                await pause.WaitAsync();
            }

            return await base.SaveAsync(preference, cancellationToken);
        }
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
