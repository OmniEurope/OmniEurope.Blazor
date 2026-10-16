using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns a sanitised mail template into the HTML a mail client renders: the values written in place of
/// their tokens (as text, or as a checked address in a link), every element's look written in its own
/// <c>style</c> attribute (mail clients drop or ignore style sheets and classes), and the body laid out in
/// the nested presentation tables that Outlook's Word engine and the web clients all draw the same way.
/// </summary>
internal static class MailDocument
{
    private const string Monospace = "Consolas, 'Courier New', monospace";
    private static readonly HtmlParser Parser = new();
    private static readonly string[] SafeSchemes = ["http", "https", "mailto"];

    /// <summary>The attribute that carries each element's look: <c>style</c> in a mail.</summary>
    internal const string MailLook = "style";

    /// <summary>
    /// The attribute that carries each element's look in the preview of <see cref="OmniMailTemplateEditor"/>: the page's
    /// strict CSP refuses a <c>style</c> attribute even in a document parsed apart, so the preview script reads
    /// this one and gives its declarations through the CSSOM.
    /// </summary>
    internal const string PreviewLook = "data-omni-mail-look";

    internal static OmniMailRenderResult Render(string? template, IReadOnlyDictionary<string, string?> values, OmniMailRenderOptions options, string lookAttribute = MailLook)
    {
        var theme = Checked(options.Theme ?? OmniMailTheme.Default);
        if (options.Width is < 320 or > 1200)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Width, "The mail width must be between 320 and 1200 pixels.");
        }

        var blockValues = options.Blocks ?? new Dictionary<string, string?>();
        var shared = blockValues.Keys.FirstOrDefault(values.ContainsKey);
        if (shared is not null)
        {
            throw new ArgumentException($"The mail variable '{shared}' is given both as a text value and as a block.", nameof(options));
        }

        var text = new MailTemplateText(values);
        var blocks = new MailBlocks(blockValues, text);
        var subject = options.Subject is null ? string.Empty : MailTemplateText.OneLine(text.Replace(options.Subject));
        var preheader = options.Preheader is null ? null : MailTemplateText.OneLine(text.Replace(options.Preheader));
        var body = Parser.ParseDocument("<!DOCTYPE html><html><body>" + OmniHtmlSanitizer.Sanitize(template) + "</body></html>").Body!;
        blocks.Mark(body);
        Substitute(body, text);
        blocks.Build(body, theme, lookAttribute);
        var plainText = OmniHtmlText.ToPlainText(PlainTextSource(body));
        Inline(body, theme, lookAttribute);
        var language = options.Language ?? CultureInfo.CurrentUICulture.Name;
        return new OmniMailRenderResult(
            MailLayout.Build(body.InnerHtml, subject, preheader, string.IsNullOrEmpty(language) ? "en" : language, options.Width, theme, lookAttribute),
            plainText,
            subject,
            InOrder(text.Missing, $"{options.Subject}\n{options.Preheader}\n{template}"));
    }

    /// <summary>The missing names in the order they first appear in the subject, the preheader, then the body.</summary>
    private static string[] InOrder(IReadOnlyList<string> missing, string written)
    {
        var order = MailBlocks.Names(written).ToList();
        return [.. missing.OrderBy(name => order.IndexOf(name) is var index and >= 0 ? index : int.MaxValue)];
    }

    /// <summary>A theme set by hand is held to literal values, like one read from tokens.</summary>
    private static OmniMailTheme Checked(OmniMailTheme theme)
    {
        _ = MailCss.Color(theme.PageBackground, nameof(theme.PageBackground));
        _ = MailCss.Color(theme.Surface, nameof(theme.Surface));
        _ = MailCss.Color(theme.Text, nameof(theme.Text));
        _ = MailCss.Color(theme.MutedText, nameof(theme.MutedText));
        _ = MailCss.Color(theme.Accent, nameof(theme.Accent));
        _ = MailCss.Color(theme.OnAccent, nameof(theme.OnAccent));
        _ = MailCss.Color(theme.Link, nameof(theme.Link));
        _ = MailCss.Color(theme.Border, nameof(theme.Border));
        _ = MailCss.Color(theme.Highlight, nameof(theme.Highlight));
        _ = MailCss.Color(theme.CodeBackground, nameof(theme.CodeBackground));
        _ = MailCss.Color(theme.Info, nameof(theme.Info));
        _ = MailCss.Color(theme.InfoSubtle, nameof(theme.InfoSubtle));
        _ = MailCss.Color(theme.Success, nameof(theme.Success));
        _ = MailCss.Color(theme.SuccessSubtle, nameof(theme.SuccessSubtle));
        _ = MailCss.Color(theme.Warning, nameof(theme.Warning));
        _ = MailCss.Color(theme.WarningSubtle, nameof(theme.WarningSubtle));
        _ = MailCss.Color(theme.Danger, nameof(theme.Danger));
        _ = MailCss.Color(theme.DangerSubtle, nameof(theme.DangerSubtle));
        if (!MailCss.IsFontFamily(theme.FontFamily))
        {
            throw new ArgumentException($"The mail theme's {nameof(theme.FontFamily)} must be a list of font names, not '{theme.FontFamily}'.", nameof(theme));
        }

        if (MailCss.Pixels(theme.Radius) is null)
        {
            throw new ArgumentException($"The mail theme's {nameof(theme.Radius)} must be a length in px, rem or em, not '{theme.Radius}'.", nameof(theme));
        }

        return theme with
        {
            FontFamily = MailCss.WithSafeFallback(theme.FontFamily),
            Radius = MailCss.Pixels(theme.Radius)!
        };
    }

    /// <summary>
    /// Writes each value in place of its token: in a text, as a text node of its own, so no value is ever
    /// read as markup; in a link's address, as the whole address, kept only for http, https and mailto.
    /// </summary>
    private static void Substitute(IElement body, MailTemplateText text)
    {
        foreach (var node in body.Descendants<IText>().ToArray())
        {
            if (!MailTemplateText.HasToken(node.Data))
            {
                continue;
            }

            var parent = node.Parent!;
            foreach (var piece in text.Pieces(node.Data))
            {
                parent.InsertBefore(body.Owner!.CreateTextNode(piece), node);
            }

            parent.RemoveChild(node);
        }

        foreach (var link in body.QuerySelectorAll("a[href]").ToArray())
        {
            var href = link.GetAttribute("href")!;
            var decoded = href.Contains('%', StringComparison.Ordinal) ? Uri.UnescapeDataString(href) : href;
            if (!MailTemplateText.HasToken(decoded))
            {
                continue;
            }

            var address = text.Replace(decoded).Trim();
            if (IsSafeAddress(address))
            {
                link.SetAttribute("href", address);
            }
            else
            {
                link.RemoveAttribute("href");
            }
        }
    }

    internal static bool IsSafeAddress(string address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri) && SafeSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);

    /// <summary>The body with each link's address written after its text, for the plain-text alternative.</summary>
    private static string PlainTextSource(IElement body)
    {
        var copy = (IElement)body.Clone(deep: true);
        foreach (var link in copy.QuerySelectorAll("a[href]"))
        {
            var href = link.GetAttribute("href")!;
            var address = href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ? href[7..] : href;
            if (!string.Equals(link.TextContent.Trim(), address, StringComparison.Ordinal))
            {
                link.AppendChild(copy.Owner!.CreateTextNode($" ({address})"));
            }
        }

        return copy.InnerHtml;
    }

    /// <summary>Writes each element's look on the element, the editor's alignment and size classes included, then drops every class.</summary>
    private static void Inline(IElement body, OmniMailTheme theme, string lookAttribute)
    {
        foreach (var element in body.Descendants<IElement>().ToArray())
        {
            if (element.HasAttribute(MailBlocks.Own))
            {
                element.RemoveAttribute(MailBlocks.Own);
                continue;
            }

            var declarations = new List<string>();
            if (ElementLooks.TryGetValue(element.LocalName, out var look))
            {
                declarations.Add(look(theme));
            }

            // The sanitiser lets only the editor's alignment and size classes through, each one listed here.
            declarations.AddRange(element.ClassList.Where(ClassLooks.ContainsKey).Select(name => ClassLooks[name]));
            element.RemoveAttribute("class");

            // Only a host's block fragment carries a style (the template's sanitiser drops every one): its declarations
            // come after the theme's, so they win, and move to the preview's attribute there. The block sanitiser has
            // already removed a blank style.
            if (element.GetAttribute("style") is { } own)
            {
                declarations.Add(own.Trim().TrimEnd(';'));
            }

            element.RemoveAttribute("style");
            if (declarations.Count > 0)
            {
                element.SetAttribute(lookAttribute, string.Join(';', declarations));
            }

            if (element.LocalName == "table")
            {
                element.SetAttribute("cellpadding", "0");
                element.SetAttribute("cellspacing", "0");
                element.SetAttribute("width", "100%");
            }
        }
    }

    /// <summary>The look of each element the mail restyles, from the theme; the others inherit the card's text style.</summary>
    private static readonly Dictionary<string, Func<OmniMailTheme, string>> ElementLooks = new(StringComparer.Ordinal)
    {
        ["p"] = _ => "margin:0 0 16px 0",
        ["div"] = _ => "margin:0 0 16px 0",
        ["h1"] = theme => $"margin:0 0 16px 0;font-size:28px;line-height:1.25;font-weight:700;color:{theme.Text}",
        ["h2"] = theme => $"margin:0 0 14px 0;font-size:22px;line-height:1.3;font-weight:700;color:{theme.Text}",
        ["h3"] = theme => $"margin:0 0 12px 0;font-size:18px;line-height:1.35;font-weight:700;color:{theme.Text}",
        ["h4"] = SmallHeading,
        ["h5"] = SmallHeading,
        ["h6"] = SmallHeading,
        ["a"] = theme => $"color:{theme.Link};text-decoration:underline",
        ["blockquote"] = theme => $"margin:0 0 16px 0;padding:0 0 0 12px;border-left:3px solid {theme.Accent};color:{theme.MutedText}",
        ["pre"] = theme => $"margin:0 0 16px 0;padding:12px;background-color:{theme.CodeBackground};font-family:{Monospace};font-size:14px;line-height:1.45;white-space:pre-wrap",
        ["code"] = theme => $"font-family:{Monospace};font-size:14px;background-color:{theme.CodeBackground};padding:1px 4px",
        ["ul"] = _ => "margin:0 0 16px 0;padding:0 0 0 24px",
        ["ol"] = _ => "margin:0 0 16px 0;padding:0 0 0 24px",
        ["li"] = _ => "margin:0 0 4px 0",
        ["table"] = _ => "border-collapse:collapse;width:100%;margin:0 0 16px 0",
        ["th"] = theme => $"border:1px solid {theme.Border};padding:6px 8px;text-align:left;vertical-align:top;font-weight:700;background-color:{theme.CodeBackground}",
        ["td"] = theme => $"border:1px solid {theme.Border};padding:6px 8px;vertical-align:top",
        ["caption"] = theme => $"padding:0 0 8px 0;text-align:left;color:{theme.MutedText}",
        ["hr"] = theme => $"border:0;border-top:1px solid {theme.Border};margin:16px 0;height:0",
        ["mark"] = theme => $"background-color:{theme.Highlight};color:{theme.Text}",
    };

    /// <summary>The editor's alignment and size classes as declarations; <c>end</c> is the right edge, the 24 languages being written left to right.</summary>
    private static readonly Dictionary<string, string> ClassLooks = new(StringComparer.Ordinal)
    {
        ["omni-align-left"] = "text-align:left",
        ["omni-align-center"] = "text-align:center",
        ["omni-align-end"] = "text-align:right",
        ["omni-align-justify"] = "text-align:justify",
        ["omni-font-size-small"] = "font-size:14px",
        ["omni-font-size-normal"] = "font-size:16px",
        ["omni-font-size-large"] = "font-size:20px",
        ["omni-font-size-xlarge"] = "font-size:26px",
    };

    private static string SmallHeading(OmniMailTheme theme) =>
        $"margin:0 0 12px 0;font-size:16px;line-height:1.4;font-weight:700;color:{theme.Text}";
}
