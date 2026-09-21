namespace OmniEurope.Blazor.Components;

public partial class OmniAxisTitle
{

    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
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
