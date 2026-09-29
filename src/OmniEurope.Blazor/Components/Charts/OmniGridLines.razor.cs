namespace OmniEurope.Blazor.Components;

/// <summary>Lines across the plot at even intervals of the value axis, the axis itself excepted.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniGridLines
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>Number of intervals; match <see cref="OmniValueAxis.TickCount"/> so each line runs through a graduation.</summary>
    [Parameter] public int Count { get; set; } = 5;

    /// <summary>The chart's layout, or one of its own when the lines are drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    protected override void OnParametersSet()
    {
        if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), "Count must be greater than zero.");
    }

    private static string N(double value) => OmniChartGeometry.Number(value);
}
