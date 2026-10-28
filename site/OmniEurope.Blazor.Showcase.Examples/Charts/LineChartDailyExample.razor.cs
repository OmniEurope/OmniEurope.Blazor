using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>Thirty days of columns and a dashed line, with one tooltip per day.</summary>
public partial class LineChartDailyExample
{
    private static readonly IReadOnlyList<string> Days =
        [.. Enumerable.Range(1, 30).Select(day => new DateOnly(2026, 9, day).ToString("dd/MM", CultureInfo.InvariantCulture))];

    private static readonly IReadOnlyList<OmniChartPoint> Visitors =
        [.. Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 60 + (day * 7 % 45)))];

    private static readonly IReadOnlyList<OmniChartPoint> PageViews =
        [.. Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 180 + (day * 37 % 170)))];
}
