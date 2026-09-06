namespace OmniEurope.Blazor.Components;

public partial class OmniCard
{
    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }
}
