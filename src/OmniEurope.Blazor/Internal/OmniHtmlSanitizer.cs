using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Internal;

internal static partial class OmniHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "sub", "sup", "blockquote",
        "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6", "code", "pre", "span"
    };

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var value = DangerousBlockRegex().Replace(html, string.Empty);
        value = CommentRegex().Replace(value, string.Empty);
        return TagRegex().Replace(value, SanitizeTag);
    }

    private static string SanitizeTag(Match match)
    {
        var closing = match.Groups[1].Success;
        var name = match.Groups[2].Value.ToLowerInvariant();
        if (!AllowedTags.Contains(name))
        {
            return string.Empty;
        }

        if (closing)
        {
            return name == "br" ? string.Empty : $"</{name}>";
        }

        if (name != "a")
        {
            return name == "br" ? "<br>" : $"<{name}>";
        }

        var hrefMatch = HrefRegex().Match(match.Groups[3].Value);
        var rawHref = hrefMatch.Success ? System.Net.WebUtility.HtmlDecode(hrefMatch.Groups[2].Value) : string.Empty;
        if (!hrefMatch.Success || !IsSafeHref(rawHref))
        {
            return "<a>";
        }

        var href = HtmlEncoder.Default.Encode(rawHref);
        return $"<a href=\"{href}\" rel=\"noopener noreferrer\">";
    }

    private static bool IsSafeHref(string value)
    {
        if (value.StartsWith('/') || value.StartsWith('#') || value.StartsWith("./") || value.StartsWith("../"))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "mailto";
    }

    [GeneratedRegex(@"<\s*(script|style|iframe|object|embed|svg|math)[^>]*>.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousBlockRegex();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"<\s*(/)?\s*([a-zA-Z0-9]+)([^>]*)>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex("href\\s*=\\s*([\\\"'])(.*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HrefRegex();
}
