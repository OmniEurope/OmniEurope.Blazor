namespace OmniEurope.Blazor.Tests;

/// <summary>A clock the test moves by hand, from <paramref name="start"/> (the first of January 2026 by default), firing the timers it owes on each step.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset? start = null) : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = start ?? new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        // A component asks for its timer on the renderer's thread while the test moves the clock on its own.
        lock (_timers)
        {
            var timer = new ManualTimer(this, callback, state, _now + dueTime);
            _timers.Add(timer);
            return timer;
        }
    }

    /// <summary>How many timers still wait for the clock to reach them.</summary>
    public int Pending
    {
        get
        {
            lock (_timers)
            {
                return _timers.Count(timer => !timer.Disposed);
            }
        }
    }

    /// <summary>The timers that still wait for the clock to reach them, in the order they were asked for.</summary>
    public IReadOnlyList<ITimer> PendingTimers
    {
        get
        {
            lock (_timers)
            {
                return [.. _timers.Where(timer => !timer.Disposed)];
            }
        }
    }

    public void Advance(TimeSpan by)
    {
        List<ManualTimer> due;
        lock (_timers)
        {
            _now += by;
            due = [.. _timers.Where(timer => !timer.Disposed && timer.Due <= _now)];
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state, DateTimeOffset due) : ITimer
    {
        public DateTimeOffset Due { get; private set; } = due;

        public bool Disposed { get; private set; }

        public void Fire()
        {
            Disposed = true;
            callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Due = owner._now + dueTime;
            return true;
        }

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
