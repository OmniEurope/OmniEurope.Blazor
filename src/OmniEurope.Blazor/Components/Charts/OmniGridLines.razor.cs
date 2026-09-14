namespace OmniEurope.Blazor.Components;

public partial class OmniGridLines
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public int Count { get; set; } = 5;

    /// <summary>The chart's layout, or one of its own when the lines are drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    protected override void OnParametersSet()
    {
        if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), "Count must be greater than zero.");
    }

    private static string N(double value) => OmniChartGeometry.Number(value);
}
