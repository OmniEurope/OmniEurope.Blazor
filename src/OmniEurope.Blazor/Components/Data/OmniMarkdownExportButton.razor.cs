namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that downloads a grid's rows as a Markdown file (<see cref="OmniMarkdownTableExporter"/>):
/// every announced row, read through the export's page provider, not the page or window the grid
/// shows. While it reads it is busy; a page that fails produces no file.
/// </summary>
public partial class OmniMarkdownExportButton<TItem>
{
    private const string DownloadModulePath = "./_content/OmniEurope.Blazor/omni-document-editor.js";

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

    /// <summary>The button text. Empty uses the localized "Export Markdown".</summary>
    [Parameter]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Raised after the file was handed to the browser.</summary>
    [Parameter]
    public EventCallback<OmniMarkdownTableDocument> OnExported { get; set; }

    /// <summary>
    /// Raised when the export fails (a page that could not be read); no file is produced. Without a
    /// handler the exception propagates to the renderer.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnError { get; set; }

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
        try
        {
            var document = await Exporter.ExportAsync(Export());
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", DownloadModulePath);
            await _module.InvokeVoidAsync("download", StampedFileName(FileName, document.GeneratedAt),
                "text/markdown;charset=utf-8", document.Markdown);
            await OnExported.InvokeAsync(document);
        }
        catch (Exception exception) when (OnError.HasDelegate && exception is not OperationCanceledException)
        {
            await OnError.InvokeAsync(exception);
        }
        finally
        {
            _busy = false;
        }
    }

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
