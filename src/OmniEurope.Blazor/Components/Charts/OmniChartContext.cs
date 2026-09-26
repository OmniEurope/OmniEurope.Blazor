using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The layout every part of one <see cref="OmniChart"/> shares: the plot rectangle inside the view box
/// (100 high, 100 wide times the chart's aspect ratio, centred on x = 50 so a pie stays in the middle), the value and X domains, and the category bands columns and bars sit in. Series,
/// axes, grid lines, legend and titles all read their coordinates here, so they line up by
/// construction rather than by each repeating the same constants.
/// </summary>
internal sealed class OmniChartContext
{
    /// <summary>
    /// More categories than this make a time series: without an explicit aspect ratio the chart
    /// widens to <see cref="TimeSeriesAspectRatio"/> so a month of days is not drawn in a tall square.
    /// </summary>
    internal const int TimeSeriesThreshold = 12;

    internal const double TimeSeriesAspectRatio = 2;

    /// <summary>Font size of every chart text, in view-box units (the stylesheet's 3px).</summary>
    internal const double FontSize = 3;

    /// <summary>
    /// Axis text of a wide chart on a narrow screen (the stylesheet's 4.5px under 40rem): the wide
    /// drawing shrinks more than a square one, so its axis text is drawn larger there, and labels are
    /// thinned for that size.
    /// </summary>
    internal const double WideAxisFontSize = 4.5;

    /// <summary>Wider than high: the stylesheet enlarges axis text on narrow screens.</summary>
    internal bool IsWide => Spread > 0;

    private double AxisFontSize => IsWide ? WideAxisFontSize : FontSize;

    /// <summary>
    /// Estimated advance of one character at <see cref="FontSize"/>: 0.57 em, a little above the
    /// average glyph of a sans-serif face, so an estimate errs towards more room.
    /// </summary>
    internal const double CharacterWidth = 1.7;

    /// <summary>The legend column right of the plot, from the plot's edge to the view box's edge.</summary>
    private const double DefaultLegendColumn = 24;

    /// <summary>From the plot's edge to the legend text: 3 of gap, a 3-wide swatch and 1.5 of gap.</summary>
    private const double LegendTextOffset = 7.5;

    private double? _explicitAspectRatio;

    /// <summary>
    /// Width over height of the drawing: the one the chart sets, else <see cref="TimeSeriesAspectRatio"/>
    /// for a time series and 1 otherwise.
    /// </summary>
    internal double AspectRatio => _explicitAspectRatio ?? (IsTimeSeries ? TimeSeriesAspectRatio : 1);

    /// <summary>
    /// Extra view-box width on each side of the 0-100 square when the chart is wider than high: the
    /// plot stretches into it while text keeps its size and pies stay centred.
    /// </summary>
    internal double Spread => (100 * Math.Max(1, AspectRatio) - 100) / 2;

    /// <summary>
    /// A vertical chart with more categories (labels or points of a series) than
    /// <see cref="TimeSeriesThreshold"/>. Horizontal bars grow downwards and never count.
    /// </summary>
    internal bool IsTimeSeries
    {
        get
        {
            if (Horizontal)
            {
                return false;
            }

            var labels = _categoryAxes.Values.Select(item => item.Count).DefaultIfEmpty(0).Max();
            var points = _series
                .Where(item => item.Kind != OmniChartSeriesKind.Auxiliary)
                .Select(item => item.Data.Count)
                .DefaultIfEmpty(0)
                .Max();
            return Math.Max(labels, points) > TimeSeriesThreshold;
        }
    }

    /// <summary>Left edge of the view box.</summary>
    internal double ViewLeft => Spread == 0 ? 0 : -Spread;

    /// <summary>Width of the view box.</summary>
    internal double ViewWidth => 100 + (2 * Spread);

    /// <summary>Left edge of the plot; the value labels and a vertical axis title live to its left.</summary>
    internal double PlotLeft => ViewLeft + 14;

    internal const double PlotTop = 4;

    /// <summary>Bottom edge of the plot; the category labels and a horizontal axis title live below it.</summary>
    internal const double PlotBottom = 86;

    private double PlotRightAlone => ViewLeft + ViewWidth - 4;

