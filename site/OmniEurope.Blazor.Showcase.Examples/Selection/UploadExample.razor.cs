using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>
/// A single-file field and a several-file field that read their files in the browser. The Upload callback
/// opens each accepted file through the request, counts what it reads and reports the progress; no
/// request leaves the browser.
/// </summary>
public partial class UploadExample
{
    private const long Kilobyte = 1024;
    private const long Megabyte = 1024 * 1024;
    private const int ChunkSize = 64 * 1024;

    /// <summary>The content types accepted, <c>image/*</c> style wildcards included; empty accepts any type.</summary>
    [Parameter]
    public IReadOnlyList<string> AllowedContentTypes { get; set; } = [];

    /// <summary>The most files the several-file field may hold, the files already attached included.</summary>
    [Parameter]
    public int MaximumFiles { get; set; } = 5;

    /// <summary>The largest size of one file, in bytes.</summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * Megabyte;

    /// <summary>Whether both fields are greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private IReadOnlyList<OmniUploadFile> SingleFiles { get; set; } = [];

    /// <summary>The files a case would already have: sample data, as if stored earlier.</summary>
    private IReadOnlyList<OmniUploadFile> AttachedFiles { get; set; } =
    [
        new("report-2026.pdf", 1_258_291, "application/pdf"),
        new("budget-forecast.csv", 48_640, "text/csv"),
        new("team-photo.jpg", 2_516_582, "image/jpeg")
    ];

    private string? LastRemoved { get; set; }

    private IReadOnlyList<ReadFile> ReadFiles { get; set; } = [];

    private string AcceptedTypesEcho => AllowedContentTypes.Count == 0
        ? "Accepted types: all."
        : $"Accepted types: {string.Join(", ", AllowedContentTypes)}";

    private string AttachedEcho =>
        $"{AttachedFiles.Count} file(s) attached, {FormatSize(AttachedFiles.Sum(file => file.Size))} in total";

    private string TotalEcho =>
        $"{ReadFiles.Count} file(s), {FormatSize(ReadFiles.Sum(file => file.Bytes))} in total";

    /// <summary>
    /// Reads every accepted file of the selection in the browser: the request opens each stream within the
    /// size limit of the field, and the loop counts the bytes and, for text, the lines.
    /// </summary>
    private async Task ReadAsync(OmniUploadRequest request)
    {
        var total = request.Files.Sum(file => file.Size);
        var done = 0L;
        var buffer = new byte[ChunkSize];
        var results = new List<ReadFile>(request.Files.Count);
        foreach (var file in request.Files)
        {
            var bytes = 0L;
            var lines = 0L;
            byte last = 0;
            await using var stream = request.OpenReadStream(file);
            int read;
            while ((read = await stream.ReadAsync(buffer, request.CancellationToken)) > 0)
            {
                bytes += read;
                done += read;
                for (var index = 0; index < read; index++)
                {
                    if (buffer[index] == (byte)'\n')
                    {
                        lines++;
                    }
                }

                last = buffer[read - 1];
                request.ReportProgress(total == 0 ? 100 : done * 100d / total);
            }

            var isText = string.IsNullOrEmpty(file.ContentType) || file.ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
            if (isText && bytes > 0 && last != (byte)'\n')
            {
                lines++;
            }

            results.Add(new ReadFile(
                file.Name,
                string.IsNullOrEmpty(file.ContentType) ? "unknown" : file.ContentType,
                bytes,
                FormatSize(file.Size),
                file.LastModified.LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
                isText
                    ? $"{bytes.ToString("N0", CultureInfo.CurrentCulture)} bytes, {lines.ToString("N0", CultureInfo.CurrentCulture)} line(s)"
                    : $"{bytes.ToString("N0", CultureInfo.CurrentCulture)} bytes"));
        }

        ReadFiles = results;
        await InvokeAsync(StateHasChanged);
    }

    private void OnFileRemoved(OmniUploadFile file) => LastRemoved = file.Name;

    private static string FormatSize(long bytes) => bytes switch
    {
        >= Megabyte => $"{(bytes / (double)Megabyte).ToString("0.#", CultureInfo.CurrentCulture)} MB",
        >= Kilobyte => $"{(bytes / (double)Kilobyte).ToString("0.#", CultureInfo.CurrentCulture)} KB",
        _ => $"{bytes} B"
    };

    private sealed record ReadFile(string Name, string ContentType, long Bytes, string Size, string Modified, string Content);
}
