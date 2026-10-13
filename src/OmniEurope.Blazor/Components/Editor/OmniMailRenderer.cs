using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Renders a mail template, the HTML written with <see cref="OmniMailTemplateEditor"/> (or any
/// <see cref="OmniHtmlEditor"/>), into the HTML mail clients display: on the server, with no browser and no
/// stylesheet, ready to hand to whatever sends the mail.
/// </summary>
/// <remarks>
/// <para>
/// The template is sanitised as the editor's value is, then each <c>{{name}}</c> is replaced by its value:
/// in the text, as text (a value is never read as markup); as the whole address of a link, kept only when
/// the result is an absolute <c>http</c>, <c>https</c> or <c>mailto</c> address, the link losing its
/// address otherwise.
/// </para>
/// <para>
/// The layout is the one mail clients agree on: presentation tables, every style written on its element
/// (Gmail and Outlook drop style sheets and classes), the editor's alignment and size classes turned into
/// declarations, a card of <see cref="OmniMailRenderOptions.Width"/> pixels for Outlook's Word engine,
/// colours and font from <see cref="OmniMailTheme"/>. Dark mode, web fonts and media queries are left out:
/// clients that honour them are a minority, and the mail reads the same without them.
/// </para>
/// </remarks>
public static class OmniMailRenderer
{
    /// <summary>Renders a template with its values.</summary>
    /// <param name="template">The body template, editor HTML with <c>{{variables}}</c>; null or empty renders an empty card.</param>
    /// <param name="values">The value of each variable, by name as written; a name absent or null renders nothing and is listed in <see cref="OmniMailRenderResult.MissingVariables"/>.</param>
    /// <param name="options">Subject, preheader, language, width and theme; the defaults when null.</param>
    /// <exception cref="ArgumentException">A colour, font or radius of the theme is not a literal value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The width is not between 320 and 1200 pixels.</exception>
    public static OmniMailRenderResult Render(
        string? template,
        IReadOnlyDictionary<string, string?>? values = null,
        OmniMailRenderOptions? options = null) =>
        MailDocument.Render(template, values ?? new Dictionary<string, string?>(), options ?? new OmniMailRenderOptions());

    /// <summary>The names of the variables a template uses (<c>{{name}}</c>), each once, in the order they first appear.</summary>
    public static IReadOnlyList<string> FindVariables(string? template) => MailTemplateText.Names(template);
}
