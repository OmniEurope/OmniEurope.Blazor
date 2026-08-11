namespace OmniEurope.Blazor.Components;

public partial class OmniCategoryAxis
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    protected override void OnParametersSet() => ChartContext?.RegisterCategoryAxis(this, Labels);
    public void Dispose() { ChartContext?.UnregisterCategoryAxis(this); GC.SuppressFinalize(this); }
}
