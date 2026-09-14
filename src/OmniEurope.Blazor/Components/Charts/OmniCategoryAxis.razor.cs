namespace OmniEurope.Blazor.Components;

public partial class OmniCategoryAxis
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();

    /// <summary>The chart's layout, or one of its own when the axis is drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    protected override void OnParametersSet() => Context.RegisterCategoryAxis(this, Labels);
    private static string N(double value) => OmniChartGeometry.Number(value);
    public void Dispose() { ChartContext?.UnregisterCategoryAxis(this); GC.SuppressFinalize(this); }
}
