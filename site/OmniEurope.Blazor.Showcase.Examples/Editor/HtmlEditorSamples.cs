namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The sample documents and character sets the HTML editor examples start from.</summary>
public static class HtmlEditorSamples
{
    /// <summary>The letter the main editor opens with: a title, a greeting, a justified paragraph, a list and a quote.</summary>
    public const string Letter =
        "<h2>Acknowledgement of receipt</h2><p>Dear Sir or Madam,</p>" +
        "<p class=\"omni-align-justify\">Your file has been received and will be reviewed shortly. <strong>No document is missing.</strong></p>" +
        "<ul><li>Signed form</li><li>Proof of address</li></ul>" +
        "<blockquote>Please keep this message.</blockquote>";

    /// <summary>The letters of French the main editor offers under its surface.</summary>
    public static IReadOnlyList<string> FrenchCharacters { get; } = ["é", "è", "ê", "à", "ç", "œ", "«", "»"];
}
