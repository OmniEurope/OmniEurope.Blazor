namespace OmniEurope.Blazor.Components;

/// <summary>
/// A light word processor: a sheet of paper to write on, paragraph styles and text sizes, inline
/// formatting, lists, alignment and tables, a live word count, and export to HTML and plain text.
/// </summary>
/// <remarks>
/// It is an <see cref="OmniHtmlEditor"/> in its visual face with the
/// <see cref="OmniHtmlEditorCommands.Document"/> toolbar, so the value is the same sanitised HTML and
/// takes part in a form the same way: <c>@bind-Value</c> passes the expression through.
/// </remarks>
public partial class OmniDocumentEditor
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-document-editor.js";

    private OmniHtmlEditor? _editor;
    private IJSObjectReference? _module;
    private string? _countedValue;
    private (int Words, int Characters) _statistics;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string>>? ValueExpression { get; set; }

    /// <summary>The accessible name of the editor. Empty uses the localized "word processor".</summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>The minimum height of the page, in lines.</summary>
    [Parameter] public int Rows { get; set; } = 20;

    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Whether the word and character counts, and the export buttons, are shown under the page.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>Whether the status bar offers the HTML and plain text downloads.</summary>
    [Parameter] public bool ShowExport { get; set; } = true;

    /// <summary>The name of downloaded files, without extension.</summary>
    [Parameter] public string FileName { get; set; } = "document";

    /// <summary>The title of the exported HTML file. Null uses the first level-one heading, then a localized default.</summary>
    [Parameter] public string? DocumentTitle { get; set; }

    [Parameter] public string? AriaDescribedBy { get; set; }

    /// <summary>The toolbar. Null uses <see cref="OmniHtmlEditorCommands.Document"/>.</summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorCommand>? Commands { get; set; }

    /// <summary>The number of words of the current value.</summary>
    public int WordCount => _statistics.Words;

    /// <summary>The number of characters of the current value, spaces included.</summary>
    public int CharacterCount => _statistics.Characters;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("DocumentEditorLabel") : Label;

    private string? EditorId => Id is null ? null : Id + "-editor";

    // Without @bind-Value there is no expression to pass on; the editor still needs one to name its
    // field, and this one names the parameter it is bound to.
    private Expression<Func<string>> EffectiveValueExpression => ValueExpression ?? (() => Value!);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Recount(Value);
    }

    /// <summary>The document as a complete HTML file, what was typed a moment ago included.</summary>
    public async Task<string> ExportHtmlAsync()
    {
        var html = await CurrentHtmlAsync();
        var title = DocumentTitle ?? OmniHtmlText.FirstHeading(html) ?? Localize("DocumentEditorDefaultTitle");
        return OmniHtmlText.ToStandaloneDocument(html, title, CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>The document as plain text, what was typed a moment ago included.</summary>
    public async Task<string> ExportTextAsync() => OmniHtmlText.ToPlainText(await CurrentHtmlAsync());

    private async Task<string> CurrentHtmlAsync() => _editor is null ? Value ?? string.Empty : await _editor.CaptureAsync();

    private async Task HandleValueChangedAsync(string value)
    {
        Recount(value);
        await ValueChanged.InvokeAsync(value);
    }

    private void Recount(string? value)
    {
        if (!string.Equals(value, _countedValue, StringComparison.Ordinal))
        {
            _countedValue = value;
            _statistics = OmniHtmlText.Count(value);
        }
    }

    private async Task DownloadHtmlAsync() =>
        await DownloadAsync(".html", "text/html;charset=utf-8", await ExportHtmlAsync());

    private async Task DownloadTextAsync() =>
        await DownloadAsync(".txt", "text/plain;charset=utf-8", await ExportTextAsync());

    private async Task DownloadAsync(string extension, string mimeType, string content)
    {
        _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        var name = string.IsNullOrWhiteSpace(FileName) ? "document" : FileName.Trim();
        await _module.InvokeVoidAsync("download", name + extension, mimeType, content);
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
