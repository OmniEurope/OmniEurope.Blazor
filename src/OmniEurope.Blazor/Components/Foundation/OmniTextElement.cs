namespace OmniEurope.Blazor.Components;

/// <summary>The HTML element an <see cref="OmniText"/> renders.</summary>
public enum OmniTextElement
{
    /// <summary>A <c>span</c>, inline text without meaning of its own. The default.</summary>
    Span,

    /// <summary>A <c>p</c> paragraph.</summary>
    Paragraph,

    /// <summary>A <c>strong</c> element: text of strong importance.</summary>
    Strong,

    /// <summary>An <c>em</c> element: stressed emphasis.</summary>
    Emphasis,

    /// <summary>A <c>small</c> element: side comments and fine print.</summary>
    Small,

    /// <summary>
    /// A <c>strong</c> element drawn as a block: the title of a section of a card or of a list, above a grid.
    /// Its margins (<c>omni-u-mb-sm</c> and the other spacing utilities) apply without a display class.
    /// For a heading of the page outline, use <see cref="OmniHeading"/>.
    /// </summary>
    Title,

    /// <summary>A <c>small</c> element drawn as a block: a caption under a title or a field, its margins applied.</summary>
    Caption
}
