namespace OmniEurope.Blazor.Components;

/// <summary>The title of an axis: centred below the category labels, or turned upwards left of the value labels.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniAxisTitle
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The title.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    /// <summary>Writes the title upwards along the left edge, for a vertical value axis. Off by default: below the plot.</summary>
    [Parameter] public bool Vertical { get; set; }

    // Below the category labels, or left of the value labels, turned to read upwards.
    // 3 units in from the left edge of the view box, which moves out when the chart is wide.
    private double VerticalX => (ChartContext?.ViewLeft ?? 0) + 3;
    private double X => Vertical ? VerticalX : ((ChartContext?.PlotLeft ?? 14) + PlotRight) / 2;
    private double Y => Vertical ? Middle : 98;
    private static double Middle => (OmniChartContext.PlotTop + OmniChartContext.PlotBottom) / 2;
    private double PlotRight => ChartContext?.PlotRight ?? 96;
    private string? Transform => Vertical ? $"rotate(-90 {N(VerticalX)} {N(Middle)})" : null;
    private static string N(double value) => OmniChartGeometry.Number(value);
}
