namespace OmniEurope.Blazor.Components;

public partial class OmniBarOptions
{
    [Parameter] public string Orientation { get; set; } = "horizontal";
    [Parameter] public bool Stacked { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string OptionsClass => Stacked ? "omni-chart__bar-options omni-chart__bar-options--stacked" : "omni-chart__bar-options";
}
