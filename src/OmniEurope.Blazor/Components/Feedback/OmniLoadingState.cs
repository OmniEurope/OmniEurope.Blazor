namespace OmniEurope.Blazor.Components;

/// <summary>
/// Whether the application is loading, and how far along. A page reports it here instead of drawing
/// its own bar in its own body: a bar inside the page pushes the content down when it appears and
/// back up when it goes, and each page ends up placing it somewhere slightly different.
/// </summary>
/// <remarks>
/// Loads are counted rather than flagged, so two overlapping requests do not have the faster of the
/// two put the bar away while the slower is still running.
/// </remarks>
public sealed class OmniLoadingState
{
    private int _running;
    private double _value;

    public event Action? Changed;

    /// <summary>True while at least one load is running.</summary>
    public bool Loading => _running > 0;

    /// <summary>Progress of the current load, 0 to 100, when the caller reports one.</summary>
    public double Value => _value;

    /// <summary>True while a load is running and nobody has reported a figure for it.</summary>
    public bool Unmeasured => Loading && _value <= 0d;

    /// <summary>
    /// Marks a load as started. Every call must be matched by <see cref="End"/>, which the disposable
    /// returned by <see cref="Track"/> does on its own.
    /// </summary>
    public void Begin()
    {
        _running++;
        if (_running == 1)
        {
            _value = 0d;
        }

        Changed?.Invoke();
    }

    /// <summary>Reports how far the current load has got, as a percentage.</summary>
    public void Report(double percentage)
    {
        var next = Math.Clamp(percentage, 0d, 100d);
        if (Math.Abs(next - _value) < 0.01d)
        {
            return;
        }

        _value = next;
        Changed?.Invoke();
    }

    /// <summary>Marks a load as finished. Ending more loads than were started is ignored.</summary>
    public void End()
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

        Changed?.Invoke();
    }

    /// <summary>
    /// Marks a load as started and ends it on dispose, so a load cannot be left running by an early
    /// return or by a throw halfway through it.
    /// </summary>
    public IDisposable Track()
    {
        Begin();
        return new Scope(this);
    }

    private sealed class Scope(OmniLoadingState owner) : IDisposable
    {
        private bool _ended;

        public void Dispose()
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            owner.End();
        }
    }
}
