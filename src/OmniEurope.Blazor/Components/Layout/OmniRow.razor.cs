namespace OmniEurope.Blazor.Components;

public partial class OmniRow
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    [Parameter]
    public bool Wrap { get; set; } = true;
}
