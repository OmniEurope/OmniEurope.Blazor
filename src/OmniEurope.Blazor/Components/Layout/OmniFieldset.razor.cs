namespace OmniEurope.Blazor.Components;

public partial class OmniFieldset
{
    [Parameter, EditorRequired]
    public RenderFragment? Legend { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Disabled { get; set; }
}
