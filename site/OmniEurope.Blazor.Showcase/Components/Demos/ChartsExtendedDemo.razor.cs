using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChartsExtendedDemo
{
    private static readonly string[] Months = ["Jan", "Fév", "Mar", "Avr", "Mai", "Jui"];

    private static readonly string[] Countries = ["Belgique", "France", "Luxembourg", "Pays-Bas"];

    private static readonly string[] LineLegend = ["Dépôts", "Décisions"];

    private static readonly IReadOnlyList<OmniChartPoint> Deposits =
    [
        new(1, 42), new(2, 58), new(3, 39), new(4, 61), new(5, 55), new(6, 68)
    ];

    private static readonly IReadOnlyList<OmniChartPoint> Decisions =
    [
        new(1, 30), new(2, 44), new(3, 35), new(4, 48), new(5, 51), new(6, 57)
    ];

    private static readonly IReadOnlyList<OmniChartPoint> Backlog =
    [
        new(1, 12), new(2, 26), new(3, 30), new(4, 43), new(5, 47), new(6, 58)
    ];

    private static readonly IReadOnlyList<OmniChartPoint> ByCountry =
    [
        new(1, 124), new(2, 187), new(3, 63), new(4, 98)
    ];

    private static readonly IReadOnlyList<OmniChartSlice> Slices =
    [
        new("Guichet", 46),
        new("Courrier", 28),
        new("En ligne", 19),
        new("Autre", 7)
    ];

    private static string FormatPercent(double value) =>
        (value / 100).ToString("P0", CultureInfo.CurrentCulture);
}
