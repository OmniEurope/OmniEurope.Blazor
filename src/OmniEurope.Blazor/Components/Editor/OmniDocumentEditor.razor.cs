using System.Text.RegularExpressions;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A minimal structured editor for a document the host converted to <see cref="OmniDocument"/>: the text of its runs is
/// edited in place, its fields are filled in with the input that matches their kind, and a paragraph may carry a drawing.
/// It reports what changed as an <see cref="OmniDocumentChangeSet"/>; it has no formatting toolbar and no paragraph editing.
/// </summary>
/// <remarks>
/// <para>
/// The component knows no document format, and the package references no document library: the host maps its document to
/// <see cref="OmniDocument"/> and applies the change set back. The editor never changes the document it was given.
/// </para>
/// <para>
/// A run is edited as plain text on one line: Enter makes no paragraph, and a paste or a drop is inserted as plain text with
/// its line breaks turned into spaces (<c>omni-structured-document.js</c> does this, and reads the text back on every input
/// and when the run loses the focus). The run is drawn with the CSS class <see cref="StyleClasses"/> gives for its
/// <see cref="OmniDocumentRun.StyleKey"/>, which is rendering only.
/// </para>
/// <para>
/// A field is a chip with its label and its value; clicking it or pressing Enter or Space on it opens a popover with the
/// input of its kind: <see cref="OmniDatePicker"/> for a date, <see cref="OmniNumeric{TValue}"/> for a number,
/// <see cref="OmniDropDown{TValue}"/> over the items for a list, <see cref="OmniAutocomplete{TValue}"/> over the items when
/// the list allows free text, <see cref="OmniTextBox"/> for text. A choice applies as soon as it is made (a text or number as it
/// is typed) and the popover stays open; Escape or a click outside closes it, Escape giving the focus back to the chip.
/// </para>
/// <para>
/// With <see cref="AllowDrawings"/> each paragraph has an "Insert a drawing" (then "Replace the drawing") button that opens a
/// dialog around an <see cref="OmniInkCanvas"/> shaped like the paragraph's drawing sheet (400 by 150 points for a new
/// one); validating stores the strokes and a transparent PNG in the change set and draws the strokes under the paragraph,
/// as an inline SVG, so no <c>data:</c> address is involved. The paragraph shows each stroke as one line of its width:
/// the pen pressure shapes only the PNG. <see cref="ReadOnly"/> turns all editing off.
/// </para>
/// <para>
/// <see cref="Culture"/> formats the date and number a field chip shows. The date picker and the number input a chip opens
/// follow the culture of the page, like every input of the package.
/// </para>
/// </remarks>
public partial class OmniDocumentEditor : IAsyncDisposable
{
    private const double DefaultDrawingWidthPt = 400;
    private const double DefaultDrawingHeightPt = 150;
    private const string ModulePath = Internal.OmniModules.StructuredDocument;

