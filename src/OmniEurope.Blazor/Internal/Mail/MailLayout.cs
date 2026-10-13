using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The document around a mail's body: a full-width table painted with the page colour, the card centred
/// in it at most the chosen width (wrapped, for Outlook, whose Word engine ignores <c>max-width</c>, in a
/// fixed-width table inside its conditional comments), the text style set on the card's cell, the
/// preheader hidden before it.
/// </summary>
/// <remarks>
/// The mail's look is written in <c>style</c> attributes because mail clients read nothing else. The
/// document goes to a mail sender, never into the page: the strict CSP of the page does not apply to it,
/// and the preview of <see cref="OmniMailTemplateEditor"/> gives the same declarations through the CSSOM.
/// </remarks>
internal static class MailLayout
{
    private static readonly HtmlParser Parser = new();

    internal static string Build(string body, string subject, string? preheader, string language, int width, OmniMailTheme theme, string lookAttribute)
    {
        void Look(IElement element, string declarations) => element.SetAttribute(lookAttribute, declarations);
        var px = width.ToString(CultureInfo.InvariantCulture);
        var scheme = theme.Dark ? "dark" : "light";
        var document = Parser.ParseDocument("<!DOCTYPE html><html><head></head><body></body></html>");
        var html = document.DocumentElement;
        html.SetAttribute("lang", language);
        html.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");

        var head = document.Head!;
        Append(head, "meta", ("charset", "utf-8"));
        Append(head, "meta", ("name", "viewport"), ("content", "width=device-width, initial-scale=1"));
        Append(head, "meta", ("name", "x-apple-disable-message-reformatting"));
        Append(head, "meta", ("name", "color-scheme"), ("content", scheme));
        Append(head, "meta", ("name", "supported-color-schemes"), ("content", scheme));
        Append(head, "title").TextContent = subject;

        var page = document.Body!;
        Look(page, $"margin:0;padding:0;width:100%;background-color:{theme.PageBackground}");
        if (!string.IsNullOrEmpty(preheader))
        {
            var hidden = Append(page, "div");
            Look(hidden, "display:none;max-height:0;overflow:hidden;opacity:0;mso-hide:all");
            hidden.TextContent = preheader;
        }

        var outer = Table(page, theme.PageBackground);
        Look(outer, $"width:100%;background-color:{theme.PageBackground}");
        var center = Append(Append(outer, "tbody"), "tr");
        var centerCell = Append(center, "td", ("align", "center"));
        Look(centerCell, "padding:24px 12px");

        centerCell.AppendChild(document.CreateComment(
            $"[if mso]><table role=\"presentation\" width=\"{px}\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr><td><![endif]"));
        var card = Table(centerCell, theme.Surface);
        Look(card, $"width:100%;max-width:{px}px;background-color:{theme.Surface};border:1px solid {theme.Border};border-radius:{theme.Radius};border-collapse:separate");
        var content = Append(Append(Append(card, "tbody"), "tr"), "td");
        Look(content, $"padding:32px;font-family:{theme.FontFamily};font-size:16px;line-height:1.5;color:{theme.Text};text-align:left");
        content.InnerHtml = body;
        centerCell.AppendChild(document.CreateComment("[if mso]></td></tr></table><![endif]"));

        return "<!DOCTYPE html>\n" + html.OuterHtml + "\n";
    }

    private static IElement Table(IElement parent, string background) =>
        Append(parent, "table",
            ("role", "presentation"), ("width", "100%"), ("cellpadding", "0"), ("cellspacing", "0"), ("border", "0"), ("bgcolor", background));

    private static IElement Append(INode parent, string name, params (string Name, string Value)[] attributes)
    {
        var element = parent.Owner!.CreateElement(name);
        foreach (var (attribute, value) in attributes)
        {
            element.SetAttribute(attribute, value);
        }

        parent.AppendChild(element);
        return element;
    }
}
