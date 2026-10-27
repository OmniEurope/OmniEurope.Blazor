using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A heading and a paragraph driven by parameters: level, element, tone and truncation.</summary>
public partial class TypographyStageExample
{
    private const string LongSample =
        "The committee reviewed the files submitted during the quarter, approved most of them, and asked for further details on the three that still lacked a signed estimate.";

    /// <summary>The text of the heading; blank falls back to a sample, as an empty heading would render an empty tag.</summary>
    [Parameter]
    public string HeadingText { get; set; } = "Quarterly activity report";

    /// <summary>The level of the heading: the HTML tag and the size on the scale.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H2;

    /// <summary>The colour of the heading, never its weight.</summary>
    [Parameter]
    public OmniTextTone HeadingTone { get; set; } = OmniTextTone.Neutral;

    /// <summary>The element of the paragraph, and with it its meaning.</summary>
    [Parameter]
    public OmniTextElement Element { get; set; } = OmniTextElement.Paragraph;

    /// <summary>The colour of the paragraph.</summary>
    [Parameter]
    public OmniTextTone TextTone { get; set; } = OmniTextTone.Neutral;

    /// <summary>Whether the paragraph is cut to one line with an ellipsis.</summary>
    [Parameter]
    public bool Truncate { get; set; }

    private string DisplayedHeading => string.IsNullOrWhiteSpace(HeadingText) ? "Quarterly activity report" : HeadingText;

    private string RenderedAs => $"Rendered as <{TypographySpecimens.LevelTag(Level)}> then <{TypographySpecimens.ElementTag(Element)}>.";
}