    /// <summary>
    /// With a legend on the right the plot stops here, and the legend takes the column to its right:
    /// 24 wide, widened to the longest entry up to 40 % of the view box.
    /// </summary>
    private double PlotRightWithLegend => ViewLeft + ViewWidth - LegendColumn;

    private double LegendColumn
    {
        get
        {
            var needed = _legends.Values
                .Where(item => !IsBelow(item))
                .Select(ColumnNeeded)
                .DefaultIfEmpty(DefaultLegendColumn)
                .Max();
            return Math.Clamp(needed, DefaultLegendColumn, Math.Max(DefaultLegendColumn, 0.4 * ViewWidth));
        }
    }

    /// <summary>Share of a category band the columns of that category occupy together.</summary>
    private const double BandFill = 0.8;

    private readonly List<SeriesRegistration> _series = [];
    private readonly Dictionary<object, (double Minimum, double Maximum)> _valueAxes = [];
    private readonly Dictionary<object, IReadOnlyList<string>> _categoryAxes = [];
    private readonly Dictionary<object, LegendRegistration> _legends = [];
    private bool _domainsDirty = true;
    private (double Minimum, double Maximum) _xDomain = (0, 1);
    private (double Minimum, double Maximum) _valueDomain = (0, 1);

    internal int DomainCalculationCount { get; private set; }

    internal event Action? Changed;

    internal double PlotRight => _legends.Values.Any(item => !IsBelow(item)) ? PlotRightWithLegend : PlotRightAlone;

    /// <summary>The legend column starts just right of the plot.</summary>
    internal double LegendLeft => PlotRightWithLegend + 3;

    /// <summary>The legends drawn below the chart, in HTML, in the order they registered.</summary>
    internal IEnumerable<LegendRegistration> LegendsBelow => _legends.Values.Where(IsBelow);

    /// <summary>Whether the legend <paramref name="owner"/> registered is drawn below the chart.</summary>
    internal bool IsLegendBelow(object owner) => _legends.TryGetValue(owner, out var legend) && IsBelow(legend);

    /// <summary>
    /// Widens the view box to <paramref name="aspectRatio"/> (width over height, 1 = square); null
    /// lets the chart choose, wide for a time series and square otherwise.
    /// </summary>
    internal void SetAspectRatio(double? aspectRatio)
    {
        if (Nullable.Equals(_explicitAspectRatio, aspectRatio)) return;
        _explicitAspectRatio = aspectRatio;
        Changed?.Invoke();
    }

    /// <summary>The label the category axis gives category <paramref name="index"/>, if any.</summary>
    internal string? CategoryLabel(int index)
    {
        var labels = _categoryAxes.Values.MaxBy(item => item.Count);
        return labels is not null && index >= 0 && index < labels.Count ? labels[index] : null;
    }

    /// <summary>
    /// The hover text of one point: its own label, else its category and value, so a category whose
    /// axis label was thinned out still names its date or name.
    /// </summary>
    internal string PointTitle(int index, OmniChartPoint point) =>
        point.Label ?? (CategoryLabel(index) is { } category
            ? string.Create(CultureInfo.CurrentCulture, $"{category} · {point.Y}")
            : point.Y.ToString(CultureInfo.CurrentCulture));

    /// <summary>
    /// The categories whose label is drawn: all of them when they fit, otherwise one every so many so
    /// that no two labels overlap, the first and the last always kept. Label widths are estimated
    /// from their length, so the step errs towards more room.
    /// </summary>
    internal IReadOnlyList<int> VisibleCategoryIndexes(IReadOnlyList<string> labels)
    {
        var count = labels.Count;
        if (count <= 2)
        {
            return [.. Enumerable.Range(0, count)];
        }

        var pitch = Math.Abs(CategoryPosition(1, count) - CategoryPosition(0, count));
        var needed = Horizontal
            ? AxisFontSize * 1.3
            : labels.Max(label => (label?.Length ?? 0) * CharacterWidth * AxisFontSize / FontSize) + 1.5;
        var step = pitch <= 0 ? count : Math.Max(1, (int)Math.Ceiling(needed / pitch));
        if (step == 1)
        {
            return [.. Enumerable.Range(0, count)];
        }

        var visible = new List<int>();
        for (var index = 0; index < count - 1; index += step)
        {
            visible.Add(index);
        }

        // The last label replaces the one before it when the two would be closer than a step.
        if (visible.Count > 1 && count - 1 - visible[^1] < step)
        {
            visible.RemoveAt(visible.Count - 1);
        }

        visible.Add(count - 1);
        return visible;
    }

