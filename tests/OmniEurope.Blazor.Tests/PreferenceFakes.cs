using System.Collections.Concurrent;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A browser for <see cref="OmniPreferences"/>: <c>omni-preferences.js</c> played over a dictionary that
/// stands for <c>localStorage</c>, keyed exactly as the script would key it. Import fails with
/// <see cref="ImportFailure"/> when set, as it does while a page is prerendered.
/// </summary>
internal sealed class PreferenceBrowser : IJSRuntime, IJSObjectReference
{
    public ConcurrentDictionary<string, string> Storage { get; } = new(StringComparer.Ordinal);

    public Exception? ImportFailure { get; set; }

    /// <summary>Calls that fail, by function name, as a call on a lost circuit does.</summary>
    public Dictionary<string, Exception> Failures { get; } = new(StringComparer.Ordinal);

    public int Imports { get; private set; }

    public int Writes { get; private set; }

    public List<string> WatchedPrefixes { get; } = [];

    public List<int> Unwatched { get; } = [];

    public PreferenceInteropBridge? Bridge { get; private set; }

    /// <summary>When set, each read of the cache answers after this delay, as a slow browser would.</summary>
    public TimeSpan? GetDelay { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args ??= [];
        if (Failures.TryGetValue(identifier, out var failure))
        {
            return ValueTask.FromException<TValue>(failure);
        }

        if (identifier == "get" && GetDelay is { } delay)
        {
            return DelayedGetAsync<TValue>((string)args[0]!, delay, cancellationToken);
        }

        object? answer = identifier switch
        {
            "import" when ImportFailure is not null => throw ImportFailure,
            "import" => Import((string)args[0]!),
            "get" => Storage.TryGetValue((string)args[0]!, out var value) ? value : null,
            "set" => Set((string)args[0]!, (string)args[1]!),
            "entries" => Storage.Where(pair => pair.Key.StartsWith((string)args[0]!, StringComparison.Ordinal))
                .Select(pair => new[] { pair.Key, pair.Value }).ToArray(),
            "watch" => Watch((DotNetObjectReference<PreferenceInteropBridge>)args[0]!, (string)args[1]!),
            "unwatch" => Unwatch((int)args[0]!),
            _ => throw new InvalidOperationException($"Unexpected call {identifier}."),
        };
        return ValueTask.FromResult((TValue)answer!);
    }

    public int Disposals { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposals++;
        return ValueTask.CompletedTask;
    }

    private async ValueTask<TValue> DelayedGetAsync<TValue>(string key, TimeSpan delay, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return (TValue)(object?)(Storage.TryGetValue(key, out var value) ? value : null)!;
    }

    private PreferenceBrowser Import(string path)
    {
        Assert.Equal(OmniModules.Preferences, path);
        Imports++;
        return this;
    }

    private object? Set(string key, string value)
    {
        Writes++;
        Storage[key] = value;
        return null;
    }

    private object Watch(DotNetObjectReference<PreferenceInteropBridge> reference, string prefix)
    {
        Bridge = reference.Value;
        WatchedPrefixes.Add(prefix);
        return 7;
    }

    private object? Unwatch(int id)
    {
        Unwatched.Add(id);
        return null;
    }
}

/// <summary>An in-memory <see cref="IOmniPreferenceStore"/> that can refuse its writes and reads, and counts its calls.</summary>
internal sealed class MemoryPreferenceStore : IOmniPreferenceStore
{
    private readonly ConcurrentDictionary<string, OmniPreference> _values = new(StringComparer.Ordinal);

    public string Scope { get; set; } = "u-7f3a";

    public bool FailWrites { get; set; }

    public bool FailReads { get; set; }

    public int Reads;

    public ConcurrentQueue<OmniPreference> Saved { get; } = new();

    public ConcurrentQueue<DateTimeOffset?> ChangedSinceCalls { get; } = new();

    public OmniPreference? this[string key] => _values.TryGetValue(key, out var value) ? value : null;

