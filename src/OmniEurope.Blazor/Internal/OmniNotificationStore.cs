using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniNotificationStore : IDisposable
{
    /// <summary>Beyond this many characters a message is long: wider card, folded text, longer life.</summary>
    internal const int LongMessageThreshold = 300;

    private static readonly TimeSpan ShortestLongLife = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan LongestLife = TimeSpan.FromSeconds(30);

    private readonly List<OmniNotificationMessage> _messages = [];
    private readonly Dictionary<Guid, Expiration> _expirations = [];
    private readonly TimeProvider _timeProvider;
    private readonly Action _changed;
    private readonly int _capacity;
    private readonly TimeSpan _defaultDuration;
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

    internal IReadOnlyList<OmniNotificationMessage> Messages => _messages;

    internal Guid Add(string message, OmniNotificationSeverity severity, string? title, TimeSpan? duration, string? detailsHref = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_messages.Count == _capacity)
        {
            Remove(_messages[0].Id, notify: false);
        }

        var effectiveDuration = LifeFor(message, duration ?? _defaultDuration);
        var notification = new OmniNotificationMessage(Guid.NewGuid(), message, severity, title)
        {
            Duration = effectiveDuration > TimeSpan.Zero ? effectiveDuration : null,
            DetailsHref = detailsHref
        };
        _messages.Add(notification);
        _changed();

        if (effectiveDuration > TimeSpan.Zero)
        {
            Schedule(notification.Id, effectiveDuration);
        }

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
    /// </summary>
    internal void Pause(Guid id)
    {
        if (!_expirations.TryGetValue(id, out var expiration) || expiration.Remaining is not null)
        {
            return;
        }

        var remaining = expiration.Deadline - _timeProvider.GetUtcNow();
        expiration.Cancellation.Cancel();
        expiration.Cancellation.Dispose();
        _expirations[id] = expiration with { Remaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero };
    }

    internal void Resume(Guid id)
    {
        if (_expirations.TryGetValue(id, out var expiration) && expiration.Remaining is { } remaining)
        {
            Schedule(id, remaining);
        }
    }

    internal bool Remove(Guid id, bool notify = true)
    {
        if (_expirations.Remove(id, out var expiration) && expiration.Remaining is null)
        {
            expiration.Cancellation.Cancel();
            expiration.Cancellation.Dispose();
        }

        var removed = _messages.RemoveAll(notification => notification.Id == id) > 0;
        if (removed && notify)
        {
            _changed();
        }

        return removed;
    }

    private void Schedule(Guid id, TimeSpan duration)
    {
        var cancellation = new CancellationTokenSource();
        _expirations[id] = new Expiration(cancellation, _timeProvider.GetUtcNow() + duration, null);
        _ = ExpireAsync(id, duration, cancellation.Token);
    }

    private async Task ExpireAsync(Guid id, TimeSpan duration, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(duration, _timeProvider, cancellationToken);
            Remove(id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var expiration in _expirations.Values.Where(expiration => expiration.Remaining is null))
        {
            expiration.Cancellation.Cancel();
            expiration.Cancellation.Dispose();
        }

        _expirations.Clear();
    }

    /// <summary>A running countdown, or a paused one when <paramref name="Remaining"/> is set.</summary>
    private sealed record Expiration(CancellationTokenSource Cancellation, DateTimeOffset Deadline, TimeSpan? Remaining);
}
