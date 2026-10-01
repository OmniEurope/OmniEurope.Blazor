namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that downloads a grid's rows as a Markdown file (<see cref="OmniMarkdownTableExporter"/>):
/// every announced row, read through the export's page provider, not the page or window the grid
/// shows. While it reads it is busy; a page that fails produces no file.
/// </summary>
/// <typeparam name="TItem">The type of the exported rows.</typeparam>
public partial class OmniMarkdownExportButton<TItem>
{
    private const string DownloadModulePath = Internal.OmniModules.DocumentEditor;

    private IJSObjectReference? _module;
    private bool _busy;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private OmniMarkdownTableExporter Exporter { get; set; } = default!;

    /// <summary>
    /// Describes the export when the button is pressed (title, columns, header lines, page provider),
    /// so the header reflects the filters in force at that moment.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<OmniMarkdownTableExport<TItem>>? Export { get; set; }

    /// <summary>
    /// The file name without extension; the generation time (UTC) and <c>.md</c> are appended, as in
    /// <c>errors-20260926-140509.md</c>.
    /// </summary>
    [Parameter]
    public string FileName { get; set; } = "export";

    /// <summary>The button text. Null uses the localized "Export Markdown".</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>The button's emphasis; secondary by default.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    /// <summary>The button's size; medium by default.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Disables the button.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Raised with the generated document after the file was handed to the browser.</summary>
    [Parameter]
    public EventCallback<OmniMarkdownTableDocument> OnExport { get; set; }

    /// <summary>
    /// Raised when the export fails (a page that could not be read); no file is produced. Without a
    /// handler the exception propagates to the renderer.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnExportError { get; set; }

    private string EffectiveText => string.IsNullOrWhiteSpace(Text) ? Localize("MarkdownExportButton") : Text;

    /// <summary>The downloaded file's name for a document generated at <paramref name="generatedAt"/>.</summary>
    internal static string StampedFileName(string fileName, DateTimeOffset generatedAt)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "export" : fileName.Trim();
        return string.Create(CultureInfo.InvariantCulture, $"{name}-{generatedAt.UtcDateTime:yyyyMMdd-HHmmss}.md");
    }

    private async Task ExportAsync()
    {
        if (_busy || Export is null)
        {
            return;
        }

        _busy = true;
        OmniMarkdownTableDocument? delivered = null;
        try
        {
            var document = await Exporter.ExportAsync(Export());
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", DownloadModulePath);
            await _module.InvokeVoidAsync("download", StampedFileName(FileName, document.GeneratedAt),
                "text/markdown;charset=utf-8", document.Markdown);
            delivered = document;
        }
        catch (Exception exception) when (OnExportError.HasDelegate && exception is not OperationCanceledException)
        {
            await OnExportError.InvokeAsync(exception);
        }
        finally
        {
            _busy = false;
        }

        // Outside the try, as in the grid's export bar: the file is already with the browser, so an
        // exception of the host's handler is not an export failure and propagates like any handler's.
        if (delivered is not null)
        {
            await OnExport.InvokeAsync(delivered);
        }
    }

    /// <summary>Releases the download script module.</summary>
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
        }

        GC.SuppressFinalize(this);
    }
}
