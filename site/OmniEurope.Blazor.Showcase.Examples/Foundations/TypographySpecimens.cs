using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The names and sample sentences the typography examples share.</summary>
public static class TypographySpecimens
{
    /// <summary>The HTML tag a heading level renders, which is technical data, not interface text.</summary>
    public static string LevelTag(OmniHeadingLevel level) => $"h{(int)level}";

    /// <summary>The HTML tag a text element renders.</summary>
    public static string ElementTag(OmniTextElement element) => element switch
    {
        OmniTextElement.Paragraph => "p",
        OmniTextElement.Strong => "strong",
        OmniTextElement.Emphasis => "em",
        OmniTextElement.Small => "small",
        OmniTextElement.Title => "strong",
        OmniTextElement.Caption => "small",
        _ => "span"
    };

    /// <summary>A sentence that shows what a text element is for.</summary>
    public static string ElementSample(OmniTextElement element) => element switch
    {
        OmniTextElement.Paragraph => "Paragraph: a block of running text",
        OmniTextElement.Strong => "Strong: important text",
        OmniTextElement.Emphasis => "Emphasis: stressed text",
        OmniTextElement.Small => "Small: side comment, fine print",
        OmniTextElement.Title => "Title: a strong block above a list or a grid",
        OmniTextElement.Caption => "Caption: a small block under a title or a field",
        _ => "Span: inline text without any particular meaning"
    };

    /// <summary>The name of a tone as a person would say it.</summary>
    public static string ToneName(OmniTextTone tone) => tone switch
    {
        OmniTextTone.Muted => "Muted",
        OmniTextTone.Accent => "Accent",
        OmniTextTone.Success => "Success",
        OmniTextTone.Danger => "Danger",
        OmniTextTone.Info => "Info",
        OmniTextTone.Warning => "Warning",
        _ => "Default"
    };
}
