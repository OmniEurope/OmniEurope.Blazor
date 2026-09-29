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
    Small
}
