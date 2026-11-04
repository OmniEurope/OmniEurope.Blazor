using AngleSharp.Dom;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// A page of the viewer once read: the address its picture is drawn from, or the sanitised body of its HTML. A class
/// rather than a record so that each page is its own key when the viewer diffs its pages.
/// </summary>
/// <param name="Page">The page the host gave.</param>
/// <param name="ImageSource">The address of the picture (the host's address, or a <c>data:</c> address for PNG bytes); null for an HTML page.</param>
/// <param name="Body">The sanitised HTML of the page, parsed; null for an image page.</param>
internal sealed class DocumentPageView(OmniDocumentPage Page, string? ImageSource, IElement? Body)
{
    /// <summary>The page the host gave.</summary>
    internal OmniDocumentPage Page { get; } = Page;

    /// <summary>The address of the picture, or null for an HTML page.</summary>
    internal string? ImageSource { get; } = ImageSource;

    /// <summary>The sanitised body of the HTML, or null for an image page.</summary>
    internal IElement? Body { get; } = Body;

    /// <summary>Reads a page: validates it, turns PNG bytes into a <c>data:</c> address and sanitises HTML with <paramref name="policy"/>.</summary>
    /// <param name="page">The page the host gave.</param>
    /// <param name="parameterName">The parameter it came through, for the exception of an invalid page.</param>
    /// <param name="policy">The policy HTML is sanitised with.</param>
    /// <returns>The page, read.</returns>
    internal static DocumentPageView Read(OmniDocumentPage page, string parameterName, OmniHtmlSanitizerPolicy policy)
    {
        page.EnsureValid(parameterName);
        if (page.Html is { } html)
        {
            return new DocumentPageView(page, null, DocumentViewerMarkup.Parse(html, policy));
        }

        var source = page.Image is { } bytes
            ? "data:image/png;base64," + Convert.ToBase64String(bytes)
            : page.ImageUrl!.Trim();
        return new DocumentPageView(page, source, null);
    }
}
