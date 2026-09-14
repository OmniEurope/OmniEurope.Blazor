using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>A labelled stretch of the time header, in pixels from the chart's left edge.</summary>
internal sealed record GanttHeaderCell(double X, double Width, string Label);

/// <summary>One row of the chart: a group heading or a task.</summary>
internal sealed record GanttRow(double Top, string Label, OmniGanttTask? Task, GanttBar? Bar, bool IsGroup);

/// <summary>Where a task's bar, or a group's summary bar, is drawn.</summary>
internal sealed record GanttBar(double X, double Y, double Width, double Height, double ProgressWidth, bool LabelInside);

/// <summary>
/// Everything an <see cref="OmniGantt"/> draws, in pixels: the rows, the two header tiers, the
/// week-end columns, the today line and the dependency arrows. Pure, so every placement rule is tested
/// without a render. Pixels rather than percentages because an arrow's path cannot mix the two, and
/// because a zoom is a width per day: the chart grows and its container scrolls.
/// </summary>
internal sealed class GanttLayout
{
    internal const double RowHeight = 36;
    internal const double HeaderHeight = 44;
    internal const double TierHeight = HeaderHeight / 2;
    internal const double BarHeight = 20;
    internal const double SummaryHeight = 8;

    /// <summary>Approximate width of one character of a 12px label, to tell whether it fits in its bar.</summary>
    private const double CharacterWidth = 7;

    private const double LabelPadding = 6;

    /// <summary>Width of one day in pixels, per zoom.</summary>
    internal static double DayWidth(OmniGanttScale scale) => scale switch
    {
        OmniGanttScale.Day => 32,
        OmniGanttScale.Week => 14,
        _ => 4
    };

    private GanttLayout(OmniGanttScale scale, DateOnly start, DateOnly end)
    {
        Scale = scale;
        RangeStart = start;
        RangeEnd = end;
        PixelsPerDay = DayWidth(scale);
    }

    internal OmniGanttScale Scale { get; }

    /// <summary>First day drawn.</summary>
    internal DateOnly RangeStart { get; }

    /// <summary>Day after the last day drawn.</summary>
    internal DateOnly RangeEnd { get; }

    internal double PixelsPerDay { get; }

    internal double Width => (RangeEnd.DayNumber - RangeStart.DayNumber) * PixelsPerDay;

    internal double Height => HeaderHeight + (Rows.Count * RowHeight);

    internal List<GanttRow> Rows { get; } = [];

    internal List<GanttHeaderCell> TopTier { get; } = [];

    internal List<GanttHeaderCell> BottomTier { get; } = [];

    /// <summary>Left edges of the Saturday and Sunday columns, shaded at the day zoom only.</summary>
    internal List<double> WeekEnds { get; } = [];

    /// <summary>The middle of today's column, or null when today falls outside the chart.</summary>
    internal double? TodayX { get; private set; }

    internal List<string> Dependencies { get; } = [];

    /// <summary>Distance of a day's left edge from the chart's left edge.</summary>
    internal double X(DateOnly day) => (day.DayNumber - RangeStart.DayNumber) * PixelsPerDay;

    internal static GanttLayout Build(IReadOnlyList<OmniGanttTask> tasks, OmniGanttScale scale, bool grouped, DateOnly today, CultureInfo culture)
    {
        var (first, last) = tasks.Count == 0
            ? (today, today)
            : (tasks.Min(task => Earliest(task)), tasks.Max(task => Latest(task)));
        var (start, end) = Range(first, last, scale);
        var layout = new GanttLayout(scale, start, end);
        layout.PlaceRows(tasks, grouped);
        layout.PlaceHeader(culture);
        layout.PlaceDependencies();
        if (today >= start && today < end)
        {
            layout.TodayX = layout.X(today) + (layout.PixelsPerDay / 2);
        }

        return layout;
    }

    /// <summary>
    /// The days drawn: the tasks' span with a margin, and whole weeks or whole months at those zooms
    /// so the header never starts on a cut column.
    /// </summary>
    internal static (DateOnly Start, DateOnly End) Range(DateOnly first, DateOnly last, OmniGanttScale scale)
    {
        switch (scale)
        {
            case OmniGanttScale.Day:
                return (first.AddDays(-2), last.AddDays(3));
            case OmniGanttScale.Week:
                var monday = first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
                var nextMonday = last.AddDays(7 - (((int)last.DayOfWeek + 6) % 7));
                return (monday.AddDays(-7), nextMonday.AddDays(7));
            default:
                var month = new DateOnly(first.Year, first.Month, 1);
                var after = new DateOnly(last.Year, last.Month, 1).AddMonths(1);
                return (month, after);
        }
    }

    private static DateOnly Earliest(OmniGanttTask task) => task.End < task.Start ? task.End : task.Start;

    private static DateOnly Latest(OmniGanttTask task) => task.End < task.Start ? task.Start : task.End;

