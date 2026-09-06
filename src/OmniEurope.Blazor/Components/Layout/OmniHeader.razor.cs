namespace OmniEurope.Blazor.Components;

public partial class OmniHeader
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Sticky { get; set; }
}
