namespace OmniEurope.Blazor.Components;

public partial class OmniStack
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniStackOrientation Orientation { get; set; } = OmniStackOrientation.Vertical;

    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;
}
