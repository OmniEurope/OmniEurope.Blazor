namespace OmniEurope.Blazor.Components;

/// <summary>
/// The axis of categories, with their labels: along the bottom of a vertical chart, down the left of a
/// horizontal one. Label <c>i</c> names the points of index <c>i</c> of every series.
/// </summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniCategoryAxis
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The category names, in the order of the points. The data table of the chart lists them all.</summary>
    [Parameter] public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();

    /// <summary>The chart's layout, or one of its own when the axis is drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    /// <summary>Registers the labels with the chart, or with the axis's own layout outside a chart.</summary>
    protected override void OnParametersSet() => Context.RegisterCategoryAxis(this, Labels);

    private static string N(double value) => OmniChartGeometry.Number(value);

    /// <summary>Removes the axis from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterCategoryAxis(this);
        GC.SuppressFinalize(this);
    }
}
