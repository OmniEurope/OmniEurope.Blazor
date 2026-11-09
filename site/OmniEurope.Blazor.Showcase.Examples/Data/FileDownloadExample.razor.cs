using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A button that generates a small text file and hands it to the browser as a download.</summary>
public partial class FileDownloadExample
{
    [Inject]
    private OmniFileDownloader Downloader { get; set; } = default!;

    private string Status { get; set; } = "Nothing downloaded yet.";

    private static string Report() => string.Create(
        CultureInfo.InvariantCulture, $"OmniEurope.Blazor file download\nGenerated on {DateTimeOffset.UtcNow:O}\n");

    private async Task DownloadBytesAsync()
    {
        var bytes = Encoding.UTF8.GetBytes(Report());
        await Downloader.DownloadAsync(bytes, "omni-report.txt", "text/plain");
        Status = $"omni-report.txt handed over, {bytes.Length} bytes.";
    }

    private async Task DownloadStreamAsync()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Report()));
        await Downloader.DownloadAsync(stream, "omni-report-stream.txt", "text/plain");
        Status = $"omni-report-stream.txt handed over from a stream, {stream.Length} bytes.";
    }
}
