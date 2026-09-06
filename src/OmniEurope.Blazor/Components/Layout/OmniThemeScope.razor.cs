namespace OmniEurope.Blazor.Components;

public partial class OmniThemeScope
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAppearance Appearance { get; set; }

    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;
}
