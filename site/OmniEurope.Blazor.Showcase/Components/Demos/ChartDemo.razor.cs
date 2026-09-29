using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChartDemo
{
    private static readonly double[] LastYearValues = [42, 58, 39, 61];

    private static readonly double[] ThisYearValues = [55, 64, 47, 72];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string[] Quarters { get; set; } = [];

    private IReadOnlyList<OmniChartPoint> LastYear { get; set; } = [];

    private IReadOnlyList<OmniChartPoint> ThisYear { get; set; } = [];

    protected override void OnInitialized()
    {
        // The quarter abbreviation of the culture: "T1" in French, "Q1" in English.
        Quarters = [.. Enumerable.Range(1, 4).Select(quarter => Text["DemoChartQuarter", quarter].Value)];
        LastYear = [.. LastYearValues.Select((value, index) => new OmniChartPoint(index + 1, value, Quarters[index]))];
        ThisYear = [.. ThisYearValues.Select((value, index) => new OmniChartPoint(index + 1, value, Quarters[index]))];
    }
}