    private static readonly Regex LineBreaks = new(@"\s*[\r\n]+\s*", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private readonly string _generatedId = $"omni-document-editor-{Guid.NewGuid():N}";
    private readonly Dictionary<string, string> _runEdits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _fieldEdits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (OmniDocumentDrawing Drawing, byte[] Png)> _drawingEdits = new(StringComparer.Ordinal);
    private ElementReference _root;
    private Task<IJSObjectReference?>? _moduleTask;
    private DotNetObjectReference<StructuredDocumentInteropBridge>? _bridge;
    private DocumentEditorIndex _index = default!;
    private OmniDocument? _readDocument;
    private int _generation;
    private bool _plainTextOnly = true;
    private bool _disposed;
    private OmniDocumentBlock? _draftBlock;
    private IReadOnlyList<OmniInkStroke> _draft = [];
    private double _draftWidthPt = DefaultDrawingWidthPt;
    private double _draftHeightPt = DefaultDrawingHeightPt;
    private OmniInkCanvas? _draftCanvas;

    /// <summary>
    /// The document to edit. Required; never changed by the editor. The editor reads it again, and drops the changes it
    /// holds, when the parameter is given another instance, so give a new document to start from a new text and keep
    /// the same one to keep editing.
    /// </summary>
    /// <exception cref="ArgumentNullException">The document is null.</exception>
    /// <exception cref="ArgumentException">The document breaks a rule of the model: a missing or duplicate id, a value that does not fit its field's kind, a list without items that does not allow free text, an unknown inline, a drawing without a usable size.</exception>
    [Parameter, EditorRequired]
    public OmniDocument Document { get; set; } = default!;

    /// <summary>
    /// Raised after each edit (a keystroke in a run, a value chosen in a field, a drawing validated) with the change set
    /// accumulated since the <see cref="Document"/> the component last received. An edit that puts a run or a field back to
    /// what the document says is raised too, with a change set that no longer holds it.
    /// </summary>
    [Parameter]
    public EventCallback<OmniDocumentChangeSet> OnChange { get; set; }

    /// <summary>Whether all editing is off: the text cannot be typed in, the chips open nothing, the drawing buttons are gone. Off by default.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gives the CSS class a run is drawn with, from its <see cref="OmniDocumentRun.StyleKey"/> (called only for a run that has
    /// one). A null or blank class adds nothing. Null, the default, draws every run alike. Rendering only: a change set never
    /// mentions a style.
    /// </summary>
    [Parameter]
    public Func<string, string?>? StyleClasses { get; set; }

    /// <summary>Whether each paragraph offers a drawing (the button, the dialog, the drawing shown under the paragraph). True by default; false still shows the drawings the document holds.</summary>
    [Parameter]
    public bool AllowDrawings { get; set; } = true;

    /// <summary>The culture the date and number of a field chip are written in. Null, the default, is the culture of the page.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>The accessible name of the editor. Null, the default, is the localized "Document".</summary>
    [Parameter]
    public string? Label { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private string RootId => Id ?? _generatedId;

    private string EffectiveLabel => LocalizeOr(Label, "DocumentViewerLabel");

    private string? RunEditable => ReadOnly ? null : _plainTextOnly ? "plaintext-only" : "true";

    private CultureInfo Format => Culture ?? CultureInfo.CurrentCulture;

    private double DraftAspect => _draftWidthPt / _draftHeightPt;

    /// <summary>
    /// Reads the document again, and starts its changes over, when the host gave another instance; the new document is
    /// checked first, so one that breaks a rule leaves the editor as it was.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="Document"/> is null.</exception>
    /// <exception cref="ArgumentException">The document breaks a rule of the model.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Document);
        if (!ReferenceEquals(Document, _readDocument))
        {
            _index = DocumentEditorIndex.Build(Document, nameof(Document));
            _readDocument = Document;
            Discard();
        }
    }

    /// <summary>
    /// On the first render attaches the script that reads the runs back, and learns from it whether the browser edits a
    /// span as plain text (it draws the spans editable another way when it does not). A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render.</param>
    /// <returns>A task that completes when the script is attached.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _moduleTask = OmniScriptModule.ImportAsync(JavaScript, ModulePath, () => _disposed);
            var module = await _moduleTask;
            if (module is null)
            {
                return;
            }

            _bridge = DotNetObjectReference.Create(new StructuredDocumentInteropBridge(HandleRunTextAsync));
            _plainTextOnly = await module.InvokeAsync<bool>("attach", _root, _bridge);
            if (!_plainTextOnly)
            {
                StateHasChanged();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the editor with it.
        }
    }

