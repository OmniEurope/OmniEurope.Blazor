using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniNotificationStore : IDisposable
{
    private readonly List<OmniNotificationMessage> _messages = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _expirations = [];
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

    internal Guid Add(string message, OmniNotificationSeverity severity, string? title, TimeSpan? duration)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_messages.Count == _capacity)
        {
            Remove(_messages[0].Id, notify: false);
        }

        var notification = new OmniNotificationMessage(Guid.NewGuid(), message, severity, title);
        _messages.Add(notification);
        _changed();

        var effectiveDuration = duration ?? _defaultDuration;
        if (effectiveDuration > TimeSpan.Zero)
        {
            var cancellation = new CancellationTokenSource();
            _expirations[notification.Id] = cancellation;
            _ = ExpireAsync(notification.Id, effectiveDuration, cancellation.Token);
        }

        return notification.Id;
    }

    internal bool Remove(Guid id, bool notify = true)
    {
        if (_expirations.Remove(id, out var cancellation))
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }

        var removed = _messages.RemoveAll(notification => notification.Id == id) > 0;
        if (removed && notify)
        {
            _changed();
        }

        return removed;
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
        foreach (var cancellation in _expirations.Values)
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }

        _expirations.Clear();
    }
}
