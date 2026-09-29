namespace OmniEurope.Blazor.Components;

/// <summary>A heading of the level the document outline needs, drawn at the size of that level.</summary>
public partial class OmniHeading
{
    /// <summary>The text of the heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The heading level, <c>h1</c> to <c>h6</c>; <see cref="OmniHeadingLevel.H2"/> by default.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H2;

    /// <summary>The colour of the heading; <see cref="OmniTextTone.Neutral"/> by default.</summary>
    [Parameter]
    public OmniTextTone Tone { get; set; }

    private string HeadingClass => Css(
        "omni-heading",
        $"omni-heading--{Level.ToString().ToLowerInvariant()}",
        $"omni-text--{Tone.ToString().ToLowerInvariant()}");
}
