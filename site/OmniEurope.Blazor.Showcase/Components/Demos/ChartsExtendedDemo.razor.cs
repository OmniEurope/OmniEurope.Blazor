using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChartsExtendedDemo
{
    private static readonly string[] Days =
        [.. Enumerable.Range(1, 30).Select(day => new DateOnly(2026, 9, day).ToString("dd/MM", CultureInfo.InvariantCulture))];

    private static readonly IReadOnlyList<OmniChartPoint> Visitors =
        [.. Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 60 + (day * 7 % 45)))];

    private static readonly IReadOnlyList<OmniChartPoint> PageViews =
        [.. Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 180 + (day * 37 % 170)))];

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

    private static readonly IReadOnlyList<OmniChartPoint> ByCountryPending =
    [
        new(1, 31), new(2, 54), new(3, 12), new(4, 40)
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    // January to June, abbreviated by the culture the page runs in.
    private string[] Months { get; } =
        [.. Enumerable.Range(1, 6).Select(month => new DateOnly(2026, month, 1).ToString("MMM", CultureInfo.CurrentCulture))];

    private string[] Countries { get; set; } = [];

    private IReadOnlyList<OmniChartSlice> Slices { get; set; } = [];

    protected override void OnInitialized()
    {
        Countries =
        [
            Text["DemoGridCountryBelgium"], Text["DemoGridCountryFrance"],
            Text["DemoGridCountryLuxembourg"], Text["DemoGridCountryNetherlands"]
        ];
        Slices =
        [
            new(Text["DemoChartsExtendedSliceCounter"], 46),
            new(Text["DemoChartsExtendedSliceMail"], 28),
            new(Text["DemoChartsExtendedSliceOnline"], 19),
            new(Text["DemoChartsExtendedSliceOther"], 7)
        ];
    }

    private static string FormatPercent(double value) =>
        (value / 100).ToString("P0", CultureInfo.CurrentCulture);
}
