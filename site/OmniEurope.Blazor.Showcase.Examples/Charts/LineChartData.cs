using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>A series of a line chart: its name, its colour in the chart palette and its points.</summary>
/// <param name="Title">The name of the series, as the legend writes it.</param>
/// <param name="ColorIndex">The rank of its colour in the chart palette.</param>
/// <param name="Data">Its points.</param>
public sealed record LineChartSeries(string Title, int ColorIndex, IReadOnlyList<OmniChartPoint> Data);

/// <summary>The figures shared by the line and area chart examples.</summary>
public static class LineChartData
{
    /// <summary>The number of colours in the chart palette.</summary>
    public const int PaletteSize = 8;

    /// <summary>The six months of the monthly charts.</summary>
    public static IReadOnlyList<string> Months { get; } = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];

    /// <summary>The files filed each month.</summary>
    public static IReadOnlyList<OmniChartPoint> Deposits { get; } =
    [
        new(1, 42), new(2, 58), new(3, 39), new(4, 61), new(5, 55), new(6, 68)
    ];

    /// <summary>The decisions taken each month.</summary>
    public static IReadOnlyList<OmniChartPoint> Decisions { get; } =
    [
        new(1, 30), new(2, 44), new(3, 35), new(4, 48), new(5, 51), new(6, 57)
    ];

    /// <summary>The files waiting at the end of each month.</summary>
    public static IReadOnlyList<OmniChartPoint> Backlog { get; } =
    [
        new(1, 12), new(2, 26), new(3, 30), new(4, 43), new(5, 47), new(6, 58)
    ];

    /// <summary>
    /// The three series of the monthly charts, the first one drawn in the palette colour
    /// <paramref name="firstColor"/> and the others in the colours that follow it.
    /// </summary>
    /// <param name="firstColor">The palette rank of the first series.</param>
    /// <returns>The filings, the decisions and the backlog.</returns>
    public static IReadOnlyList<LineChartSeries> Monthly(int firstColor) =>
    [
        new("Filings", (firstColor + 0) % PaletteSize, Deposits),
        new("Decisions", (firstColor + 1) % PaletteSize, Decisions),
        new("Backlog", (firstColor + 2) % PaletteSize, Backlog)
    ];

    /// <summary>One line per colour of the palette, shifted so that they do not overlap.</summary>
    /// <returns>Eight series, one per palette rank.</returns>
    public static IReadOnlyList<LineChartSeries> Palette() =>
    [
        .. Enumerable.Range(0, PaletteSize)
            .Select(index => new LineChartSeries(
                $"Colour {index}",
                index,
                [.. Deposits.Select(point => new OmniChartPoint(point.X, (point.Y / 2) + (index * 6)))]))
    ];
}
