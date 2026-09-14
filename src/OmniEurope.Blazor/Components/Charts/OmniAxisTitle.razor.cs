namespace OmniEurope.Blazor.Components;

public partial class OmniAxisTitle
{
    private const double VerticalX = 3;

    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
    [Parameter] public bool Vertical { get; set; }

    // Below the category labels, or left of the value labels, turned to read upwards.
    private double X => Vertical ? VerticalX : (OmniChartContext.PlotLeft + PlotRight) / 2;
    private double Y => Vertical ? Middle : 98;
    private static double Middle => (OmniChartContext.PlotTop + OmniChartContext.PlotBottom) / 2;
    private double PlotRight => ChartContext?.PlotRight ?? 96;
    private string? Transform => Vertical ? $"rotate(-90 {N(VerticalX)} {N(Middle)})" : null;
    private static string N(double value) => OmniChartGeometry.Number(value);
}
