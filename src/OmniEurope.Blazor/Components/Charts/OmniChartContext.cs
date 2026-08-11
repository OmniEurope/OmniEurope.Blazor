namespace OmniEurope.Blazor.Components;

internal sealed class OmniChartContext
{
    private const double PlotStart = 5;
    private const double PlotEnd = 95;
    private readonly List<SeriesRegistration> _series = [];
    private readonly Dictionary<object, (double Minimum, double Maximum)> _valueAxes = [];
    private readonly Dictionary<object, IReadOnlyList<string>> _categoryAxes = [];
    private bool _domainsDirty = true;
    private (double Minimum, double Maximum) _xDomain = (0, 1);
    private (double Minimum, double Maximum) _valueDomain = (0, 1);

    internal int DomainCalculationCount { get; private set; }

    internal event Action? Changed;

    internal void RegisterSeries(object owner, OmniChartSeriesKind kind, IReadOnlyList<OmniChartPoint> data)
    {
        var snapshot = data.ToArray();
        var index = _series.FindIndex(item => ReferenceEquals(item.Owner, owner));
        if (index >= 0 && _series[index].Kind == kind && _series[index].Data.SequenceEqual(snapshot))
        {
            return;
        }

        var registration = new SeriesRegistration(owner, kind, snapshot);
        if (index >= 0)
        {
            _series[index] = registration;
        }
        else
        {
            _series.Add(registration);
        }
        _domainsDirty = true;
        Changed?.Invoke();
    }

    internal void UnregisterSeries(object owner)
    {
        if (_series.RemoveAll(item => ReferenceEquals(item.Owner, owner)) > 0)
        {
            _domainsDirty = true;
            Changed?.Invoke();
        }
    }

    internal void RegisterValueAxis(object owner, double minimum, double maximum)
    {
        var bounds = (minimum, maximum);
        if (_valueAxes.TryGetValue(owner, out var current) && current == bounds)
        {
            return;
        }
        _valueAxes[owner] = bounds;
        _domainsDirty = true;
        Changed?.Invoke();
    }

    internal void UnregisterValueAxis(object owner)
    {
        if (_valueAxes.Remove(owner))
        {
            _domainsDirty = true;
            Changed?.Invoke();
        }
    }

    internal void RegisterCategoryAxis(object owner, IReadOnlyList<string> labels)
    {
        var snapshot = labels.ToArray();
        if (_categoryAxes.TryGetValue(owner, out var current) && current.SequenceEqual(snapshot))
        {
            return;
        }
        _categoryAxes[owner] = snapshot;
        Changed?.Invoke();
    }

    internal void UnregisterCategoryAxis(object owner)
    {
        if (_categoryAxes.Remove(owner))
        {
            Changed?.Invoke();
        }
    }

    internal string Points(object owner) => string.Join(' ', GetSeries(owner).Data.Select(Project));

    internal string AreaPoints(object owner, bool stacked)
    {
        var series = GetSeries(owner);
        if (series.Data.Count == 0)
        {
            return string.Empty;
        }

        var top = new List<string>(series.Data.Count);
        var baseline = new List<string>(series.Data.Count);
        for (var index = 0; index < series.Data.Count; index++)
        {
            var point = series.Data[index];
            var start = stacked ? StackBaseline(series, index, point.Y) : 0;
            top.Add(Project(point.X, start + point.Y));
            baseline.Add(Project(point.X, start));
        }
        baseline.Reverse();
        return string.Join(' ', top.Concat(baseline));
    }

    internal (double X, double Y, double Width, double Height) ColumnRect(object owner, int index, bool stacked)
    {
        var series = GetSeries(owner);
        var point = series.Data[index];
        var count = Math.Max(1, series.Data.Count);
        var slot = 80d / count;
        var start = stacked ? StackBaseline(series, index, point.Y) : 0;
        var first = 100 - ProjectY(start);
        var second = 100 - ProjectY(start + point.Y);
        return (10 + index * slot, Math.Min(first, second), slot * 0.75, Math.Abs(first - second));
    }

    internal (double X, double Y, double Width, double Height) BarRect(object owner, int index)
    {
        var series = GetSeries(owner);
        var point = series.Data[index];
        var count = Math.Max(1, series.Data.Count);
        var slot = 80d / count;
        var first = ProjectHorizontalValue(0);
        var second = ProjectHorizontalValue(point.Y);
        return (Math.Min(first, second), 10 + index * slot, Math.Abs(first - second), slot * 0.75);
    }

