using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>The figures shared by the column and bar chart examples.</summary>
public static class ColumnChartData
{
    /// <summary>The name of the first series.</summary>
    public const string LastYearTitle = "2025";

    /// <summary>The name of the second series.</summary>
    public const string ThisYearTitle = "2026";

    /// <summary>The four quarters, the categories of the quarterly charts.</summary>
    public static IReadOnlyList<string> Quarters { get; } = ["Q1", "Q2", "Q3", "Q4"];

    /// <summary>The files filed each quarter of 2025.</summary>
    public static IReadOnlyList<OmniChartPoint> LastYear { get; } =
    [
        new(1, 42), new(2, 58), new(3, 39), new(4, 61)
    ];

    /// <summary>The files filed each quarter of 2026.</summary>
    public static IReadOnlyList<OmniChartPoint> ThisYear { get; } =
    [
        new(1, 55), new(2, 64), new(3, 47), new(4, 72)
    ];

    /// <summary>The countries, the categories of the horizontal charts.</summary>
    public static IReadOnlyList<string> Countries { get; } = ["Belgium", "France", "Luxembourg", "Netherlands"];

    /// <summary>The cases processed in each country.</summary>
    public static IReadOnlyList<OmniChartPoint> ByCountry { get; } =
    [
        new(1, 124), new(2, 187), new(3, 63), new(4, 98)
    ];

    /// <summary>The cases still pending in each country.</summary>
    public static IReadOnlyList<OmniChartPoint> ByCountryPending { get; } =
    [
        new(1, 31), new(2, 54), new(3, 12), new(4, 40)
    ];

    /// <summary>Writes a count with its thousands separators.</summary>
    /// <param name="value">The count.</param>
    /// <returns>The count as text.</returns>
    public static string FormatCount(double value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
