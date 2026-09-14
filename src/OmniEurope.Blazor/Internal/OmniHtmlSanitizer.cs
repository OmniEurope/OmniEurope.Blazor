using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace OmniEurope.Blazor.Internal;

internal static class OmniHtmlSanitizer
{
    private static readonly string[] AllowedTagNames =
    [
        "p", "br", "div", "strong", "b", "em", "i", "u", "s", "strike", "del", "sub", "sup", "blockquote",
        "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6", "code", "pre", "span", "hr",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption"
    ];

    /// <summary>
    /// The only classes a value may carry: alignment and text size, which the editor writes as
    /// classes because a style attribute would break the strict CSP. Any other class is dropped.
    /// </summary>
    internal static readonly string[] AllowedClassNames =
    [
        "omni-align-left", "omni-align-center", "omni-align-right", "omni-align-justify",
        "omni-font-size-small", "omni-font-size-normal", "omni-font-size-large", "omni-font-size-xlarge"
    ];

    /// <summary>
    /// Presentational containers that are dropped but whose content is kept, so a paste from a word
    /// processor or a web page keeps its text. Anything else that is not allowed goes with its
    /// content: a script, a style element, an embedded document or a form control is never unwrapped.
    /// </summary>
    private static readonly HashSet<string> TransparentTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "font", "section", "article", "header", "footer", "main", "aside", "nav", "address", "center",
        "label", "small", "big", "mark", "abbr", "cite", "dfn", "kbd", "samp", "var", "time", "ins",
        "figure", "figcaption", "dl", "dt", "dd", "q", "bdi", "bdo", "wbr", "o:p"
    };

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    internal static string Sanitize(string? html) =>
        string.IsNullOrEmpty(html) ? string.Empty : Sanitizer.Sanitize(html);

    /// <summary>
    /// What a paste inserts: the clipboard HTML through the allow-list when there is some, else the
    /// plain text as escaped paragraphs, one per blank-line separated block, with line breaks kept.
    /// </summary>
    internal static string SanitizePaste(string? html, string? text)
    {
        if (!string.IsNullOrWhiteSpace(html))
        {
            return Sanitize(html);
        }

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        if (blocks.Length == 1 && !blocks[0].Contains('\n', StringComparison.Ordinal))
        {
            return WebUtility.HtmlEncode(blocks[0]);
        }

        return string.Concat(blocks.Select(block =>
            "<p>" + string.Join("<br>", block.Trim('\n').Split('\n').Select(WebUtility.HtmlEncode)) + "</p>"));
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTagNames);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "rel", "class", "colspan", "rowspan"]);
        sanitizer.AllowedClasses.Clear();
        sanitizer.AllowedClasses.UnionWith(AllowedClassNames);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto", "tel"]);
        sanitizer.UriAttributes.Clear();
        sanitizer.UriAttributes.Add("href");
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        sanitizer.RemovingTag += static (_, eventArgs) =>
        {
            if (eventArgs.Reason == RemoveReason.NotAllowedTag && TransparentTagNames.Contains(eventArgs.Tag.LocalName))
            {
                KeepChildren(eventArgs.Tag);
            }
        };
        sanitizer.PostProcessNode += static (_, eventArgs) =>
        {
            if (eventArgs.Node is IHtmlAnchorElement anchor && anchor.HasAttribute("href"))
            {
                anchor.SetAttribute("rel", "noopener noreferrer");
            }

            if (eventArgs.Node is IElement element && element.HasAttribute("class") && string.IsNullOrWhiteSpace(element.GetAttribute("class")))
            {
                element.RemoveAttribute("class");
            }
        };
        return sanitizer;
    }

    /// <summary>
    /// Moves the children of a container about to be removed in front of it. They were collected
    /// with the rest of the document before the removal pass, so they are still sanitised themselves.
    /// </summary>
    private static void KeepChildren(IElement element)
    {
        var parent = element.Parent;
        if (parent is null)
        {
            return;
        }

        while (element.FirstChild is { } child)
        {
            parent.InsertBefore(child, element);
        }
    }
}
