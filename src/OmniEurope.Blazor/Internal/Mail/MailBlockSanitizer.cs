using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The allow-list of the HTML fragments a host gives as block variables (<see cref="Components.OmniMailRenderOptions.Blocks"/>):
/// the text, list and table elements a mail client draws, links to http, https and mailto, and inline
/// <c>style</c> declarations of a few presentation properties. Everything that runs code, loads a resource or
/// reaches outside the mail is removed: scripts, style sheets and <c>link</c> elements, embedded documents,
/// forms, images, event handlers (<c>on*</c>), classes and identifiers, and any declaration holding a
/// <c>url(</c>, an <c>expression(</c>, an <c>@import</c> or a backslash escape.
/// </summary>
internal static partial class MailBlockSanitizer
{
    private static readonly string[] AllowedTagNames =
    [
        "p", "br", "div", "span", "strong", "b", "em", "i", "u", "s", "del", "sub", "sup", "small", "blockquote",
        "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6", "code", "pre", "hr",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "mark"
    ];

    private static readonly string[] AllowedAttributeNames = ["href", "rel", "colspan", "rowspan", "style", "title"];

    private static readonly string[] AllowedCssProperties =
    [
        "color", "background-color", "font-weight", "font-style", "font-size", "text-align", "text-decoration",
        "vertical-align", "white-space", "width", "padding", "padding-top", "padding-right", "padding-bottom",
        "padding-left", "border", "border-top", "border-right", "border-bottom", "border-left", "border-color",
        "border-width", "border-style"
    ];

    private static readonly string[] RefusedInStyle = ["url(", "expression(", "@import", "\\", "javascript:"];

    private static readonly HtmlSanitizer Sanitizer = Create();

    /// <summary>The fragment through the allow-list; empty for null or empty.</summary>
    internal static string Sanitize(string? html) => string.IsNullOrEmpty(html) ? string.Empty : Sanitizer.Sanitize(html);

    private static HtmlSanitizer Create()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTagNames);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(AllowedAttributeNames);
        sanitizer.AllowedClasses.Clear();
        sanitizer.AllowDataAttributes = false;
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        sanitizer.UriAttributes.Clear();
        sanitizer.UriAttributes.UnionWith(["href"]);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedCssProperties.UnionWith(AllowedCssProperties);
        sanitizer.AllowedAtRules.Clear();
        sanitizer.PostProcessNode += static (_, eventArgs) =>
        {
            if (eventArgs.Node is IHtmlAnchorElement anchor && anchor.HasAttribute("href"))
            {
                anchor.SetAttribute("rel", "noopener noreferrer");
            }

            if (eventArgs.Node is not IElement element || element.GetAttribute("style") is not { } style)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(style) || RefusedInStyle.Any(refused => style.Contains(refused, StringComparison.OrdinalIgnoreCase)))
            {
                element.RemoveAttribute("style");
                return;
            }

            // The CSS parser writes every colour as rgba(), which Outlook's Word engine ignores: an opaque one goes back to hexadecimal.
            element.SetAttribute("style", OpaqueColor().Replace(style, match => "#" + string.Concat(
                new[] { "r", "g", "b" }.Select(channel =>
                    Math.Min(255, int.Parse(match.Groups[channel].Value, CultureInfo.InvariantCulture)).ToString("x2", CultureInfo.InvariantCulture)))));
        };
        return sanitizer;
    }

    [GeneratedRegex(@"rgba?\(\s*(?<r>[0-9]{1,3}),\s*(?<g>[0-9]{1,3}),\s*(?<b>[0-9]{1,3})(?:,\s*1(?:\.0+)?)?\s*\)")]
    private static partial Regex OpaqueColor();
}