    public void Put(string key, string? value, DateTimeOffset updatedAt) => _values[key] = new(key, value, updatedAt);

    public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Reads);
        return FailReads ? Task.FromException<OmniPreference?>(new TimeoutException("database down")) : Task.FromResult(this[key]);
    }

    public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        ChangedSinceCalls.Enqueue(since);
        if (FailReads)
        {
            return Task.FromException<IReadOnlyList<OmniPreference>>(new TimeoutException("database down"));
        }

        IReadOnlyList<OmniPreference> changed = [.. _values.Values.Where(value => since is null || value.UpdatedAt >= since), .. ExtraChanges];
        return Hold is null ? Task.FromResult(changed) : HeldAsync(changed, cancellationToken);
    }

    /// <summary>Changes listed besides the stored values: duplicates, malformed rows.</summary>
    public List<OmniPreference> ExtraChanges { get; } = [];

    /// <summary>When set, the listing of changes waits for it (or for the cancellation) before answering.</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>Run before a write is kept, to change the browser cache while the store is busy.</summary>
    public Action<OmniPreference>? OnSave { get; set; }

    private async Task<IReadOnlyList<OmniPreference>> HeldAsync(IReadOnlyList<OmniPreference> changed, CancellationToken cancellationToken)
    {
        await Hold!.Task.WaitAsync(cancellationToken);
        return changed;
    }

    public Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
    {
        if (FailWrites)
        {
            return Task.FromException(new TimeoutException("database down"));
        }

        OnSave?.Invoke(preference);
        Saved.Enqueue(preference);
        _values[preference.Key] = preference;
        return Task.CompletedTask;
    }

    public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(Scope);
}

/// <summary>A clock the test moves by hand, whose timers fire only when the test says so.</summary>
internal sealed class PreferenceClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public List<PreferenceTimer> Timers { get; } = [];

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new PreferenceTimer(callback, state, dueTime, period);
        Timers.Add(timer);
        return timer;
    }

    internal sealed class PreferenceTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        public TimeSpan DueTime { get; } = dueTime;

        public TimeSpan Period { get; } = period;

        public bool Disposed { get; private set; }

        public void Fire()
        {
            if (!Disposed)
            {
                callback(state);
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !Disposed;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>A logger that keeps what <see cref="OmniPreferences"/> logs.</summary>
internal sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger<OmniPreferences>
{
    public ConcurrentQueue<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Enqueue((logLevel, formatter(state, exception)));
}

/// <summary>
/// A store that, like one over a scoped <c>DbContext</c>, cannot run two calls at once: a call that starts
/// while another runs is counted in <see cref="Overlaps"/> and fails as EF Core does. Each call takes a
/// little time so that an overlap shows, and <see cref="BeforeGet"/> can hold a read.
/// </summary>
internal sealed class ExclusivePreferenceStore(MemoryPreferenceStore inner) : IOmniPreferenceStore
{
    private int _running;

    public int Overlaps;

    /// <summary>Awaited inside a read, before it answers, with the key read.</summary>
    public Func<string, Task>? BeforeGet { get; set; }

    public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            if (BeforeGet is { } hook)
            {
                await hook(key);
            }

            return await inner.GetAsync(key, cancellationToken);
        });

    public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
        RunAsync(() => inner.GetChangedSinceAsync(since, cancellationToken));

    public Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            await inner.SaveAsync(preference, cancellationToken);
            return true;
        });

    public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) =>
        new(RunAsync(() => inner.GetScopeAsync(cancellationToken).AsTask()));

    private async Task<T> RunAsync<T>(Func<Task<T>> call)
    {
        if (Interlocked.Increment(ref _running) != 1)
        {
            Interlocked.Decrement(ref _running);
            Interlocked.Increment(ref Overlaps);
            throw new InvalidOperationException("A second operation was started on this context instance before a previous operation completed.");
        }

        try
        {
            await Task.Delay(10);
            return await call();
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }
}
