using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Components;

public sealed record OmniOption<TValue>(TValue Value, string Text, bool Disabled = false, string? Group = null);

public sealed class OmniUploadRequest
{
    private readonly Action<double> _reportProgress;

    internal OmniUploadRequest(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken,
        Action<double> reportProgress)
    {
        Files = files;
        CancellationToken = cancellationToken;
        _reportProgress = reportProgress;
    }

    public IReadOnlyList<IBrowserFile> Files { get; }
    public CancellationToken CancellationToken { get; }

    public void ReportProgress(double percentage) => _reportProgress(Math.Clamp(percentage, 0, 100));
}
