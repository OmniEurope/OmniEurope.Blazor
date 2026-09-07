using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace OmniEurope.Blazor.Internal;

internal static class OmniHtmlSanitizer
{
    private static readonly string[] AllowedTagNames =
    [
        "p", "br", "strong", "b", "em", "i", "sub", "sup", "blockquote",
        "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6", "code", "pre", "span"
    ];

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    internal static string Sanitize(string? html) =>
        string.IsNullOrEmpty(html) ? string.Empty : Sanitizer.Sanitize(html);

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTagNames);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "rel"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto", "tel"]);
        sanitizer.UriAttributes.Clear();
        sanitizer.UriAttributes.Add("href");
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        sanitizer.PostProcessNode += static (_, eventArgs) =>
        {
            if (eventArgs.Node is IHtmlAnchorElement anchor && anchor.HasAttribute("href"))
            {
                anchor.SetAttribute("rel", "noopener noreferrer");
            }
        };
        return sanitizer;
    }
}
