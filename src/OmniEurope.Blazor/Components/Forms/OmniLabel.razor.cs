namespace OmniEurope.Blazor.Components;

public partial class OmniLabel
{
    [Parameter, EditorRequired]
    public string For { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Required { get; set; }
}
