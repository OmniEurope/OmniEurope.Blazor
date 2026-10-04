using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Writes a table the host built itself (an income statement, a balance sheet, any <see cref="OmniTableExportDocument"/>)
/// in a format and hands the file to the browser, as a grid's export bar does. Registered per scope by
/// <c>AddOmniEuropeBlazor</c>; inject it in a component.
/// </summary>
public sealed class OmniTableExportDownloader : IAsyncDisposable
{
    private readonly IJSRuntime _javaScript;
    private readonly OmniTableExporter _exporter;
    private IJSObjectReference? _module;

    /// <summary>Creates the downloader; <c>AddOmniEuropeBlazor</c> registers one per scope.</summary>
    /// <param name="javaScript">The runtime that triggers the download.</param>
    /// <param name="exporter">The writer of the formats.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public OmniTableExportDownloader(IJSRuntime javaScript, OmniTableExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(javaScript);
        ArgumentNullException.ThrowIfNull(exporter);
        _javaScript = javaScript;
        _exporter = exporter;
    }

    /// <summary>
    /// Writes <paramref name="document"/> in <paramref name="format"/> (see <see cref="OmniTableExporter.RenderAsync"/>)
    /// and downloads it as <paramref name="fileName"/> followed by the format's extension.
    /// </summary>
    /// <param name="document">The table to write.</param>
    /// <param name="format">The file format: Markdown, CSV and Excel are written by the package.</param>
    /// <param name="fileName">The file name without extension, for example <c>compte-de-resultat-2026</c>.</param>
    /// <param name="cancellationToken">Cancels the writing and the hand-over.</param>
    /// <returns>The file handed to the browser.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
    /// <exception cref="NotSupportedException">Nobody writes <paramref name="format"/>.</exception>
    public async Task<OmniTableExportFile> DownloadAsync(
        OmniTableExportDocument document, OmniTableExportFormat format, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var file = await _exporter.RenderAsync(document, format, cancellationToken);
        _module ??= await _javaScript.InvokeAsync<IJSObjectReference>("import", cancellationToken, Internal.OmniModules.DocumentEditor);
        await _module.InvokeVoidAsync("download", cancellationToken, $"{fileName.Trim()}.{file.Extension}", file.ContentType, file.Content.ToArray());
        return file;
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
}
