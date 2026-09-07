namespace OmniEurope.Blazor.Catalog;

internal sealed class CspViolationStore
{
    private const int MaximumReports = 100;
    private readonly Lock _gate = new();
    private readonly Queue<string> _violations = new();

    internal IReadOnlyList<string> Violations
    {
        get
        {
            lock (_gate)
            {
                return _violations.ToArray();
            }
        }
    }

    internal int Count
    {
        get
        {
            lock (_gate)
            {
                return _violations.Count;
            }
        }
    }

    internal void Add(string report)
    {
        lock (_gate)
        {
            _violations.Enqueue(report);
            while (_violations.Count > MaximumReports)
            {
                _violations.Dequeue();
            }
        }
    }
}
