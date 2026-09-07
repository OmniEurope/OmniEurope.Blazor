namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChartDemo
{
    private static readonly string[] Quarters = ["T1", "T2", "T3", "T4"];

    private static readonly IReadOnlyList<OmniChartPoint> LastYear =
    [
        new(1, 42, "T1"),
        new(2, 58, "T2"),
        new(3, 39, "T3"),
        new(4, 61, "T4")
    ];

    private static readonly IReadOnlyList<OmniChartPoint> ThisYear =
    [
        new(1, 55, "T1"),
        new(2, 64, "T2"),
        new(3, 47, "T3"),
        new(4, 72, "T4")
    ];
}
