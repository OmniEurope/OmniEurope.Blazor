namespace OmniEurope.Blazor.Components;

public partial class OmniBadge
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniBadgeVariant Variant { get; set; }
}
