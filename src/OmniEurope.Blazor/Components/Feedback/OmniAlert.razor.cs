namespace OmniEurope.Blazor.Components;

public partial class OmniAlert
{
    [Parameter]
    public string? Title { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAlertSeverity Severity { get; set; } = OmniAlertSeverity.Info;

    [Parameter]
    public bool Live { get; set; }
}
