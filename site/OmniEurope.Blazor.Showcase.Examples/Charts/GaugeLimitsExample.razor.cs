using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>Gauges at the ends of their scale, and past them.</summary>
public partial class GaugeLimitsExample
{
    private static readonly IReadOnlyList<LimitSample> Samples =
    [
        new("Empty (0)", 0, 0),
        new("Half (50)", 50, 1),
        new("Full (100)", 100, 2),
        new("Above the maximum (140)", 140, 3),
        new("Below the minimum (-30)", -30, 4)
    ];

    private static string FormatPercentOfHundred(double value) =>
        (value / 100).ToString("P0", CultureInfo.CurrentCulture);

    private sealed record LimitSample(string Label, double Value, int ColorIndex);
}
