namespace OmniEurope.Blazor.Components;

/// <summary>
/// Whether the application is loading, and how far along. A page reports it here instead of drawing
/// its own bar in its own body: a bar inside the page pushes the content down when it appears and
/// back up when it goes, and each page ends up placing it somewhere slightly different.
/// </summary>
/// <remarks>
/// Loads are counted rather than flagged, so two overlapping requests do not have the faster of the
/// two put the bar away while the slower is still running. The count is safe to change from several
/// threads at once (a load ended on a thread-pool continuation, for instance).
/// </remarks>
public sealed class OmniLoadingState
{
    private readonly Lock _gate = new();
    private int _running;
    private double _value;

    /// <summary>
    /// Raised, on the calling thread, after every <see cref="Begin"/> and every effective <see cref="End"/>,
    /// and after a <see cref="Report"/> that moves the figure by 0.01 or more.
    /// </summary>
    public event Action? Changed;

    /// <summary>True while at least one load is running.</summary>
    public bool Loading => Volatile.Read(ref _running) > 0;

    /// <summary>Progress of the current load, 0 to 100, when the caller reports one.</summary>
    public double Value => Volatile.Read(ref _value);

    /// <summary>True while a load is running and nobody has reported a figure for it.</summary>
    public bool Unmeasured => Loading && Value <= 0d;

    /// <summary>
    /// Marks a load as started. Every call must be matched by <see cref="End"/>, which the disposable
    /// returned by <see cref="Track"/> does on its own.
    /// </summary>
    public void Begin()
    {
        lock (_gate)
        {
            _running++;
            if (_running == 1)
            {
                _value = 0d;
            }
        }

        Changed?.Invoke();
    }

    /// <summary>Reports how far the current load has got, as a percentage.</summary>
    public void Report(double percentage)
    {
        var next = Math.Clamp(percentage, 0d, 100d);
        lock (_gate)
        {
            if (Math.Abs(next - _value) < 0.01d)
            {
                return;
            }

            _value = next;
        }

        Changed?.Invoke();
    }

    /// <summary>Marks a load as finished. Ending more loads than were started is ignored.</summary>
    public void End()
    {
        lock (_gate)
        {
            if (_running == 0)
            {
                return;
            }

            _running--;
            if (_running == 0)
            {
                _value = 0d;
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Marks a load as started and ends it on dispose, so a load cannot be left running by an early
    /// return or by a throw halfway through it. Disposing the scope more than once ends the load once.
    /// </summary>
    public IDisposable Track()
    {
        Begin();
        return new Scope(this);
    }

    private sealed class Scope(OmniLoadingState owner) : IDisposable
    {
        private int _ended;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 0)
            {
                owner.End();
            }
        }
    }
}
