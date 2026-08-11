using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Components;

public sealed class OmniUploadRequest
{
    private readonly Action<double> _reportProgress;
    private readonly long _maximumFileSize;

    internal OmniUploadRequest(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken,
        long maximumFileSize,
        Action<double> reportProgress)
    {
        Files = files;
        CancellationToken = cancellationToken;
        _maximumFileSize = maximumFileSize;
        _reportProgress = reportProgress;
    }

    public IReadOnlyList<IBrowserFile> Files { get; }
    public CancellationToken CancellationToken { get; }

    public Stream OpenReadStream(IBrowserFile file) =>
        file.OpenReadStream(_maximumFileSize, CancellationToken);

    public void ReportProgress(double percentage) => _reportProgress(Math.Clamp(percentage, 0, 100));
}