    /// <summary>Horizontal bars turn the chart: values run along the bottom, categories down the left.</summary>
    internal bool Horizontal => _series.Any(item => item.Kind == OmniChartSeriesKind.Bar);

    /// <summary>
    /// Columns and bars need bands, one per category, so a category label sits under the middle of
    /// its columns; lines alone are plotted edge to edge.
    /// </summary>
    internal bool Banded => _series.Any(item => IsBanded(item.Kind));

    internal int CategoryCount
    {
        get
        {
            var labels = _categoryAxes.Values.Select(item => item.Count).DefaultIfEmpty(0).Max();
            var points = _series
                .Where(item => IsBanded(item.Kind))
                .Select(item => item.Data.Count)
                .DefaultIfEmpty(0)
                .Max();
            return Math.Max(1, Math.Max(labels, points));
        }
    }

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

    internal void RegisterLegend(object owner, LegendRegistration legend)
    {
        if (_legends.TryGetValue(owner, out var current) && current.SameAs(legend))
        {
            return;
        }
        _legends[owner] = legend;
        Changed?.Invoke();
    }

    internal void UnregisterLegend(object owner)
    {
        if (_legends.Remove(owner))
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

    /// <summary>
    /// A column inside its category band. Column series stand side by side in the band, every stacked
    /// series sharing one place, so two series of the same category never cover each other.
    /// </summary>
    internal (double X, double Y, double Width, double Height) ColumnRect(object owner, int index, bool stacked)
    {
        var series = GetSeries(owner);
        var point = series.Data[index];
        var (slot, slots) = SlotOf(series, OmniChartSeriesKind.Column, OmniChartSeriesKind.StackedColumn);
        var (start, width) = SlotSpan(PlotLeft, PlotRight, index, slot, slots);
        var from = stacked ? StackBaseline(series, index, point.Y) : 0;
        var first = ValueToY(from);
        var second = ValueToY(from + point.Y);
        return (start, Math.Min(first, second), width, Math.Abs(first - second));
    }

    /// <summary>A horizontal bar inside its category band, bar series standing one above the other.</summary>
    internal (double X, double Y, double Width, double Height) BarRect(object owner, int index)
    {
        var series = GetSeries(owner);
        var point = series.Data[index];
        var (slot, slots) = SlotOf(series, OmniChartSeriesKind.Bar, null);
        var (start, height) = SlotSpan(PlotTop, PlotBottom, index, slot, slots);
        var first = ValueToX(0);
        var second = ValueToX(point.Y);
        return (Math.Min(first, second), start, Math.Abs(first - second), height);
    }

    internal string Project(OmniChartPoint point) => Project(point.X, point.Y);

    /// <summary>Where a data point lands: its X along the categories, its value across them.</summary>
    internal (double X, double Y) ProjectCoordinates(OmniChartPoint point) =>
        Horizontal
            ? (ValueToX(point.Y), XToPosition(point.X, PlotTop, PlotBottom))
            : (XToPosition(point.X, PlotLeft, PlotRight), ValueToY(point.Y));

    /// <summary>The SVG y of a value on a vertical value axis.</summary>
    internal double ValueToY(double value) => PlotBottom - (Ratio(value, ValueDomain) * (PlotBottom - PlotTop));

    /// <summary>The SVG x of a value on a horizontal value axis.</summary>
    internal double ValueToX(double value) => PlotLeft + (Ratio(value, ValueDomain) * (PlotRight - PlotLeft));

    /// <summary>
    /// Where the label of category <paramref name="index"/> of <paramref name="count"/> sits along the
    /// category axis: in the middle of its band when the chart has bands, edge to edge otherwise.
    /// </summary>
    internal double CategoryPosition(int index, int count)
    {
        var (start, end) = Horizontal ? (PlotTop, PlotBottom) : (PlotLeft, PlotRight);
        if (Banded)
        {
            var band = (end - start) / Math.Max(1, count);
            return start + ((index + 0.5) * band);
        }

        return count <= 1 ? (start + end) / 2 : start + (index * (end - start) / (count - 1));
    }

    private static bool IsBanded(OmniChartSeriesKind kind) =>
        kind is OmniChartSeriesKind.Bar or OmniChartSeriesKind.Column or OmniChartSeriesKind.StackedColumn;

    private string Project(double x, double y)
    {
        var point = ProjectCoordinates(new OmniChartPoint(x, y));
        return $"{OmniChartGeometry.Number(point.X)},{OmniChartGeometry.Number(point.Y)}";
    }

    /// <summary>
    /// Places an X value between two edges. With bands the extreme values land in the middle of the
    /// first and last bands, so a line drawn over columns passes above their centres.
    /// </summary>
    private double XToPosition(double x, double start, double end)
    {
        if (Banded)
        {
            var half = (end - start) / CategoryCount / 2;
            start += half;
            end -= half;
        }

        return start + (Ratio(x, XDomain) * (end - start));
    }

    /// <summary>
    /// The place of a series among those sharing a band: every series of the separate kind takes its
    /// own place, and all the series of the shared kind (the stacked ones) take a single place, where
    /// the first of them appears.
    /// </summary>
    private (int Slot, int Slots) SlotOf(SeriesRegistration series, OmniChartSeriesKind separate, OmniChartSeriesKind? shared)
    {
        var slot = 0;
        var slots = 0;
        var sharedSlot = -1;
        foreach (var item in _series)
        {
            var isShared = shared is { } kind && item.Kind == kind;
            if (item.Kind != separate && !isShared)
            {
                continue;
            }

            int place;
            if (isShared)
            {
                if (sharedSlot < 0)
                {
                    sharedSlot = slots++;
                }

                place = sharedSlot;
            }
            else
            {
                place = slots++;
            }

            if (ReferenceEquals(item.Owner, series.Owner))
            {
                slot = place;
            }
        }

        return (slot, Math.Max(1, slots));
    }

    private (double Start, double Size) SlotSpan(double from, double to, int index, int slot, int slots)
    {
        var band = (to - from) / CategoryCount;
        var group = band * BandFill;
        var size = group / slots;
        var start = from + (index * band) + ((band - group) / 2) + (slot * size);
        return (start + (size * 0.05), size * 0.9);
    }

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

    private static double Ratio(double value, (double Minimum, double Maximum) domain) =>
        Math.Clamp((value - domain.Minimum) / (domain.Maximum - domain.Minimum), 0, 1);

    private static (double Minimum, double Maximum) Expand((double Minimum, double Maximum) domain) =>
        domain.Minimum.Equals(domain.Maximum)
            ? (domain.Minimum - 0.5, domain.Maximum + 0.5)
            : domain;

    /// <summary>
    /// Below when asked, or, for <see cref="OmniLegendPosition.Auto"/>, when the longest entry needs a
    /// wider column than the default one and than a fifth of the view box.
    /// </summary>
    private bool IsBelow(LegendRegistration legend) => legend.Position switch
    {
        OmniLegendPosition.Bottom => true,
        OmniLegendPosition.Right => false,
        _ => ColumnNeeded(legend) > Math.Max(DefaultLegendColumn, 0.2 * ViewWidth)
    };

    private static double ColumnNeeded(LegendRegistration legend) =>
        LegendTextOffset + legend.Items.Select(item => (item?.Length ?? 0) * CharacterWidth).DefaultIfEmpty(0).Max() + 1;

    /// <summary>What one <see cref="OmniLegend"/> shows, which the chart needs to draw it below the plot.</summary>
    internal sealed record LegendRegistration(
        string Label,
        IReadOnlyList<string> Items,
        IReadOnlyList<int> ColorIndexes,
        OmniLegendPosition Position)
    {
        /// <summary>The colour class index of entry <paramref name="index"/>, as the legend draws it.</summary>
        internal int ColorOf(int index) => Math.Abs(index < ColorIndexes.Count ? ColorIndexes[index] : index) % 8;

        internal bool SameAs(LegendRegistration other) =>
            Label == other.Label
            && Position == other.Position
            && Items.SequenceEqual(other.Items)
            && ColorIndexes.SequenceEqual(other.ColorIndexes);
    }

    private sealed record SeriesRegistration(object Owner, OmniChartSeriesKind Kind, IReadOnlyList<OmniChartPoint> Data);
}
