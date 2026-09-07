namespace OmniEurope.Blazor.Components;

public partial class OmniHeading
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H2;

    [Parameter]
    public OmniTextTone Tone { get; set; }

    private string HeadingClass => Css(
        "omni-heading",
        $"omni-heading--{Level.ToString().ToLowerInvariant()}",
        $"omni-text--{Tone.ToString().ToLowerInvariant()}");
}
