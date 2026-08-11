namespace OmniEurope.Blazor.Components;

public partial class OmniMain
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool FocusTarget { get; set; } = true;

    [Parameter]
    public string? AriaLabelledBy { get; set; }
}