    internal string Project(OmniChartPoint point) => Project(point.X, point.Y);
    internal (double X, double Y) ProjectCoordinates(OmniChartPoint point) =>
        (ProjectToPlot(point.X, XDomain), 100 - ProjectToPlot(point.Y, ValueDomain));
    internal double ProjectY(double value) => ProjectToPlot(value, ValueDomain);
    internal double CategoryPosition(int index, int count) => count <= 1 ? 50 : PlotStart + index * (PlotEnd - PlotStart) / (count - 1);

    private string Project(double x, double y)
    {
        var point = ProjectCoordinates(new OmniChartPoint(x, y));
        return $"{OmniChartGeometry.Number(point.X)},{OmniChartGeometry.Number(point.Y)}";
    }

    private double ProjectHorizontalValue(double value) => ProjectToPlot(value, ValueDomain);

    private (double Minimum, double Maximum) XDomain
    {
        get
        {
            EnsureDomains();
            return _xDomain;
        }
    }

    private (double Minimum, double Maximum) ValueDomain
    {
        get
        {
            EnsureDomains();
            return _valueDomain;
        }
    }

    private void EnsureDomains()
    {
        if (!_domainsDirty)
        {
            return;
        }

        var xValues = _series.SelectMany(item => item.Data).Select(point => point.X).ToArray();
        _xDomain = Expand(xValues.Length == 0 ? (0d, 1d) : (xValues.Min(), xValues.Max()));

        if (_valueAxes.Count > 0)
        {
            _valueDomain = Expand((_valueAxes.Values.Min(item => item.Minimum), _valueAxes.Values.Max(item => item.Maximum)));
        }
        else
        {
            var values = new List<double> { 0 };
            foreach (var series in _series.Where(item => item.Kind is not OmniChartSeriesKind.StackedArea and not OmniChartSeriesKind.StackedColumn))
            {
                values.AddRange(series.Data.Select(point => point.Y));
            }
            foreach (var kind in new[] { OmniChartSeriesKind.StackedArea, OmniChartSeriesKind.StackedColumn })
            {
                var stacked = _series.Where(item => item.Kind == kind).ToArray();
                var maximumCount = stacked.Length == 0 ? 0 : stacked.Max(item => item.Data.Count);
                for (var index = 0; index < maximumCount; index++)
                {
                    var positive = stacked.Where(item => index < item.Data.Count).Select(item => item.Data[index].Y).Where(value => value > 0).Sum();
                    var negative = stacked.Where(item => index < item.Data.Count).Select(item => item.Data[index].Y).Where(value => value < 0).Sum();
                    values.Add(positive);
                    values.Add(negative);
                }
            }
            _valueDomain = Expand((values.Min(), values.Max()));
        }

        DomainCalculationCount++;
        _domainsDirty = false;
    }

    private double StackBaseline(SeriesRegistration current, int index, double value)
    {
        var baseline = 0d;
        foreach (var series in _series)
        {
            if (ReferenceEquals(series.Owner, current.Owner))
            {
                break;
            }
            if (series.Kind != current.Kind || index >= series.Data.Count)
            {
                continue;
            }
            var previous = series.Data[index].Y;
            if (value >= 0 && previous >= 0 || value < 0 && previous < 0)
            {
                baseline += previous;
            }
        }
        return baseline;
    }

    private SeriesRegistration GetSeries(object owner) =>
        _series.First(item => ReferenceEquals(item.Owner, owner));

    private static double ProjectToPlot(double value, (double Minimum, double Maximum) domain)
    {
        var ratio = (value - domain.Minimum) / (domain.Maximum - domain.Minimum);
        return PlotStart + Math.Clamp(ratio, 0, 1) * (PlotEnd - PlotStart);
    }

    private static (double Minimum, double Maximum) Expand((double Minimum, double Maximum) domain) =>
        domain.Minimum.Equals(domain.Maximum)
            ? (domain.Minimum - 0.5, domain.Maximum + 0.5)
            : domain;

    private sealed record SeriesRegistration(object Owner, OmniChartSeriesKind Kind, IReadOnlyList<OmniChartPoint> Data);
}
