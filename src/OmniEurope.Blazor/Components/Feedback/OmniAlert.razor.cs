namespace OmniEurope.Blazor.Components;

public partial class OmniAlert
{
    [Parameter]
    public string? Title { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAlertSeverity Severity { get; set; } = OmniAlertSeverity.Info;

    /// <summary>
    /// Outline keeps the message light on the page; Filled paints the severity colour behind it,
    /// which is what a blocking message needs to be read as one.
    /// </summary>
    [Parameter]
    public OmniAlertVariant Variant { get; set; } = OmniAlertVariant.Outline;

    /// <summary>
    /// Rendered before the title. A slot rather than a name, so the consumer keeps its own icon set.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public bool Live { get; set; }
}
