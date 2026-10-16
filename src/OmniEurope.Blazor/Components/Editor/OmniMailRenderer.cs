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
/// Three blocks build on that. A paragraph holding <c>{{#button name}}</c> becomes an action button: the
/// paragraph's text (its variables written) is the label, the value of <c>name</c> the address, kept under the
/// same rule as a link's, the fill <see cref="OmniMailTheme.Accent"/> and the label <see cref="OmniMailTheme.OnAccent"/>.
/// A paragraph holding <c>{{#banner tone}}</c> (<c>info</c>, the default, <c>success</c>, <c>warning</c> or
/// <c>danger</c>) becomes a banner across the card, its content kept, on the tone's pale colour with the tone's rule at
/// its start. Both are presentation tables with their look on each element, the label a link padded inside its cell, so
/// Outlook draws them as the web clients do; the marker writes nothing itself. A block variable, a name given in
/// <see cref="OmniMailRenderOptions.Blocks"/>, inserts the host's HTML fragment, sanitised through an allow-list of its
/// own: text, list and table elements, links to http, https and mailto, and inline declarations of colour, weight,
/// size, alignment, padding, border and width, which win over the mail's; scripts, style sheets, <c>link</c>
/// elements, embedded documents, forms, images, event handlers, classes and any declaration with <c>url(</c>,
/// <c>expression(</c>, <c>@import</c> or a backslash are removed. The tokens of a fragment are never replaced.
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
    public static IReadOnlyList<string> FindVariables(string? template) => MailBlocks.Names(template);
}
