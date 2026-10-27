using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Upload page: the accepted types, count and size limits of the file fields, which read their files in the browser, then the field shapes of a form.</summary>
public partial class UploadPage
{
    private const long Kilobyte = 1024;
    private const long Megabyte = 1024 * 1024;

    private static readonly IReadOnlyList<string> ImageTypes = ["image/png", "image/jpeg", "image/gif", "image/webp"];
    private static readonly IReadOnlyList<string> DocumentTypes = ["application/pdf"];
    private static readonly IReadOnlyList<string> TextTypes = ["text/plain", "text/csv"];
    private static readonly long[] Sizes = [100 * Kilobyte, Megabyte, 10 * Megabyte];

    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private AcceptFilter AcceptMode { get; set; } = AcceptFilter.Any;

    private double MaximumFilesValue { get; set; } = 5;

    private long MaximumFileSize { get; set; } = 10 * Megabyte;

    private bool UploadDisabled { get; set; }

    private int MaximumFiles => (int)MaximumFilesValue;

    private IReadOnlyList<string> AllowedContentTypes => AcceptMode switch
    {
        AcceptFilter.Images => ImageTypes,
        AcceptFilter.Documents => DocumentTypes,
        AcceptFilter.Text => TextTypes,
        _ => []
    };

    private IReadOnlyList<OmniOption<AcceptFilter>> AcceptOptions =>
    [
        new(AcceptFilter.Any, Text["UploadAcceptAny"]),
        new(AcceptFilter.Images, Text["UploadAcceptImages"]),
        new(AcceptFilter.Documents, Text["UploadAcceptDocuments"]),
        new(AcceptFilter.Text, Text["UploadAcceptText"])
    ];

    private IReadOnlyList<OmniOption<long>> SizeOptions =>
    [
        .. Sizes.Select(size => new OmniOption<long>(size, FormatSize(size)))
    ];

    private string FormatSize(long bytes) => bytes >= Megabyte
        ? string.Format(CultureInfo.CurrentCulture, Text["UploadSizeMegabytes"], (bytes / (double)Megabyte).ToString("0.#", CultureInfo.CurrentCulture))
        : string.Format(CultureInfo.CurrentCulture, Text["UploadSizeKilobytes"], (bytes / (double)Kilobyte).ToString("0.#", CultureInfo.CurrentCulture));

    private enum AcceptFilter
    {
        Any,
        Images,
        Documents,
        Text
    }
}
