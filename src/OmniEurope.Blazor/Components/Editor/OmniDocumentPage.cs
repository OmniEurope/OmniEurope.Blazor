namespace OmniEurope.Blazor.Components;

/// <summary>
/// One page of an <see cref="OmniDocumentViewer"/>: a rendering of the host's document, in exactly one of three
/// forms, an image served by address, a PNG held in memory, or HTML.
/// </summary>
/// <param name="Number">The number of the page in the document, from 1. It names the page for assistive technology.</param>
/// <param name="ImageUrl">The address of a picture of the page, served by the host (a path of the application, an <c>https</c> address or a <c>blob:</c> address).</param>
/// <param name="Image">The bytes of a PNG picture of the page, drawn as a <c>data:</c> address.</param>
/// <param name="Html">
/// The HTML of the page, written by the host's own converter. The viewer sanitises it again with the package
/// allow-list (see <see cref="OmniHtmlSanitizerPolicy"/>), so a script, an event handler attribute or a
/// <c>javascript:</c> address is removed, and an element with <c>data-oe-field</c> is a field of the document.
/// </param>
/// <param name="WidthPt">The width of the page in points (1/72 inch), 595.3 for an A4 portrait page. Null takes the width of the picture, or an A4 width for HTML.</param>
/// <param name="HeightPt">The height of the page in points, 841.9 for an A4 portrait page. An HTML page is at least that high; null lets it follow its content. An image page keeps the proportions of its picture.</param>
public sealed record OmniDocumentPage(
    int Number,
    string? ImageUrl = null,
    byte[]? Image = null,
    string? Html = null,
    double? WidthPt = null,
    double? HeightPt = null)
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Throws when the page is not usable: not one source exactly, a number below 1, a size that is not a
    /// positive finite number, or bytes that are not a PNG.
    /// </summary>
    /// <param name="parameterName">The parameter the page was given through, for the exception.</param>
    /// <exception cref="ArgumentException">The page breaks one of the rules above.</exception>
    internal void EnsureValid(string parameterName)
    {
        var sources = (string.IsNullOrWhiteSpace(ImageUrl) ? 0 : 1) + (Image is null ? 0 : 1) + (Html is null ? 0 : 1);
        if (sources != 1)
        {
            throw new ArgumentException(
                $"Page {Number} must carry exactly one of {nameof(ImageUrl)}, {nameof(Image)} or {nameof(Html)}, found {sources}.",
                parameterName);
        }

        if (Number < 1)
        {
            throw new ArgumentException($"The page number {Number} is below 1.", parameterName);
        }

        if (WidthPt is { } width && !(double.IsFinite(width) && width > 0))
        {
            throw new ArgumentException($"Page {Number} has a {nameof(WidthPt)} of {width}, which is not a positive number.", parameterName);
        }

        if (HeightPt is { } height && !(double.IsFinite(height) && height > 0))
        {
            throw new ArgumentException($"Page {Number} has a {nameof(HeightPt)} of {height}, which is not a positive number.", parameterName);
        }

        if (Image is not null && !Image.AsSpan().StartsWith(PngSignature))
        {
            throw new ArgumentException($"The {nameof(Image)} of page {Number} is not a PNG picture.", parameterName);
        }
    }
}
