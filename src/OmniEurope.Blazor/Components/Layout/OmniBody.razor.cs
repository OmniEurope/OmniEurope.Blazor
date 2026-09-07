namespace OmniEurope.Blazor.Components;

public partial class OmniBody
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }
}
