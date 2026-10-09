using OmniEurope.Blazor.Internal;

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

    /// <summary>The name under which a chart hands its layout to the HTML parts of its <c>FooterContent</c>.</summary>
    internal const string FooterName = "OmniChartFooter";

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

    /// <summary>
    /// Left edge of the plot; the value labels and a vertical axis title live to its left. The margin is
    /// <see cref="MinimumLeftMargin"/>, widened on a vertical chart with a value axis to its longest
    /// graduation, so a long amount ("100 000,00 €") is never drawn outside the chart (recette R-010).
    /// </summary>
    internal double PlotLeft => ViewLeft + Math.Max(MinimumLeftMargin, ValueLabelWidth);

    /// <summary>The margin left of the plot whatever its labels: room for short graduations and an axis title.</summary>
    private const double MinimumLeftMargin = 14;

    private double? _valueLabelWidth;

    /// <summary>
    /// Width the value graduations need left of a vertical plot: the longest one at
    /// <see cref="CharacterWidth"/> per character, plus the 1.5 gap before the axis and one unit of
    /// edge. Zero on a horizontal chart, whose values run along the bottom, and without a value axis.
    /// </summary>
    private double ValueLabelWidth
    {
        get
        {
            if (Horizontal || (_valueAxes.Count == 0 && _automaticValueAxes.Count == 0))
            {
                return 0;
            }

            EnsureDomains();
            return _valueLabelWidth ??= MeasureValueLabels();
        }
    }

    private double MeasureValueLabels()
    {
        var (minimum, maximum) = _valueDomain;
        var intervals = _automaticValueAxes.Count > 0 ? _automaticValueAxes.Values.Max() : 5;
        var longest = Enumerable.Range(0, intervals + 1)
            .Select(index => FormatValue(minimum + ((maximum - minimum) * index / intervals)).Length)
            .Max();
        return (longest * CharacterWidth) + 2.5;
    }

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
                .Where(item => !IsOutside(item))
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
    private readonly Dictionary<object, int> _automaticValueAxes = [];
    private readonly Dictionary<object, Func<double, string>> _valueFormats = [];
    private readonly Dictionary<object, IReadOnlyList<string>> _categoryAxes = [];
    private readonly Dictionary<object, LegendRegistration> _legends = [];
    private readonly List<PieRegistration> _pies = [];
    private readonly HashSet<int> _hiddenColors = [];
    private readonly OmniChartRange _range = new();
    private bool _domainsDirty = true;
    private (double Minimum, double Maximum) _xDomain = (0, 1);
    private (double Minimum, double Maximum) _valueDomain = (0, 1);

    internal int DomainCalculationCount { get; private set; }

    internal event Action? Changed;

    internal double PlotRight => _legends.Values.Any(item => !IsOutside(item)) ? PlotRightWithLegend : PlotRightAlone;

    /// <summary>The legend column starts just right of the plot.</summary>
    internal double LegendLeft => PlotRightWithLegend + 3;

    /// <summary>The legends drawn above the chart, in HTML, in the order they registered.</summary>
    internal IEnumerable<LegendRegistration> LegendsAbove =>
        _legends.Values.Where(item => item.Position == OmniLegendPosition.Top);

    /// <summary>The legends drawn below the chart, in HTML, in the order they registered.</summary>
    internal IEnumerable<LegendRegistration> LegendsBelow =>
        _legends.Values.Where(item => item.Position != OmniLegendPosition.Top && IsOutside(item));

    /// <summary>
    /// Whether the legend <paramref name="owner"/> registered is drawn outside the drawing, as an HTML
    /// list below or above it, rather than in the column right of the plot.
    /// </summary>
    internal bool IsLegendOutside(object owner) => _legends.TryGetValue(owner, out var legend) && IsOutside(legend);

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

    /// <summary>The number of labels of the longest category axis, zero without one.</summary>
    internal int CategoryLabelCount => _categoryAxes.Values.Select(item => item.Count).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Keeps the <see cref="OmniValueAxis.FormatValue"/> of an axis, or forgets it when null. Not a
    /// change of layout, so it notifies nobody: a host lambda is a new delegate on every render, and
    /// a notification would redraw the chart forever. The parts that read it render after the axis.
    /// </summary>
    internal void SetValueFormat(object owner, Func<double, string>? format)
    {
        _valueLabelWidth = null;
        if (format is null)
        {
            _valueFormats.Remove(owner);
        }
        else
        {
            _valueFormats[owner] = format;
        }
    }

    /// <summary>A value as the value axis writes it: its <c>FormatValue</c> when it has one, else the current culture.</summary>
    internal string FormatValue(double value) =>
        _valueFormats.Values.FirstOrDefault() is { } format ? format(value) : OmniChartGeometry.Display(value);

    /// <summary>
    /// The hover text of one point: its own label, else its category and value, so a category whose
    /// axis label was thinned out still names its date or name. The value is written in the current
    /// culture, the category and the value separated by <see cref="OmniChartGeometry.Separator"/>.
    /// </summary>
    internal string PointTitle(int index, OmniChartPoint point) =>
        point.Label ?? (CategoryLabel(index) is { } category
            ? OmniChartGeometry.Pair(category, OmniChartGeometry.Display(point.Y))
            : OmniChartGeometry.Display(point.Y));

    /// <summary>Whether the series of colour <paramref name="colorIndex"/> is hidden through a legend entry.</summary>
    internal bool IsHidden(int colorIndex) => _hiddenColors.Contains(colorIndex);

    /// <summary>
    /// Whether a decoration drawn on <paramref name="data"/> (markers, data labels) belongs to a hidden
    /// series: one with the same points, so that hiding a series also hides what decorates it.
    /// </summary>
    internal bool IsDataHidden(IReadOnlyList<OmniChartPoint> data) =>
        _hiddenColors.Count > 0
        && _series.Any(item => item.Kind != OmniChartSeriesKind.Auxiliary && IsHidden(item.ColorIndex) && item.Data.SequenceEqual(data));

    /// <summary>
    /// Hides the series of colour <paramref name="colorIndex"/>, or shows it again: the value domain,
    /// the stacks and the column slots are computed again without it, and the chart redraws.
    /// </summary>
    internal void ToggleSeries(int colorIndex)
    {
        if (!_hiddenColors.Remove(colorIndex))
        {
            _hiddenColors.Add(colorIndex);
        }

        _domainsDirty = true;
        Changed?.Invoke();
    }

    /// <summary>The categories shown, first and last index included; null while every category is shown.</summary>
    internal (int First, int Last)? Range => _range.Window;

    /// <summary>
    /// Shows only categories <paramref name="first"/> to <paramref name="last"/> (indexes, both
    /// included): the series, the category axis, the hover bands and the data table keep the points of
    /// those indexes, and the value and X domains are computed from them alone. One range per chart;
    /// the owner that set it is the one that can clear it.
    /// </summary>
    internal void SetRange(object owner, int first, int last) => RangeChanged(_range.Set(owner, first, last));

    /// <summary>Shows every category again, when <paramref name="owner"/> is the one that set the range.</summary>
    internal void ClearRange(object owner) => RangeChanged(_range.Clear(owner));

    private void RangeChanged(bool changed)
    {
        if (changed)
        {
            _domainsDirty = true;
            Changed?.Invoke();
        }
    }

    /// <summary>Whether category <paramref name="index"/> is shown: always without a range.</summary>
    internal bool InRange(int index) => _range.Contains(index);

    /// <summary>
    /// Where a point outside the range shown is sent by <see cref="ProjectCoordinates"/>: far above the
    /// view box, out of the clip the chart sets while a range is active. Only a decoration that walks
    /// its own points without asking <see cref="InRange"/> (data labels) ever lands there.
    /// </summary>
    internal const double OutOfRangeY = -1000;

    /// <summary>A series the plot draws: not hidden, or a decoration of a series that is not hidden.</summary>
    private bool IsDrawn(SeriesRegistration series) =>
        _hiddenColors.Count == 0
        || (series.Kind == OmniChartSeriesKind.Auxiliary ? !IsDataHidden(series.Data) : !IsHidden(series.ColorIndex));

    private IEnumerable<SeriesRegistration> DrawnSeries => _series.Where(IsDrawn);

    /// <summary>The series a data table lists: every drawn series (markers and data labels excepted), in the order they registered.</summary>
    internal IReadOnlyList<ChartSeriesView> TableSeries =>
        [.. _series.Where(item => item.Kind != OmniChartSeriesKind.Auxiliary).Select(item => new ChartSeriesView(item.Title, item.ColorIndex, item.Data))];

    /// <summary>The pie and donut series of the chart, in the order they registered.</summary>
    internal IReadOnlyList<PieRegistration> Pies => _pies;

    /// <summary>
    /// The entries a legend shows: its own <see cref="LegendRegistration.Items"/> when it has some,
    /// each taking the colour of the series at the same position (its position in the palette past the
    /// last series); otherwise the slices of the first pie, or else every titled series with its own
    /// colour.
    /// </summary>
    internal IReadOnlyList<LegendEntry> LegendEntries(LegendRegistration legend)
    {
        if (legend.Items is { Count: > 0 } items)
        {
            var drawn = TableSeries;
            return [.. items.Select((text, index) => index < drawn.Count
                ? new LegendEntry(text, ChartColor.Slot(drawn[index].ColorIndex), drawn[index].ColorIndex)
                : new LegendEntry(text, ChartColor.Slot(index), null))];
        }

        if (_pies.Count > 0)
        {
            return [.. _pies[0].Slices.Select((slice, index) => new LegendEntry(slice.Label, ChartColor.Slot(index), null))];
        }

        return [.. TableSeries
            .Where(series => !string.IsNullOrWhiteSpace(series.Title))
            .Select(series => new LegendEntry(series.Title!, ChartColor.Slot(series.ColorIndex), series.ColorIndex))];
    }

    /// <summary>
    /// The categories whose label is drawn: all of them when they fit, otherwise one every so many so
    /// that no two labels overlap, the first and the last always kept. Label widths are estimated
    /// from their length, so the step errs towards more room. While a range is shown, only the
    /// categories of the range are candidates, its first and last kept.
    /// </summary>
    internal IReadOnlyList<int> VisibleCategoryIndexes(IReadOnlyList<string> labels)
    {
        var (first, last) = _range.Bounds(labels.Count);
        var count = last - first + 1;
        if (count <= 2)
        {
            return [.. Enumerable.Range(first, Math.Max(0, count))];
        }

        var pitch = Math.Abs(CategoryPosition(first + 1, labels.Count) - CategoryPosition(first, labels.Count));
        var needed = Horizontal
            ? AxisFontSize * 1.3
            : labels.Skip(first).Take(count).Max(label => (label?.Length ?? 0) * CharacterWidth * AxisFontSize / FontSize) + 1.5;
        return OmniChartRange.Thin(first, last, pitch <= 0 ? count : Math.Max(1, (int)Math.Ceiling(needed / pitch)));
    }

    /// <summary>Horizontal bars turn the chart: values run along the bottom, categories down the left.</summary>
    internal bool Horizontal => _series.Any(item => item.Kind is OmniChartSeriesKind.Bar or OmniChartSeriesKind.StackedBar);

    /// <summary>
    /// Columns and bars need bands, one per category, so a category label sits under the middle of
    /// its columns; lines alone are plotted edge to edge.
    /// </summary>
    internal bool Banded => _series.Any(item => IsBanded(item.Kind));

    internal int CategoryCount
    {
        get
        {
            if (_range.Window is { } range)
            {
                return Math.Max(1, range.Last - range.First + 1);
            }

            var labels = _categoryAxes.Values.Select(item => item.Count).DefaultIfEmpty(0).Max();
            var points = _series
                .Where(item => IsBanded(item.Kind))
                .Select(item => item.Data.Count)
                .DefaultIfEmpty(0)
                .Max();
            return Math.Max(1, Math.Max(labels, points));
        }
    }

    /// <summary>
    /// Adds or updates a series: its kind, its points, and the title and colour a legend and the data
    /// table name it with (none for markers and data labels, which only decorate another series).
    /// </summary>
    internal void RegisterSeries(object owner, OmniChartSeriesKind kind, IReadOnlyList<OmniChartPoint> data, string? title = null, int colorIndex = 0)
    {
        var snapshot = data.ToArray();
        var index = _series.FindIndex(item => ReferenceEquals(item.Owner, owner));
        if (index >= 0
            && _series[index].Kind == kind
            && _series[index].Title == title
            && _series[index].ColorIndex == colorIndex
            && _series[index].Data.SequenceEqual(snapshot))
        {
            return;
        }

        var registration = new SeriesRegistration(owner, kind, snapshot, title, colorIndex);
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

    /// <summary>Adds or updates a pie or donut series: its title and the slices it draws, positive values only.</summary>
    internal void RegisterPie(object owner, string? title, IReadOnlyList<OmniChartSlice> slices)
    {
        var snapshot = slices.ToArray();
        var index = _pies.FindIndex(item => ReferenceEquals(item.Owner, owner));
        if (index >= 0 && _pies[index].Title == title && _pies[index].Slices.SequenceEqual(snapshot))
        {
            return;
        }

        var registration = new PieRegistration(owner, title, snapshot);
        if (index >= 0)
        {
            _pies[index] = registration;
        }
        else
        {
            _pies.Add(registration);
        }

        Changed?.Invoke();
    }

    internal void UnregisterPie(object owner)
    {
        if (_pies.RemoveAll(item => ReferenceEquals(item.Owner, owner)) > 0)
        {
            Changed?.Invoke();
        }
    }

    internal void RegisterValueAxis(object owner, double minimum, double maximum)
    {
        var wasAutomatic = _automaticValueAxes.Remove(owner);
        var bounds = (minimum, maximum);
        if (!wasAutomatic && _valueAxes.TryGetValue(owner, out var current) && current == bounds)
        {
            return;
        }
        _valueAxes[owner] = bounds;
        _domainsDirty = true;
        Changed?.Invoke();
    }

    /// <summary>An axis whose bounds follow the series, rounded outward for <paramref name="tickCount"/> graduations.</summary>
    internal void RegisterAutomaticValueAxis(object owner, int tickCount)
    {
        _valueAxes.Remove(owner);
        if (_automaticValueAxes.TryGetValue(owner, out var current) && current == tickCount)
        {
            return;
        }
        _automaticValueAxes[owner] = tickCount;
        _domainsDirty = true;
        Changed?.Invoke();
    }

    /// <summary>The value bounds the plot uses, for an automatic axis to draw its graduations on.</summary>
    internal (double Minimum, double Maximum) ValueBounds => ValueDomain;

    internal void UnregisterValueAxis(object owner)
    {
        _valueFormats.Remove(owner);
        if (_valueAxes.Remove(owner) | _automaticValueAxes.Remove(owner))
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

    internal string Points(object owner) => string.Join(' ', _range.Points(GetSeries(owner).Data).Select(Project));

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
            if (!InRange(index))
            {
                continue;
            }

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

    /// <summary>
    /// A horizontal bar inside its category band. Bar series stand one above the other in the band,
    /// every stacked series sharing one place, each value starting where the previous one ended.
    /// </summary>
    internal (double X, double Y, double Width, double Height) BarRect(object owner, int index, bool stacked)
    {
        var series = GetSeries(owner);
        var point = series.Data[index];
        var (slot, slots) = SlotOf(series, OmniChartSeriesKind.Bar, OmniChartSeriesKind.StackedBar);
        var (start, height) = SlotSpan(PlotTop, PlotBottom, index, slot, slots);
        var from = stacked ? StackBaseline(series, index, point.Y) : 0;
        var first = ValueToX(from);
        var second = ValueToX(from + point.Y);
        return (Math.Min(first, second), start, Math.Abs(first - second), height);
    }

    internal string Project(OmniChartPoint point) => Project(point.X, point.Y);

    /// <summary>
    /// Where a data point lands: its X along the categories, its value across them. While a range is
    /// shown, a point whose X lies outside the X of the categories shown lands at <see cref="OutOfRangeY"/>.
    /// </summary>
    internal (double X, double Y) ProjectCoordinates(OmniChartPoint point) =>
        _range.Window is not null && (point.X < XDomain.Minimum || point.X > XDomain.Maximum)
            ? (PlotLeft, OutOfRangeY)
            : Horizontal
            ? (ValueToX(point.Y), XToPosition(point.X, PlotTop, PlotBottom))
            : (XToPosition(point.X, PlotLeft, PlotRight), ValueToY(point.Y));

    /// <summary>The SVG y of a value on a vertical value axis.</summary>
    internal double ValueToY(double value) => PlotBottom - (Ratio(value, ValueDomain) * (PlotBottom - PlotTop));

    /// <summary>The SVG x of a value on a horizontal value axis.</summary>
    internal double ValueToX(double value) => PlotLeft + (Ratio(value, ValueDomain) * (PlotRight - PlotLeft));

    /// <summary>
    /// Where the label of category <paramref name="index"/> of <paramref name="count"/> sits along the
    /// category axis: in the middle of its band when the chart has bands, edge to edge otherwise. While a
    /// range is shown, the categories of the range share the axis and <paramref name="count"/> is theirs.
    /// </summary>
    internal double CategoryPosition(int index, int count)
    {
        (index, count) = _range.Local(index, count);

        var (start, end) = Horizontal ? (PlotTop, PlotBottom) : (PlotLeft, PlotRight);
        if (Banded)
        {
            var band = (end - start) / Math.Max(1, count);
            return start + ((index + 0.5) * band);
        }

        return count <= 1 ? (start + end) / 2 : start + (index * (end - start) / (count - 1));
    }

    private static bool IsBanded(OmniChartSeriesKind kind) =>
        kind is OmniChartSeriesKind.Bar or OmniChartSeriesKind.StackedBar or OmniChartSeriesKind.Column or OmniChartSeriesKind.StackedColumn;

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
    private (int Slot, int Slots) SlotOf(SeriesRegistration series, OmniChartSeriesKind separate, OmniChartSeriesKind shared)
    {
        var slot = 0;
        var slots = 0;
        var sharedSlot = -1;
        foreach (var item in DrawnSeries)
        {
            var isShared = item.Kind == shared;
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
        index -= _range.First;
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

        var xValues = DrawnSeries.SelectMany(item => _range.Points(item.Data)).Select(point => point.X).ToArray();
        _xDomain = Expand(xValues.Length == 0 ? (0d, 1d) : (xValues.Min(), xValues.Max()));

        if (_valueAxes.Count > 0)
        {
            _valueDomain = Expand((_valueAxes.Values.Min(item => item.Minimum), _valueAxes.Values.Max(item => item.Maximum)));
        }
        else
        {
            var values = new List<double> { 0 };
            foreach (var series in DrawnSeries.Where(item => item.Kind is not OmniChartSeriesKind.StackedArea and not OmniChartSeriesKind.StackedColumn and not OmniChartSeriesKind.StackedBar))
            {
                values.AddRange(_range.Points(series.Data).Select(point => point.Y));
            }
            foreach (var kind in new[] { OmniChartSeriesKind.StackedArea, OmniChartSeriesKind.StackedColumn, OmniChartSeriesKind.StackedBar })
            {
                var stacked = DrawnSeries.Where(item => item.Kind == kind).ToArray();
                var maximumCount = stacked.Length == 0 ? 0 : stacked.Max(item => item.Data.Count);
                for (var index = 0; index < maximumCount; index++)
                {
                    if (!InRange(index))
                    {
                        continue;
                    }

                    var positive = stacked.Where(item => index < item.Data.Count).Select(item => item.Data[index].Y).Where(value => value > 0).Sum();
                    var negative = stacked.Where(item => index < item.Data.Count).Select(item => item.Data[index].Y).Where(value => value < 0).Sum();
                    values.Add(positive);
                    values.Add(negative);
                }
            }
            _valueDomain = _automaticValueAxes.Count > 0
                ? RoundOutward(values.Min(), values.Max(), _automaticValueAxes.Values.Max())
                : Expand((values.Min(), values.Max()));
        }

        DomainCalculationCount++;
        _valueLabelWidth = null;
        _domainsDirty = false;
    }

    private double StackBaseline(SeriesRegistration current, int index, double value)
    {
        var baseline = 0d;
        foreach (var series in DrawnSeries)
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

    // Bounds rounded outward to a step of 1, 2, 2.5 or 5 times a power of ten, the step chosen so that tickCount
    // graduations from the rounded minimum reach the maximum: every graduation lands on a round number.
    internal static (double Minimum, double Maximum) RoundOutward(double minimum, double maximum, int tickCount)
    {
        var ticks = Math.Max(1, tickCount);
        if (maximum <= minimum)
        {
            maximum = minimum + 1;
        }
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10((maximum - minimum) / ticks)));
        for (var attempt = 0; attempt < 4; attempt++, magnitude *= 10)
        {
            foreach (var multiplier in new[] { 1d, 2d, 2.5d, 5d })
            {
                var step = multiplier * magnitude;
                var low = Math.Floor(minimum / step) * step;
                if (low + (step * ticks) >= maximum - (step * 1e-9))
                {
                    return (low, low + (step * ticks));
                }
            }
        }
        return (minimum, maximum);
    }

    private static (double Minimum, double Maximum) Expand((double Minimum, double Maximum) domain) =>
        domain.Minimum.Equals(domain.Maximum)
            ? (domain.Minimum - 0.5, domain.Maximum + 0.5)
            : domain;

    /// <summary>
    /// Outside the drawing, as an HTML list, when asked (below or above), or, for
    /// <see cref="OmniLegendPosition.Auto"/>, below when the longest entry needs a wider column than
    /// the default one and than a fifth of the view box.
    /// </summary>
    private bool IsOutside(LegendRegistration legend) => legend.Position switch
    {
        OmniLegendPosition.Bottom or OmniLegendPosition.Top => true,
        OmniLegendPosition.Right => false,
        _ => ColumnNeeded(legend) > Math.Max(DefaultLegendColumn, 0.2 * ViewWidth)
    };

    private double ColumnNeeded(LegendRegistration legend) =>
        LegendTextOffset + LegendEntries(legend).Select(entry => (entry.Text?.Length ?? 0) * CharacterWidth).DefaultIfEmpty(0).Max() + 1;

    /// <summary>
    /// What one <see cref="OmniLegend"/> asks for, which the chart needs to draw it below the plot:
    /// its name, its own entries (empty to take them from the series) and its position.
    /// </summary>
    internal sealed record LegendRegistration(
        string Label,
        IReadOnlyList<string> Items,
        OmniLegendPosition Position,
        bool AllowToggle = true)
    {
        internal bool SameAs(LegendRegistration other) =>
            Label == other.Label
            && Position == other.Position
            && AllowToggle == other.AllowToggle
            && Items.SequenceEqual(other.Items);
    }

    /// <summary>
    /// One entry of a legend: its text, the palette slot of its swatch and, for an entry that names a
    /// series, the colour index of that series, which a click on the entry hides or shows (null for a
    /// pie slice or an entry past the last series).
    /// </summary>
    internal readonly record struct LegendEntry(string Text, int ColorSlot, int? SeriesColor);

    /// <summary>Whether a click on <paramref name="entry"/> of <paramref name="legend"/> hides or shows its series.</summary>
    internal static bool CanToggle(LegendRegistration legend, LegendEntry entry) => legend.AllowToggle && entry.SeriesColor is not null;

    /// <summary>Whether the series an entry names is hidden.</summary>
    internal bool IsEntryHidden(LegendEntry entry) => entry.SeriesColor is { } color && IsHidden(color);

    /// <summary>A drawn series as a legend and a data table see it.</summary>
    internal sealed record ChartSeriesView(string? Title, int ColorIndex, IReadOnlyList<OmniChartPoint> Data);

    /// <summary>A pie or donut series: its title and its slices, positive values only, in the order they are drawn.</summary>
    internal sealed record PieRegistration(object Owner, string? Title, IReadOnlyList<OmniChartSlice> Slices);

    private sealed record SeriesRegistration(object Owner, OmniChartSeriesKind Kind, IReadOnlyList<OmniChartPoint> Data, string? Title, int ColorIndex);
}
