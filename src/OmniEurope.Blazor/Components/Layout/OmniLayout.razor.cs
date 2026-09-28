namespace OmniEurope.Blazor.Components;

public partial class OmniLayout
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }
}
