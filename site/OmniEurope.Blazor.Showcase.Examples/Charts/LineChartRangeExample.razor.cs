using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>Three years of monthly figures, of which a range navigator picks the months drawn.</summary>
public partial class LineChartRangeExample
{
    // Three years of monthly figures, with a seasonal swing and a slow rise.
    private static readonly IReadOnlyList<OmniChartPoint> LongDeposits =
        [.. Enumerable.Range(0, 36).Select(month => new OmniChartPoint(month, 40 + month + (month * 7 % 12 * 3)))];

    private static readonly IReadOnlyList<OmniChartPoint> LongDecisions =
        [.. Enumerable.Range(0, 36).Select(month => new OmniChartPoint(month, 30 + month + (month * 5 % 12 * 2)))];

    // January 2024 to December 2026.
    private static readonly IReadOnlyList<string> LongMonths =
        [.. Enumerable.Range(0, 36).Select(month => new DateOnly(2024, 1, 1).AddMonths(month).ToString("MMM yyyy", CultureInfo.InvariantCulture))];

    // The last twelve months at first; the end follows the last month until a handle moves it.
    private int? RangeStart { get; set; } = 24;

    private int? RangeEnd { get; set; }

    private string RangeShown =>
        $"Months shown: from {LongMonths[RangeStart ?? 0]} to {LongMonths[RangeEnd ?? (LongMonths.Count - 1)]}";
}
