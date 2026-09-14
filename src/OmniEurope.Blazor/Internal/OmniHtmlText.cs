using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Reads sanitised editor HTML as a document: its plain text, its word and character counts, and a
/// standalone HTML file of it.
/// </summary>
internal static partial class OmniHtmlText
{
    private static readonly HtmlParser Parser = new();

    private static readonly HashSet<string> SeparatedBlocks = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "ul", "ol", "table"
    };

    /// <summary>
    /// What the value looks like as plain text: blocks separated by a blank line, list items one per
    /// line with a dash or their number, table cells separated by tabs.
    /// </summary>
    internal static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendChildren(Body(html), builder, preformatted: false);
        return ExtraBlankLines().Replace(builder.ToString().Replace("\r", string.Empty, StringComparison.Ordinal), "\n\n")
            .Trim('\n', ' ', '\t');
    }

    /// <summary>Words (runs of characters holding a letter or a digit) and characters, spaces included, line breaks excluded.</summary>
    internal static (int Words, int Characters) Count(string? html)
    {
        var text = ToPlainText(html);
        var words = WordSeparators().Split(text).Count(token => token.Any(char.IsLetterOrDigit));
        var characters = text.Count(character => character is not '\n' and not '\r');
        return (words, characters);
    }

    /// <summary>
    /// A complete HTML file of the value. The alignment and size classes, which only mean something
    /// next to the package stylesheet, become the equivalent declarations on the elements, so the
    /// file reads the same in any browser or word processor. The file is handed to the user, never
    /// rendered in the page, so the strict CSP of the page does not apply to it.
    /// </summary>
    internal static string ToStandaloneDocument(string? html, string title, string language)
    {
        var body = Body(html);
        foreach (var element in body.QuerySelectorAll("[class]").ToArray())
        {
            var declarations = element.ClassList.Select(Declaration).Where(value => value is not null).ToArray();
            element.RemoveAttribute("class");
            if (declarations.Length > 0)
            {
                element.SetAttribute("style", string.Join("; ", declarations));
            }
        }

        return $"""
            <!DOCTYPE html>
            <html lang="{WebUtility.HtmlEncode(language)}">
            <head>
            <meta charset="utf-8">
            <title>{WebUtility.HtmlEncode(title)}</title>
            </head>
            <body>
            {body.InnerHtml}
            </body>
            </html>

            """;
    }

    /// <summary>The text of the first level-one heading, if the document has one.</summary>
    internal static string? FirstHeading(string? html)
    {
        var heading = string.IsNullOrWhiteSpace(html) ? null : Body(html).QuerySelector("h1")?.TextContent.Trim();
        return string.IsNullOrEmpty(heading) ? null : heading;
    }

    private static IElement Body(string? html) =>
        Parser.ParseDocument("<!DOCTYPE html><html><body>" + OmniHtmlSanitizer.Sanitize(html) + "</body></html>").Body!;

    private static string? Declaration(string className) => className switch
    {
        "omni-align-center" => "text-align: center",
        "omni-align-right" => "text-align: right",
        "omni-align-justify" => "text-align: justify",
        "omni-font-size-small" => "font-size: 0.85em",
        "omni-font-size-normal" => "font-size: 1rem",
        "omni-font-size-large" => "font-size: 1.3em",
        "omni-font-size-xlarge" => "font-size: 1.7em",
        _ => null
    };

    private static void AppendChildren(INode parent, StringBuilder builder, bool preformatted)
    {
        var ordinal = 0;
        foreach (var child in parent.ChildNodes)
        {
            if (child is IText text)
            {
                builder.Append(preformatted ? text.Data : Whitespace().Replace(text.Data, " "));
                continue;
            }

            if (child is not IElement element)
            {
                continue;
            }

            var name = element.LocalName;
            switch (name)
            {
                case "br":
                    builder.Append('\n');
                    break;
                case "hr":
                    builder.Append("\n\n----\n\n");
                    break;
                case "li":
                    ordinal++;
                    builder.Append('\n');
                    builder.Append(string.Equals(parent is IElement list ? list.LocalName : null, "ol", StringComparison.Ordinal)
                        ? $"{ordinal.ToString(CultureInfo.InvariantCulture)}. "
                        : "- ");
                    AppendChildren(element, builder, preformatted);
                    break;
                case "tr":
                    builder.Append('\n');
                    var cells = element.Children.Where(cell => cell.LocalName is "td" or "th").ToArray();
                    for (var index = 0; index < cells.Length; index++)
                    {
                        if (index > 0)
                        {
                            builder.Append('\t');
                        }

                        var cell = new StringBuilder();
                        AppendChildren(cells[index], cell, preformatted);
                        builder.Append(Whitespace().Replace(cell.ToString(), " ").Trim());
                    }

                    break;
                default:
                    var separated = SeparatedBlocks.Contains(name);
                    if (separated)
                    {
                        builder.Append("\n\n");
                    }

                    AppendChildren(element, builder, preformatted || name == "pre");
                    if (separated)
                    {
                        builder.Append("\n\n");
                    }

                    break;
            }
        }
    }

    [GeneratedRegex("\\n{3,}")]
    private static partial Regex ExtraBlankLines();

    [GeneratedRegex("\\s+")]
    private static partial Regex WordSeparators();

    [GeneratedRegex("[ \\t\\r\\n\\f]+")]
    private static partial Regex Whitespace();
}
