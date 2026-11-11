using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Hands a file the host holds in memory, or reads from a stream, to the browser as a download: a Blob
/// behind an object URL that a hidden link follows, then revokes. Nothing goes over the network and the
/// strict CSP needs no allowance. Registered per scope by <c>AddOmniEuropeBlazor</c>; inject it in a
/// component. <see cref="OmniTableExportDownloader"/> uses it to hand over the tables it writes.
/// </summary>
public sealed class OmniFileDownloader : IAsyncDisposable
{
    private readonly IJSRuntime _javaScript;
    private IJSObjectReference? _module;

    /// <summary>Creates the downloader; <c>AddOmniEuropeBlazor</c> registers one per scope.</summary>
    /// <param name="javaScript">The runtime that triggers the download.</param>
    /// <exception cref="ArgumentNullException"><paramref name="javaScript"/> is null.</exception>
    public OmniFileDownloader(IJSRuntime javaScript)
    {
        ArgumentNullException.ThrowIfNull(javaScript);
        _javaScript = javaScript;
    }

    /// <summary>Downloads <paramref name="content"/> as <paramref name="fileName"/>.</summary>
    /// <param name="content">The bytes of the file; they travel to the browser in one piece, so a large file goes through the <see cref="Stream"/> overload.</param>
    /// <param name="fileName">The name the browser proposes, extension included.</param>
    /// <param name="contentType">The media type of the file.</param>
    /// <param name="cancellationToken">Cancels the hand-over.</param>
    /// <returns>A task that completes when the browser has been handed the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> or <paramref name="contentType"/> is empty.</exception>
    public async ValueTask DownloadAsync(
        byte[] content, string fileName, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        var module = await ModuleAsync(cancellationToken);
        await module.InvokeVoidTrackedAsync("download", cancellationToken, fileName.Trim(), contentType.Trim(), content);
    }

    /// <summary>
    /// Downloads what <paramref name="content"/> holds as <paramref name="fileName"/>, streamed to the
    /// browser rather than copied into one message, for a large file. The stream is read from where it is
    /// and left open: the caller disposes it.
    /// </summary>
    /// <param name="content">The readable stream of the file.</param>
    /// <param name="fileName">The name the browser proposes, extension included.</param>
    /// <param name="contentType">The media type of the file.</param>
    /// <param name="cancellationToken">Cancels the hand-over.</param>
    /// <returns>A task that completes when the browser has been handed the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="content"/> is not readable, or <paramref name="fileName"/> or <paramref name="contentType"/> is empty.</exception>
    public async ValueTask DownloadAsync(
        Stream content, string fileName, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (!content.CanRead)
        {
            throw new ArgumentException("The stream cannot be read.", nameof(content));
        }

        var module = await ModuleAsync(cancellationToken);
        using var reference = new DotNetStreamReference(content, leaveOpen: true);
        await module.InvokeVoidTrackedAsync("downloadStream", cancellationToken, fileName.Trim(), contentType.Trim(), reference);
    }

    /// <summary>Releases the download script module.</summary>
    /// <returns>A task that completes when the module is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the module with it.
            }

            _module = null;
        }
    }

    private async ValueTask<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken)
        => _module ??= await Internal.OmniScriptModule.ImportAsync(_javaScript, Internal.OmniModules.DocumentEditor, cancellationToken);
}
