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
    /// <c>omni-preferences.js</c> over the real <see cref="JSRuntime"/>: the framework makes the module a
    /// <c>JSObjectReference</c> and refuses a call on it once disposed. Every answer comes from another thread, as a
    /// real round trip does; a test can hold one call already issued (<see cref="Hold"/>).
    /// </summary>
    private sealed class FrameworkBrowser : JSRuntime
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);
        private readonly List<Held> _held = [];
        private long _nextModule;
        private int _live;

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
    private sealed class AccountStore : IOmniPreferenceStore
    {
        private readonly ConcurrentDictionary<string, MemoryPreferenceStore> _accounts = new(StringComparer.Ordinal);

        public volatile string? User;

        private MemoryPreferenceStore Current() =>
            _accounts.GetOrAdd(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."), name => new MemoryPreferenceStore { Scope = name });

        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => Current().GetAsync(key, cancellationToken);

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Current().GetChangedSinceAsync(since, cancellationToken);

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Current().SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(User ?? OmniPreferences.NoUserScope);
    }
}