    private void PlaceRows(IReadOnlyList<OmniGanttTask> tasks, bool grouped)
    {
        if (!grouped || tasks.All(task => task.Group is null))
        {
            foreach (var task in tasks)
            {
                AddTask(task);
            }

            return;
        }

        foreach (var task in tasks.Where(task => task.Group is null))
        {
            AddTask(task);
        }

        foreach (var group in tasks.Where(task => task.Group is not null).GroupBy(task => task.Group!, StringComparer.Ordinal))
        {
            var members = group.ToArray();
            var x = X(members.Min(Earliest));
            var width = X(members.Max(Latest).AddDays(1)) - x;
            var top = HeaderHeight + (Rows.Count * RowHeight);
            var bar = new GanttBar(x, top + ((RowHeight - SummaryHeight) / 2), width, SummaryHeight, 0, false);
            Rows.Add(new GanttRow(top, group.Key, null, bar, true));
            foreach (var task in members)
            {
                AddTask(task);
            }
        }
    }

    private void AddTask(OmniGanttTask task)
    {
        var top = HeaderHeight + (Rows.Count * RowHeight);
        var x = X(Earliest(task));
        var width = X(Latest(task).AddDays(1)) - x;
        var progress = double.IsFinite(task.Progress) ? Math.Clamp(task.Progress, 0, 1) : 0;
        var fits = (task.Title.Length * CharacterWidth) + (LabelPadding * 2) <= width;
        Rows.Add(new GanttRow(top, task.Title, task, new GanttBar(x, top + ((RowHeight - BarHeight) / 2), width, BarHeight, width * progress, fits), false));
    }

    private void PlaceHeader(CultureInfo culture)
    {
        if (Scale == OmniGanttScale.Month)
        {
            AddSpans(TopTier, day => new DateOnly(day.Year, 1, 1), day => day.AddYears(1), day => day.Year.ToString(culture));
            AddSpans(BottomTier, day => new DateOnly(day.Year, day.Month, 1), day => day.AddMonths(1), day => day.ToString("MMM", culture));
            return;
        }

        AddSpans(TopTier, day => new DateOnly(day.Year, day.Month, 1), day => day.AddMonths(1), day => day.ToString("MMMM yyyy", culture));
        if (Scale == OmniGanttScale.Week)
        {
            AddSpans(
                BottomTier,
                day => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
                day => day.AddDays(7),
                day => "S" + ISOWeek.GetWeekOfYear(day.ToDateTime(TimeOnly.MinValue)).ToString(culture));
            return;
        }

        for (var day = RangeStart; day < RangeEnd; day = day.AddDays(1))
        {
            BottomTier.Add(new GanttHeaderCell(X(day), PixelsPerDay, day.Day.ToString(culture)));
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                WeekEnds.Add(X(day));
            }
        }
    }

    /// <summary>Cuts the range into periods, the first and last clipped to the chart's edges.</summary>
    private void AddSpans(List<GanttHeaderCell> tier, Func<DateOnly, DateOnly> periodStart, Func<DateOnly, DateOnly> next, Func<DateOnly, string> label)
    {
        for (var period = periodStart(RangeStart); period < RangeEnd; period = next(period))
        {
            var from = period < RangeStart ? RangeStart : period;
            var to = next(period) > RangeEnd ? RangeEnd : next(period);
            tier.Add(new GanttHeaderCell(X(from), X(to) - X(from), label(period)));
        }
    }

    /// <summary>
    /// A finish-to-start arrow from the end of each prerequisite to the start of the task that waits
    /// for it: right, down to the task's row, right again; when the task starts too soon after, the
    /// arrow steps back along the gap between the two rows before coming in from the left.
    /// </summary>
    private void PlaceDependencies()
    {
        var rows = Rows.Where(row => row.Task is not null).ToDictionary(row => row.Task!.Id, StringComparer.Ordinal);
        foreach (var row in Rows.Where(row => row.Task is not null))
        {
            foreach (var id in row.Task!.DependsOn.Distinct(StringComparer.Ordinal))
            {
                if (!rows.TryGetValue(id, out var before) || ReferenceEquals(before, row))
                {
                    continue;
                }

                var x1 = before.Bar!.X + before.Bar.Width;
                var y1 = before.Top + (RowHeight / 2);
                var x2 = row.Bar!.X;
                var y2 = row.Top + (RowHeight / 2);
                var bend = x1 + 8;
                Dependencies.Add(x2 - x1 >= 16
                    ? $"M {N(x1)} {N(y1)} H {N(bend)} V {N(y2)} H {N(x2)}"
                    : $"M {N(x1)} {N(y1)} H {N(bend)} V {N(y2 > y1 ? row.Top : row.Top + RowHeight)} H {N(x2 - 8)} V {N(y2)} H {N(x2)}");
            }
        }
    }

    internal static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