    /// <summary>Detaches and releases the editor script and its .NET reference.</summary>
    /// <returns>A task that completes when the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;
                if (module is not null)
                {
                    await module.InvokeVoidAsync("detach", _root);
                    await module.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// What changed since the <see cref="Document"/> the component last received: the runs whose text differs, the fields whose
    /// value differs and the drawings made, each in the order of the document. A new object each time.
    /// </summary>
    /// <returns>The change set; empty (see <see cref="OmniDocumentChangeSet.IsEmpty"/>) when nothing was changed.</returns>
    public OmniDocumentChangeSet GetChangeSet()
    {
        List<OmniRunTextChange> runs = [];
        List<OmniFieldValueChange> fields = [];
        List<OmniDrawingChange> drawings = [];
        foreach (var block in Document.Blocks)
        {
            if (_drawingEdits.TryGetValue(block.Id, out var edit))
            {
                drawings.Add(new OmniDrawingChange(block.Id, edit.Drawing, edit.Png));
            }

            foreach (var inline in block.Inlines)
            {
                switch (inline)
                {
                    case OmniDocumentRun run when _runEdits.TryGetValue(run.Id, out var text):
                        runs.Add(new OmniRunTextChange(run.Id, text));
                        break;
                    case OmniDocumentField field when _fieldEdits.TryGetValue(field.Id, out var value):
                        fields.Add(new OmniFieldValueChange(field.Id, field.Tag, value));
                        break;
                }
            }
        }

        return new OmniDocumentChangeSet(runs, fields, drawings);
    }

    /// <summary>
    /// Drops every change: the runs, fields and drawings are drawn as the <see cref="Document"/> says again, and the next change
    /// set starts empty. <see cref="OnChange"/> is not raised. The host calls it once it has applied a change set to its own
    /// document without handing the editor a new <see cref="Document"/>.
    /// </summary>
    public void ResetChanges()
    {
        Discard();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>The text of a run the script read back; the same entry point as the script's, for the tests of the component.</summary>
    internal Task HandleRunTextAsync(string runId, string text) => InvokeAsync(async () =>
    {
        if (_disposed || ReadOnly || !_index.Runs.TryGetValue(runId, out var run))
        {
            return;
        }

        var line = LineBreaks.Replace(text, " ");
        var shown = _runEdits.TryGetValue(runId, out var edit) ? edit : run.Text;
        if (string.Equals(line, shown, StringComparison.Ordinal))
        {
            return;
        }

        var wasEdited = edit is not null;
        if (string.Equals(line, run.Text, StringComparison.Ordinal))
        {
            _runEdits.Remove(runId);
        }
        else
        {
            _runEdits[runId] = line;
        }

        if (wasEdited != _runEdits.ContainsKey(runId))
        {
            StateHasChanged();
        }

        await OnChange.InvokeAsync(GetChangeSet());
    });

    private void Discard()
    {
        _runEdits.Clear();
        _fieldEdits.Clear();
        _drawingEdits.Clear();
        _generation++;
        CloseDrawing();
    }

    // ----- Runs ------------------------------------------------------------------------------------------

    private string RunClass(OmniDocumentRun run)
    {
        var keyed = run.StyleKey is null ? null : StyleClasses?.Invoke(run.StyleKey);
        return Css("omni-document-editor__run", _runEdits.ContainsKey(run.Id) ? "omni-document-editor__run--changed" : null, keyed);
    }

    // ----- Fields ----------------------------------------------------------------------------------------

    private string? ValueOf(OmniDocumentField field)
        => _fieldEdits.TryGetValue(field.Id, out var edit) ? edit : DocumentEditorIndex.Normalize(field.Value);

    private DateOnly? DateOf(OmniDocumentField field)
        => ValueOf(field) is { } value ? DateOnly.ParseExact(value, DocumentEditorIndex.DateFormat, CultureInfo.InvariantCulture) : null;

    private decimal? NumberOf(OmniDocumentField field)
        => ValueOf(field) is { } value ? decimal.Parse(value, CultureInfo.InvariantCulture) : null;

    private string? FieldValueText(OmniDocumentField field) => ValueOf(field) is not { } value
        ? null
        : field.Kind switch
        {
            OmniDocumentFieldKind.Date => DateOf(field)!.Value.ToString("d", Format),
            OmniDocumentFieldKind.Number => NumberOf(field)!.Value.ToString(Format),
            _ => value
        };

    private string FieldText(OmniDocumentField field) => FieldValueText(field) ?? Localize("DocumentEditorFieldEmpty");

    private string FieldValueClass(OmniDocumentField field)
        => CssClassBuilder.Combine(["omni-document-editor__chip-value", ValueOf(field) is null ? "omni-document-editor__chip-value--empty" : null]);

    private string FieldChipClass(OmniDocumentField field, bool readOnly)
        => CssClassBuilder.Combine([
            "omni-document-editor__chip",
            readOnly ? "omni-document-editor__chip--readonly" : null,
            _fieldEdits.ContainsKey(field.Id) ? "omni-document-editor__chip--changed" : null
        ]);

    private static IReadOnlyList<OmniOption<string>> OptionsOf(OmniDocumentField field)
        => [.. (field.Items ?? []).Select(item => new OmniOption<string>(item, item))];

    private static Task<IReadOnlyList<OmniOption<string>>> SearchItemsAsync(OmniDocumentField field, string text)
        => Task.FromResult<IReadOnlyList<OmniOption<string>>>([.. OptionsOf(field).Where(option => option.Text.Contains(text, StringComparison.OrdinalIgnoreCase))]);

    private Task ChooseDateAsync(OmniDocumentField field, DateOnly? date)
        => ChooseAsync(field, date?.ToString(DocumentEditorIndex.DateFormat, CultureInfo.InvariantCulture));

    private Task ChooseNumberAsync(OmniDocumentField field, decimal? number)
        => ChooseAsync(field, number?.ToString(CultureInfo.InvariantCulture));

    private async Task ChooseAsync(OmniDocumentField field, string? chosen)
    {
        var value = DocumentEditorIndex.Normalize(chosen);
        if (DocumentEditorIndex.SameValue(field.Kind, value, ValueOf(field)))
        {
            return;
        }

        if (DocumentEditorIndex.SameValue(field.Kind, value, DocumentEditorIndex.Normalize(field.Value)))
        {
            _fieldEdits.Remove(field.Id);
        }
        else
        {
            _fieldEdits[field.Id] = value;
        }

        await OnChange.InvokeAsync(GetChangeSet());
    }

    // ----- Drawings --------------------------------------------------------------------------------------

    private OmniDocumentDrawing? DrawingOf(OmniDocumentBlock block)
        => _drawingEdits.TryGetValue(block.Id, out var edit) ? edit.Drawing : block.Drawing;

    private void OpenDrawing(OmniDocumentBlock block)
    {
        var drawing = DrawingOf(block);
        _draftBlock = block;
        _draft = drawing?.Strokes ?? [];
        _draftWidthPt = drawing?.WidthPt ?? DefaultDrawingWidthPt;
        _draftHeightPt = drawing?.HeightPt ?? DefaultDrawingHeightPt;
    }

    private void CancelDrawing() => CloseDrawing();

    private void CloseDrawing()
    {
        _draftBlock = null;
        _draftCanvas = null;
        _draft = [];
        _draftWidthPt = DefaultDrawingWidthPt;
        _draftHeightPt = DefaultDrawingHeightPt;
    }

    private async Task ApplyDrawingAsync()
    {
        var block = _draftBlock!;
        var drawing = new OmniDocumentDrawing(_draft, _draftWidthPt, _draftHeightPt);
        var png = await _draftCanvas!.ExportPngAsync(DocumentEditorIndex.Pixels(drawing.WidthPt), DocumentEditorIndex.Pixels(drawing.HeightPt));
        _drawingEdits[block.Id] = (drawing, png);
        CloseDrawing();
        await OnChange.InvokeAsync(GetChangeSet());
    }

    private static double SheetWidth(OmniDocumentDrawing drawing) => drawing.WidthPt * 96d / 72d;

    private static double SheetHeight(OmniDocumentDrawing drawing) => drawing.HeightPt * 96d / 72d;

    // A line is as wide as its width says at OmniInkStroke.ReferenceWidth, scaled to the sheet.
    private static double LineWidth(OmniInkStroke stroke, OmniDocumentDrawing drawing)
        => stroke.Width * SheetWidth(drawing) / OmniInkStroke.ReferenceWidth;

    private static string PointsOf(OmniInkStroke stroke, OmniDocumentDrawing drawing)
        => string.Join(' ', stroke.Points.Select(point => $"{Number(point.X * SheetWidth(drawing))},{Number(point.Y * SheetHeight(drawing))}"));

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
