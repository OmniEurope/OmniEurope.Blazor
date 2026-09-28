using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// What <see cref="OmniUpload.Upload"/> receives: the accepted files, the cancellation of the upload,
/// a way to report progress and a way to refuse the files with a message.
/// </summary>
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

    /// <summary>The message given to <see cref="Reject"/>; null while the files are not refused.</summary>
    internal string? Rejection { get; private set; }

    public Stream OpenReadStream(IBrowserFile file) =>
        file.OpenReadStream(_maximumFileSize, CancellationToken);

    public void ReportProgress(double percentage) => _reportProgress(Math.Clamp(percentage, 0, 100));

    /// <summary>
    /// Refuses the files: once the callback returns, the field shows <paramref name="message"/> as its
    /// error, offers no retry and does not add the files to its list. The check an application makes
    /// before storing the files (a duplicate name, a quota) goes here, in the same callback.
    /// </summary>
    public void Reject(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Rejection = message;
    }
}
