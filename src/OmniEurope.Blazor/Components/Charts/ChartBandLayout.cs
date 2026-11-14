namespace OmniEurope.Blazor.Components;

/// <summary>
/// The shapes a chart draws inside its category bands: the outline of an area, a column or a bar, and
/// the place each series takes among those sharing a band (columns side by side, stacked series in one
/// place, each value starting where the previous one ended). It reads the registrations, the plot
/// rectangle and the value scale of the <see cref="OmniChartContext"/> it is built on.
/// </summary>
internal sealed class ChartBandLayout(OmniChartContext chart)
{
    /// <summary>Share of a category band the columns of that category occupy together.</summary>
    private const double BandFill = 0.8;

    /// <summary>
    /// The outline of an area series: its top edge left to right, then its baseline (zero, or the
    /// top of the series stacked below) right to left, as SVG points.
    /// </summary>
    internal string AreaPoints(object owner, bool stacked)
    {
        var series = chart.GetSeries(owner);
        if (series.Data.Count == 0)
        {
            return string.Empty;
        }

        var top = new List<string>(series.Data.Count);
        var baseline = new List<string>(series.Data.Count);
        for (var index = 0; index < series.Data.Count; index++)
        {
            if (!chart.InRange(index))
            {
                continue;
            }

            var point = series.Data[index];
            var start = stacked ? StackBaseline(series, index, point.Y) : 0;
            top.Add(chart.Project(point.X, start + point.Y));
            baseline.Add(chart.Project(point.X, start));
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
        var series = chart.GetSeries(owner);
        var point = series.Data[index];
        var (slot, slots) = SlotOf(series, OmniChartSeriesKind.Column, OmniChartSeriesKind.StackedColumn);
        var (start, width) = SlotSpan(chart.PlotLeft, chart.PlotRight, index, slot, slots);
        var from = stacked ? StackBaseline(series, index, point.Y) : 0;
        var first = chart.ValueToY(from);
        var second = chart.ValueToY(from + point.Y);
        return (start, Math.Min(first, second), width, Math.Abs(first - second));
    }

    /// <summary>
    /// A horizontal bar inside its category band. Bar series stand one above the other in the band,
    /// every stacked series sharing one place, each value starting where the previous one ended.
    /// </summary>
    internal (double X, double Y, double Width, double Height) BarRect(object owner, int index, bool stacked)
    {
        var series = chart.GetSeries(owner);
        var point = series.Data[index];
        var (slot, slots) = SlotOf(series, OmniChartSeriesKind.Bar, OmniChartSeriesKind.StackedBar);
        var (start, height) = SlotSpan(OmniChartContext.PlotTop, OmniChartContext.PlotBottom, index, slot, slots);
        var from = stacked ? StackBaseline(series, index, point.Y) : 0;
        var first = chart.ValueToX(from);
        var second = chart.ValueToX(from + point.Y);
        return (Math.Min(first, second), start, Math.Abs(first - second), height);
    }

    /// <summary>
    /// The place of a series among those sharing a band: every series of the separate kind takes its
    /// own place, and all the series of the shared kind (the stacked ones) take a single place, where
    /// the first of them appears.
    /// </summary>
    private (int Slot, int Slots) SlotOf(OmniChartContext.SeriesRegistration series, OmniChartSeriesKind separate, OmniChartSeriesKind shared)
    {
        var slot = 0;
        var slots = 0;
        var sharedSlot = -1;
        foreach (var item in chart.DrawnSeries)
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
        index -= chart.FirstCategory;
        var band = (to - from) / chart.CategoryCount;
        var group = band * BandFill;
        var size = group / slots;
        var start = from + (index * band) + ((band - group) / 2) + (slot * size);
        return (start + (size * 0.05), size * 0.9);
    }

    private double StackBaseline(OmniChartContext.SeriesRegistration current, int index, double value)
    {
        var baseline = 0d;
        foreach (var series in chart.DrawnSeries)
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
}
