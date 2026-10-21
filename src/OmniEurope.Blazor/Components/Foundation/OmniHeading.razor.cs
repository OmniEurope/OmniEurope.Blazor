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

    /// <summary>
    /// A help text for the heading, behind a "?" after it, drawn and behaving as <see cref="OmniFormField.Help"/>:
    /// it shows on hover and on keyboard focus (the mark is in the Tab order), and is the mark's accessible
    /// description, never a part of the heading's name. The heading and the mark are then wrapped in a
    /// <c>div.omni-heading-row</c>, which carries the heading's bottom margin. Given an
    /// <see cref="OmniComponentBase.Id"/>, the text has the id <c>{Id}-help-content</c>. Null or blank, the
    /// default, renders no mark and leaves the markup unchanged.
    /// </summary>
    [Parameter]
    public string? Help { get; set; }

    private string? HelpId => string.IsNullOrWhiteSpace(Id) ? null : $"{Id}-help";

    private string RowClass => $"omni-heading-row omni-heading-row--{Level.ToString().ToLowerInvariant()}";

    private string HeadingClass => Css(
        "omni-heading",
        $"omni-heading--{Level.ToString().ToLowerInvariant()}",
        $"omni-text--{Tone.ToString().ToLowerInvariant()}");
}
