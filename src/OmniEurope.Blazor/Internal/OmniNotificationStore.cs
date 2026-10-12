using System.Diagnostics;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The notifications shown and their countdowns. A countdown ends on the thread pool while the renderer
/// reads the list, so one lock guards the list, the countdowns and the disposed flag; readers get a
/// snapshot, never the list itself, and <c>changed</c> is raised outside the lock.
/// </summary>
internal sealed class OmniNotificationStore : IDisposable
{
    /// <summary>Beyond this many characters a message is long: wider card, folded text, longer life.</summary>
    internal const int LongMessageThreshold = 300;

    private static readonly TimeSpan ShortestLongLife = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan LongestLife = TimeSpan.FromSeconds(30);

    private readonly Lock _gate = new();
    private readonly List<OmniNotificationMessage> _messages = [];
    private readonly Dictionary<Guid, Expiration> _expirations = [];
    private readonly TimeProvider _timeProvider;
    private readonly Action _changed;
    private readonly int _capacity;
    private readonly TimeSpan _defaultDuration;
    private IReadOnlyList<OmniNotificationMessage>? _snapshot;
    private long _generation;
    private bool _disposed;

    internal OmniNotificationStore(TimeProvider timeProvider, Action changed, int capacity, TimeSpan defaultDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultDuration, TimeSpan.Zero);
        _timeProvider = timeProvider;
        _changed = changed;
        _capacity = capacity;
        _defaultDuration = defaultDuration;
    }

    /// <summary>
    /// The notifications at the time of the call, oldest first. A later change does not reach a list
    /// already handed out; the same list is handed out again until something changes.
    /// </summary>
    internal IReadOnlyList<OmniNotificationMessage> Messages
    {
        get
        {
            lock (_gate)
            {
                return _snapshot ??= _messages.ToArray().AsReadOnly();
            }
        }
    }

    internal Guid Add(string message, OmniSeverity severity, string? title, TimeSpan? duration, string? detailsHref = null, string? actionText = null, Func<Task>? action = null)
    {
        var effectiveDuration = LifeFor(message, duration ?? _defaultDuration);
        var notification = new OmniNotificationMessage(Guid.NewGuid(), message, severity, title)
        {
            Duration = effectiveDuration > TimeSpan.Zero ? effectiveDuration : null,
            DetailsHref = detailsHref,
            ActionText = actionText,
            Action = action
        };
        CancellationTokenSource? evicted = null;
        Expiration? countdown = null;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_messages.Count == _capacity)
            {
                evicted = Detach(_messages[0].Id);
            }

            _messages.Add(notification);
            _snapshot = null;
            if (effectiveDuration > TimeSpan.Zero)
            {
                countdown = Arm(notification.Id, effectiveDuration);
            }
        }

        Stop(evicted);
        _changed();
        Start(notification.Id, countdown);
        return notification.Id;
    }

    /// <summary>
    /// A long message stays long enough to be read: four seconds plus one per hundred characters,
    /// thirty at most, and never less than the duration asked for. A short message keeps the
    /// duration asked for. A notification that stays until dismissed stays.
    /// </summary>
    internal static TimeSpan LifeFor(string message, TimeSpan requested)
    {
        if (requested <= TimeSpan.Zero || message.Length <= LongMessageThreshold)
        {
            return requested;
        }

        // Whole seconds: the tint that counts the life down is timed to the second, and a life of 7.5 s
        // under an 8 s tint would close with a sliver of tint left.
        var byLength = ShortestLongLife + TimeSpan.FromSeconds(Math.Ceiling(message.Length / 100d));
        var life = byLength > LongestLife ? LongestLife : byLength;
        return life > requested ? life : requested;
    }

    /// <summary>
    /// Holds a notification while it is being read, pointer over it or focus inside it: it would
    /// otherwise close under the reader's eyes. The time it had left is kept for <see cref="Resume"/>.
    /// A countdown already due whose end has not run yet is held too: it is no longer the one on record.
    /// </summary>
    internal void Pause(Guid id)
    {
        CancellationTokenSource stopped;
        lock (_gate)
        {
            if (!_expirations.TryGetValue(id, out var expiration) || expiration.Remaining is not null)
            {
                return;
            }

            var remaining = expiration.Deadline - _timeProvider.GetUtcNow();
            stopped = expiration.Cancellation;
            _expirations[id] = expiration with
            {
                Remaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero,
                Generation = ++_generation
            };
        }

        Stop(stopped);
    }

    internal void Resume(Guid id)
    {
        Expiration countdown;
        lock (_gate)
        {
            if (_disposed || !_expirations.TryGetValue(id, out var expiration) || expiration.Remaining is not { } remaining)
            {
                return;
            }

            countdown = Arm(id, remaining);
        }

        Start(id, countdown);
    }

    internal bool Remove(Guid id, bool notify = true)
    {
        CancellationTokenSource? stopped;
        bool removed;
        lock (_gate)
        {
            removed = _messages.Exists(notification => notification.Id == id);
            stopped = Detach(id);
        }

        Stop(stopped);
        if (removed && notify)
        {
            _changed();
        }

        return removed;
    }

    /// <summary>
    /// Takes a notification and its countdown out; called under the lock. Returns the cancellation of a
    /// running countdown for the caller to stop once the lock is released.
    /// </summary>
    private CancellationTokenSource? Detach(Guid id)
    {
        if (_messages.RemoveAll(notification => notification.Id == id) > 0)
        {
            _snapshot = null;
        }

        return _expirations.Remove(id, out var expiration) && expiration.Remaining is null
            ? expiration.Cancellation
            : null;
    }

    /// <summary>Records a new countdown, under the lock; <see cref="Start"/> runs it once the lock is released.</summary>
    private Expiration Arm(Guid id, TimeSpan duration)
    {
        var expiration = new Expiration(new CancellationTokenSource(), _timeProvider.GetUtcNow() + duration, duration, null, ++_generation);
        _expirations[id] = expiration;
        return expiration;
    }

    private void Start(Guid id, Expiration? countdown)
    {
        if (countdown is not null)
        {
            Observe(ExpireAsync(id, countdown));
        }
    }

    private async Task ExpireAsync(Guid id, Expiration countdown)
    {
        var cancellationToken = countdown.Cancellation.Token;
        try
        {
            await Task.Delay(countdown.Life, _timeProvider, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Paused, dismissed or disposed: whoever stopped this countdown has decided what follows.
            return;
        }

        bool removed;
        lock (_gate)
        {
            // Only the countdown on record may close the notification: one paused, resumed or replaced
            // since this delay began is stale, even when its delay ended before it was cancelled.
            if (_disposed || !_expirations.TryGetValue(id, out var current) || current.Generation != countdown.Generation)
            {
                return;
            }

            _expirations.Remove(id);
            removed = _messages.RemoveAll(notification => notification.Id == id) > 0;
            _snapshot = null;
        }

        countdown.Cancellation.Dispose();
        if (removed)
        {
            _changed();
        }
    }

    /// <summary>
    /// A countdown runs unawaited. A fault in it, a <c>changed</c> handler that throws, is read and traced
    /// rather than left unobserved with the task.
    /// </summary>
    private static void Observe(Task expiry) => _ = expiry.ContinueWith(
        static task => Trace.TraceError("A notification expiry failed: {0}", task.Exception),
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

    private static void Stop(CancellationTokenSource? cancellation)
    {
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }

    public void Dispose()
    {
        List<CancellationTokenSource> running;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            running = [.. _expirations.Values.Where(expiration => expiration.Remaining is null).Select(expiration => expiration.Cancellation)];
            _expirations.Clear();
        }

        foreach (var cancellation in running)
        {
            Stop(cancellation);
        }
    }

    /// <summary>
    /// A countdown of <paramref name="Life"/> running, or paused when <paramref name="Remaining"/> is set.
    /// <paramref name="Generation"/> tells the countdown on record from a stale one.
    /// </summary>
    private sealed record Expiration(CancellationTokenSource Cancellation, DateTimeOffset Deadline, TimeSpan Life, TimeSpan? Remaining, long Generation);
}
