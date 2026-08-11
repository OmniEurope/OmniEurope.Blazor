namespace OmniEurope.Blazor.Components;

public partial class OmniTooltip
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public int? TabIndex { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string TooltipId => $"{Id ?? "omni-tooltip"}-content";
}
