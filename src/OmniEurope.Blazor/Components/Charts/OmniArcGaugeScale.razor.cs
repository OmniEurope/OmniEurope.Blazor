namespace OmniEurope.Blazor.Components;

public partial class OmniArcGaugeScale
{
    [Parameter] public double Minimum { get; set; }
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
