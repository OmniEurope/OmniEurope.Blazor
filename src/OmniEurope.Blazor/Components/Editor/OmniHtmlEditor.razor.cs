using System.Net;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// An HTML editor with two faces: the formatted document edited in place, and its source. The value
/// is always sanitised with an allow-list (HtmlSanitizer), whichever face produced it.
/// </summary>
/// <remarks>
/// The toolbar is a list of <see cref="OmniHtmlEditorCommand"/>: <see cref="Commands"/> replaces the
/// default one, which is <see cref="OmniHtmlEditorCommands.Default"/>. The visual face is driven by
/// <c>omni-html-editor.js</c>, which never writes a style attribute; the source face keeps the
/// textarea and the tag wrapping this component always had. <see cref="Sheet"/>,
/// <see cref="ShowStatusBar"/> and <see cref="OmniHtmlEditorCommands.Document"/> make it a light word
/// processor: a sheet of paper, live word and character counts, and export to HTML and plain text.
/// </remarks>
public partial class OmniHtmlEditor
{
    private const string DownloadModulePath = OmniModules.DocumentEditor;

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly string _generatedId = $"omni-html-editor-{Guid.NewGuid():N}";
    private readonly HtmlEditorPanels _panels = new();
    private ElementReference _source;
    private ElementReference _surface;
    private ElementReference _linkInput;
    private HtmlEditorVisualSurface? _visual;
    private HtmlEditorProofreading? _proofreading;
    private HtmlEditorSourceFace? _sourceFace;
    private HtmlEditorContextMenu? _menu;
    private OmniHtmlEditorMode _mode;
    private bool _modeInitialized;
    private OmniHtmlEditorMode _modeParameter;
    private HtmlEditorExtensionSet _extensionSet = HtmlEditorExtensionSet.Empty;
    private OmniHtmlEditorSelection? _caret;
    private HtmlEditorFormatState _selection = HtmlEditorFormatState.Empty;
    private int _selectGeneration;
    private bool _showBlocks;
    private bool _disposed;
    private IJSObjectReference? _downloadModule;
    private HtmlEditorToolbarFit? _toolbarFit;
    private ElementReference _toolbar;
    private string? _countedValue;
    private bool _counted;
    private (int Words, int Characters) _statistics;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// The accessible name of the editor. In the source face it is the <c>aria-label</c> of the text area,
    /// written only when set, so that the <c>label</c> of an enclosing <see cref="OmniFormField"/> names
    /// it otherwise. The visual face is an editable <c>div</c>, which a <c>label for</c> cannot name: inside
    /// an <see cref="OmniFormField"/> whose <c>For</c> is <see cref="OmniInputBase{TValue}.Id"/>, it and
    /// the region around the toolbar are named by that field's label (<c>aria-labelledby</c>); elsewhere
    /// they fall back to the localized "HTML editor", or "word processor" when <see cref="Sheet"/> is set.
    /// </summary>
    [Parameter] public string? Label { get; set; }
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize(Sheet ? "DocumentEditorLabel" : "HtmlEditorLabel")
        : Label;

    /// <summary>The label of the enclosing form field when it names this editor and no <see cref="Label"/> is set.</summary>
    private string? VisualLabelledBy => string.IsNullOrWhiteSpace(Label) ? FormFieldLabelId : null;

    /// <summary>The <c>aria-label</c> of the visual face and its regions: none when a form field label names them.</summary>
    private string? VisualAriaLabel => VisualLabelledBy is null ? EffectiveLabel : null;

    /// <summary>
    /// Presents the visual face as a word processor: a sheet of paper centred on a muted background,
    /// with the toolbar sticking to the top while the page scrolls. Off by default. Pair it with
    /// <see cref="OmniHtmlEditorCommands.Document"/> as <see cref="Commands"/> and
    /// <see cref="ShowStatusBar"/> for a complete light word processor.
    /// </summary>
    [Parameter] public bool Sheet { get; set; }

    /// <summary>
    /// Shows a status bar under the editor: the word and character counts of the value (spaces
    /// included), and two buttons that download it as a standalone HTML file and as plain text. Off
    /// by default.
    /// </summary>
    [Parameter] public bool ShowStatusBar { get; set; }

    /// <summary>The name of the files the status bar downloads, without extension.</summary>
    [Parameter] public string FileName { get; set; } = "document";

    /// <summary>
    /// The title of the exported HTML file (<see cref="ExportHtmlAsync"/>). Null uses the first
    /// level-one heading of the value, then the localized "Document".
    /// </summary>
    [Parameter] public string? DocumentTitle { get; set; }

    /// <summary>The number of words of the current value.</summary>
    public int WordCount => Statistics.Words;

    /// <summary>The number of characters of the current value, spaces included.</summary>
    public int CharacterCount => Statistics.Characters;

    /// <summary>Whether the editor is framed as a document: the sheet, the status bar, or both.</summary>
    private bool Framed => Sheet || ShowStatusBar;

    // Class goes on the outermost element: the document frame when there is one, the editor otherwise.
    private string FrameClass => CssClassBuilder.Combine(["omni-document-editor", Sheet ? "omni-document-editor--sheet" : null, Class]);

    private string EditorClass => CssClassBuilder.Combine([
        "omni-html-editor",
        _mode == OmniHtmlEditorMode.Visual ? "omni-html-editor--visual" : "omni-html-editor--source",
        _showBlocks && _mode == OmniHtmlEditorMode.Visual ? "omni-html-editor--show-blocks" : null,
        Disabled ? "omni-html-editor--disabled" : null,
        ReadOnly && !Disabled ? "omni-html-editor--readonly" : null,
        CssClass,
        Framed ? null : Class]);

    /// <summary>The counts of the value, computed again only when it changes.</summary>
    private (int Words, int Characters) Statistics
    {
        get
        {
            var value = CurrentValue;
            if (!string.Equals(value, _countedValue, StringComparison.Ordinal) || !_counted)
            {
                _countedValue = value;
                _counted = true;
                _statistics = OmniHtmlText.Count(value);
            }

            return _statistics;
        }
    }

    /// <summary>The height of the source textarea in rows, and the minimum height of the visual surface.</summary>
    [Parameter] public int Rows { get; set; } = 12;

    /// <summary>
    /// Whether the editor is disabled: nothing can be typed or run, the toolbar is disabled, the source
    /// text area is disabled and the whole editor is dimmed.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Whether the value can be read, selected and copied but not changed: nothing can be typed, the
    /// toolbar is disabled, the visual surface is announced read-only (<c>aria-readonly</c>) and the
    /// source text area is <c>readonly</c>; the editor keeps its focus and is not dimmed. Inline elements
    /// still act when clicked. <see cref="Disabled"/> wins over it.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Whether the value is closed to editing, read-only or disabled.</summary>
    internal bool IsLocked => ReadOnly || Disabled;

    /// <summary>Whether the source face shows a sanitised preview under the textarea. The visual face is its own preview.</summary>
    [Parameter] public bool ShowPreview { get; set; } = true;

    /// <summary>Identifiers of the elements that describe the editor, written as <c>aria-describedby</c> on the visual surface and the source text area; none when null.</summary>
    [Parameter] public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// The toolbar, in order. Null uses <see cref="OmniHtmlEditorCommands.Default"/>; an application
    /// adds or reorders commands by passing its own list, typically built from that one.
    /// </summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorCommand>? Commands { get; set; }

    /// <summary>
    /// The most rows the toolbar takes (recette R-042): the buttons that would open a row beyond
    /// it move, from the end, into the "more" menu (⋮) at the end of the bar, and come back when the bar
    /// widens. Null, the default: the bar wraps onto as many rows as its commands need.
    /// </summary>
    [Parameter] public int? ToolbarRows { get; set; }

    /// <summary>
    /// Shows each command's name beside its icon while the toolbar is at least 64rem wide; narrower, the
    /// icons stand alone and the name stays in the tooltip. Off by default.
    /// </summary>
    [Parameter] public bool ToolbarLabels { get; set; }

    /// <summary>The face shown. The editor's own source button changes it and raises <see cref="ModeChanged"/>.</summary>
    [Parameter] public OmniHtmlEditorMode Mode { get; set; }
    /// <summary>Raised with the new face when the editor's own source button switches it; not raised when the parent changes <see cref="Mode"/>.</summary>
    [Parameter] public EventCallback<OmniHtmlEditorMode> ModeChanged { get; set; }

    /// <summary>
    /// What applications add to this editor, in order: commands placed in the toolbar, markup kept by the
    /// sanitiser, shortcuts, inline elements that act when clicked, a context menu and table readers.
    /// A parent may pass a new list on every render: the extensions are compared by instance.
    /// </summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorExtension>? Extensions { get; set; }

    /// <summary>
    /// Elements, attributes and classes kept beyond the built-in allow-list, wherever the editor sanitises:
    /// the policies of the <see cref="Extensions"/> merged. Null keeps the built-in allow-list alone.
    /// </summary>
    private OmniHtmlSanitizerPolicy? EffectivePolicy => _extensionSet.Policy;

    /// <summary>
    /// Raised, a moment after the caret or the selection stops moving in the visual face, with the
    /// elements around it. Not raised in the source face, nor for a move that changes nothing.
    /// </summary>
    [Parameter] public EventCallback<OmniHtmlEditorSelection> SelectionChanged { get; set; }

    /// <summary>The last selection the visual face reported, or null before the first one.</summary>
    internal OmniHtmlEditorSelection? CurrentSelection => _caret;

    internal string CurrentHtml => CurrentValue ?? string.Empty;

    /// <summary>The value as it stands, null included, for the collaborators that compare it.</summary>
    internal string? EditorValue => CurrentValue;

    /// <summary>The extensions taken together, built again only when <see cref="Extensions"/> changes.</summary>
    internal HtmlEditorExtensionSet ExtensionSet => _extensionSet;

    /// <summary>The editable surface of the visual face.</summary>
    internal ElementReference SurfaceElement => _surface;

    /// <summary>The text area of the source face.</summary>
    internal ElementReference SourceElement => _source;

    /// <summary>
    /// Whether the surface reports where the selection is: only when someone listens, through
    /// <see cref="SelectionChanged"/> or a command whose <see cref="OmniHtmlEditorCommand.Pressed"/> or
    /// <see cref="OmniHtmlEditorCommand.Enabled"/> depends on it.
    /// </summary>
    internal bool TracksSelection => SelectionChanged.HasDelegate
        || _extensionSet.TracksSelection
        || ArrangedCommands.Any(command => command.Pressed is not null || command.Enabled is not null);

    /// <summary>The HTML sanitised with the allow-list and the extensions' policies.</summary>
    internal string Clean(string? html) => OmniHtmlSanitizer.Sanitize(html, EffectivePolicy);

    internal string CleanPaste(string? html, string? text) => OmniHtmlSanitizer.SanitizePaste(html, text, EffectivePolicy);

    /// <summary>The toolbar before the tidying of separators: the editor's own, arranged by each extension.</summary>
    private IReadOnlyList<OmniHtmlEditorCommand> ArrangedCommands => _extensionSet.Arrange(Commands ?? OmniHtmlEditorCommands.Default);
    internal OmniHtmlEditorMode CurrentMode => _mode;
    internal string SurfaceId => Id ?? _generatedId;
    private string LinkInputId => SurfaceId + "-link";

    /// <summary>The visual face, created on first use once the script runtime is injected.</summary>
    private HtmlEditorVisualSurface Visual => _visual ??= new(this, JSRuntime);

    /// <summary>The proofreaders of the extensions as the surface script and the context menu use them.</summary>
    internal HtmlEditorProofreading Proofreading => _proofreading ??= new(this);

    internal void CancelProofreadingSuggestions() => _proofreading?.Cancel();

    /// <summary>The source face, created on first use once the script runtime is injected.</summary>
    private HtmlEditorSourceFace SourceFace => _sourceFace ??= new(this, JSRuntime);

    /// <summary>The toolbar cut at its separators into groups of commands (see <see cref="HtmlEditorToolbar.Groups"/>), the commands placed in the menu left out.</summary>
    private List<List<OmniHtmlEditorCommand>> ToolbarGroups => HtmlEditorToolbar.Groups(ArrangedCommands.Where(command => !HtmlEditorToolbar.InMenu(command)).ToList());

    /// <summary>The script's view of the toolbar: its tooltips and, under a row limit, what moves to the menu.</summary>
    private HtmlEditorToolbarFit ToolbarFit => _toolbarFit ??= new(this, JSRuntime);

    /// <summary>The commands of the "more" menu, in toolbar order: those placed there, then those the row limit moved.</summary>
    private IReadOnlyList<OmniHtmlEditorCommand> MenuCommands => ArrangedCommands
        .Where(command => command.Action != OmniHtmlEditorAction.Separator
            && (HtmlEditorToolbar.InMenu(command) || (_toolbarFit?.Overflowed.Contains(command.Name) ?? false)))
        .ToList();

    /// <summary>Whether the bar ends with the "more" menu: a row limit, or a command placed in the menu.</summary>
    private bool ShowsMoreMenu => ToolbarRows is not null || ArrangedCommands.Any(HtmlEditorToolbar.InMenu);

    /// <summary>The package tooltip of a toolbar control: its name, then the command's description on the next line.</summary>
    private string TipOf(OmniHtmlEditorCommand command) =>
        string.IsNullOrWhiteSpace(command.Description) ? LabelOf(command) : LabelOf(command) + "\n" + command.Description;

    /// <summary>
    /// Rebuilds the extension set when <see cref="Extensions"/> holds other instances, then checks the
    /// merged sanitiser policy, that every custom command has an <see cref="OmniHtmlEditorCommand.Execute"/>
    /// handler and that every shortcut names a command the editor holds.
    /// </summary>
    /// <exception cref="InvalidOperationException">A custom command has no handler, or a shortcut names an unknown command.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_extensionSet.Matches(Extensions))
        {
            _extensionSet = HtmlEditorExtensionSet.For(Extensions);
        }

        OmniHtmlSanitizer.Validate(EffectivePolicy);
        var toolbar = ArrangedCommands;
        var incomplete = toolbar.Concat(_extensionSet.ContextMenu)
            .FirstOrDefault(command => command.Action == OmniHtmlEditorAction.Custom && command.Execute is null);
        if (incomplete is not null)
        {
            throw new InvalidOperationException($"The custom command '{incomplete.Name}' of {nameof(OmniHtmlEditor)} has no {nameof(OmniHtmlEditorCommand.Execute)} handler.");
        }

        var unknown = _extensionSet.Shortcuts.FirstOrDefault(shortcut => _extensionSet.Find(shortcut.Command, toolbar) is null);
        if (unknown is not null)
        {
            throw new InvalidOperationException($"The shortcut '{unknown.Keys}' of {nameof(OmniHtmlEditor)} names the command '{unknown.Command}', which none of its toolbar, extensions or context menu holds.");
        }
    }

    /// <summary>
    /// Takes the first <see cref="Mode"/> as the face shown, then switches face whenever the parent
    /// changes it, without raising <see cref="ModeChanged"/>.
    /// </summary>
    /// <returns>A task that completes once the face is switched.</returns>
    protected override async Task OnParametersSetAsync()
    {
        if (!_modeInitialized)
        {
            _modeInitialized = true;
            _modeParameter = Mode;
            _mode = Mode;
            return;
        }

        if (_modeParameter != Mode)
        {
            _modeParameter = Mode;
            await SwitchModeAsync(Mode, notify: false);
        }
    }

    /// <summary>
    /// On the visual face, mounts the editing surface with the sanitised value, or pushes a changed value
    /// or changed options to it; then focuses the link field when asked and brings the context menu script
    /// in line with the menu's open state (placed at the pointer).
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the surface is up to date.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await Visual.SyncAsync();
        }

        await ToolbarFit.SyncAsync(_toolbar, ToolbarRows);

        if (_panels.FocusLink)
        {
            _panels.FocusLink = false;
            await _linkInput.FocusAsync();
        }

        if (_menu is not null)
        {
            await _menu.SyncAsync();
        }
    }

    internal Task DispatchAsync(Func<Task> work) => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        await work();
        StateHasChanged();
    });

    internal Task DispatchAsync(Action work) => DispatchAsync(() =>
    {
        work();
        return Task.CompletedTask;
    });

    /// <summary>Renders the editor again, from any thread.</summary>
    internal Task RenderAsync() => InvokeAsync(StateHasChanged);

    /// <summary>Renders the editor again, already on the dispatcher.</summary>
    internal void Rerender() => StateHasChanged();

    internal Task HandleVisualInputAsync(string html) => Visual.TakeInputAsync(html);

    internal void HandleVisualState(string state) => _selection = HtmlEditorFormatState.Parse(state);

    internal async Task HandleSelectionAsync(string json)
    {
        if (_mode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        _caret = OmniHtmlEditorSelection.Parse(json);
        await SelectionChanged.InvokeAsync(_caret);
        await _extensionSet.NotifySelectionAsync(_caret);
    }

    private Task HandleInputAsync(ChangeEventArgs args) => IsLocked
        ? Task.CompletedTask
        : CommitAsync(Clean(args.Value?.ToString()));

    private async Task RunAsync(OmniHtmlEditorCommand command)
    {
        if (IsDisabled(command))
        {
            return;
        }

        if (command.Action == OmniHtmlEditorAction.Custom)
        {
            await CaptureVisualAsync();
            await command.Execute!(new OmniHtmlEditorCommandContext(this, command));
            return;
        }

        await RunBuiltInAsync(command.Action, null);
    }

    private Task ChooseAsync(OmniHtmlEditorCommand command, ChangeEventArgs args)
    {
        _selectGeneration++;
        return RunBuiltInAsync(command.Action, args.Value?.ToString());
    }

    internal async Task RunBuiltInAsync(OmniHtmlEditorAction action, string? argument)
    {
        if (IsLocked)
        {
            return;
        }

        switch (action)
        {
            case OmniHtmlEditorAction.Custom:
            case OmniHtmlEditorAction.Separator:
                return;
            case OmniHtmlEditorAction.Undo:
                await UndoAsync();
                return;
            case OmniHtmlEditorAction.Redo:
                await RedoAsync();
                return;
            case OmniHtmlEditorAction.ToggleSource:
                await SwitchModeAsync(_mode == OmniHtmlEditorMode.Visual ? OmniHtmlEditorMode.Source : OmniHtmlEditorMode.Visual, notify: true);
                return;
            case OmniHtmlEditorAction.Link when argument is null:
                await OpenLinkAsync();
                return;
            case OmniHtmlEditorAction.ShowBlocks:
                _showBlocks = !_showBlocks;
                return;
            case OmniHtmlEditorAction.ChangeCase when argument is not ("upper" or "lower" or "title"):
                return;
            case OmniHtmlEditorAction.InsertSpecialCharacter when string.IsNullOrEmpty(argument):
                _panels.Open(HtmlEditorPanel.Characters);
                return;
            case OmniHtmlEditorAction.InsertSpecialCharacter:
                await InsertCharacterAsync(argument!);
                return;
            case OmniHtmlEditorAction.ImportTable:
                _panels.Open(HtmlEditorPanel.Table);
                return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await Visual.ExecuteAsync(HtmlEditorToolbar.ScriptName(action), argument);
        }
        else
        {
            await SourceFace.ExecuteAsync(action, argument);
        }
    }

    internal async Task InsertHtmlAsync(string html)
    {
        if (IsLocked || string.IsNullOrEmpty(html))
        {
            return;
        }

        var clean = Clean(html);
        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await Visual.ExecuteAsync("inserthtml", clean);
        }
        else
        {
            await SourceFace.WrapSelectionAsync(clean, string.Empty);
        }
    }

    internal async Task ReplaceHtmlAsync(string html)
    {
        if (IsLocked)
        {
            return;
        }

        await CaptureVisualAsync();
        await CommitAsync(Clean(html));
    }

    /// <summary>Takes a sanitised value as the new one, recorded as one undo step when it differs.</summary>
    internal Task CommitAsync(string value)
    {
        if (!string.Equals(CurrentValue, value, StringComparison.Ordinal))
        {
            _undo.Push(CurrentValue ?? string.Empty);
            _redo.Clear();
            CurrentValue = value;
        }
        return Task.CompletedTask;
    }

    internal async Task UndoAsync()
    {
        if (IsLocked)
        {
            return;
        }

        await CaptureVisualAsync();
        if (_undo.TryPop(out var previous))
        {
            _redo.Push(CurrentValue ?? string.Empty);
            CurrentValue = previous;
        }
    }

    internal async Task RedoAsync()
    {
        if (IsLocked)
        {
            return;
        }

        await CaptureVisualAsync();
        if (_redo.TryPop(out var next))
        {
            _undo.Push(CurrentValue ?? string.Empty);
            CurrentValue = next;
        }
    }

    /// <summary>
    /// Takes in what was typed in the visual surface and not yet reported, so a command, the history
    /// or a mode switch never acts on a value a quarter of a second old.
    /// </summary>
    private Task CaptureVisualAsync() => _visual is null ? Task.CompletedTask : _visual.CaptureAsync();

    /// <summary>
    /// Takes in a surface the host changed through its own script, outside a command (a click on
    /// an inline note, an accepted suggestion): the document is read, sanitised with the allow-list
    /// and the extensions' policies, committed as one undo step and raised through
    /// <c>ValueChanged</c> when it differs. What the sanitiser removed is also removed from the
    /// surface. Does nothing in the source face, while disabled, or before the surface is mounted.
    /// Safe to call from any thread: the work runs on the renderer's dispatcher.
    /// </summary>
    public Task CommitDomAsync() => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        await CommitSurfaceAsync();
        StateHasChanged();
    });

    /// <summary>The body of <see cref="CommitDomAsync"/>, already on the dispatcher (a command's context calls it).</summary>
    internal Task CommitSurfaceAsync() => _visual is null ? Task.CompletedTask : _visual.CommitSurfaceAsync();

    /// <summary>The surface element of the visual face, or null in the source face.</summary>
    internal ElementReference? SurfaceReference => _mode == OmniHtmlEditorMode.Visual && _visual is { Mounted: true } ? _surface : null;

    /// <summary>The value, once what the visual surface still holds back has been taken in.</summary>
    internal async Task<string> CaptureAsync()
    {
        await CaptureVisualAsync();
        return CurrentHtml;
    }

    /// <summary>
    /// The value as a complete HTML file, what was typed a moment ago included: the <c>lang</c> of the
    /// current UI culture, the <see cref="DocumentTitle"/> (or the first level-one heading), and the
    /// editor's classes turned into declarations, so the file reads the same anywhere.
    /// </summary>
    public async Task<string> ExportHtmlAsync()
    {
        var html = await CaptureAsync();
        var title = DocumentTitle ?? OmniHtmlText.FirstHeading(html) ?? Localize("DocumentEditorDefaultTitle");
        return OmniHtmlText.ToStandaloneDocument(html, title, CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>The value as plain text, what was typed a moment ago included.</summary>
    public async Task<string> ExportTextAsync() => OmniHtmlText.ToPlainText(await CaptureAsync());

    private async Task DownloadHtmlAsync() =>
        await DownloadAsync(".html", "text/html;charset=utf-8", await ExportHtmlAsync());

    private async Task DownloadTextAsync() =>
        await DownloadAsync(".txt", "text/plain;charset=utf-8", await ExportTextAsync());

    private async Task DownloadAsync(string extension, string mimeType, string content)
    {
        _downloadModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", DownloadModulePath);
        var name = string.IsNullOrWhiteSpace(FileName) ? "document" : FileName.Trim();
        await _downloadModule.InvokeVoidAsync("download", name + extension, mimeType, content);
    }

    internal Task<bool> ReplaceClosestAsync(string selector, string html) => Visual.ReplaceClosestAsync(selector, html);

    internal Task ReplaceActivatedAsync(string html) => Visual.ReplaceActivatedAsync(html);

    /// <summary>The first proposal of the extensions that suggest text, or null.</summary>
    internal async Task<string?> SuggestAsync(string textBeforeCaret)
    {
        if (IsLocked || _mode != OmniHtmlEditorMode.Visual || string.IsNullOrWhiteSpace(textBeforeCaret))
        {
            return null;
        }

        foreach (var extension in _extensionSet.Source.Where(extension => extension.SuggestsText))
        {
            var proposal = await extension.SuggestAsync(textBeforeCaret);
            if (!string.IsNullOrEmpty(proposal))
            {
                return proposal;
            }
        }

        return null;
    }

    internal Task SetActivatedTextAsync(string text) => Visual.SetActivatedTextAsync(text);

    internal Task<OmniHtmlCaretSplit> GetHtmlAroundCaretAsync() => Visual.GetHtmlAroundCaretAsync();

    internal Task<string> GetSelectedTextAsync() => Visual.GetSelectedTextAsync();

    internal Task InsertTextAsync(string text) => Visual.InsertTextAsync(text);

    internal async Task HandleShortcutAsync(int index)
    {
        if (index < 0 || index >= _extensionSet.Shortcuts.Count || _mode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        var command = _extensionSet.Find(_extensionSet.Shortcuts[index].Command, ArrangedCommands);
        if (command is not null)
        {
            await RunAsync(command);
        }
    }

    internal async Task HandleElementActivatedAsync(int index, string element, string text)
    {
        if (Disabled || index < 0 || index >= _extensionSet.InlineElements.Count || _mode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        var node = OmniHtmlEditorSelection.Parse($$"""{"ancestors":[{{element}}]}""").Ancestors.FirstOrDefault();
        if (node is null)
        {
            return;
        }

        await CaptureVisualAsync();
        await _extensionSet.InlineElements[index].Activate(new OmniHtmlEditorElementContext(this, node, text));
    }

    // ── Panels of the built-in special characters and table import (HtmlEditorPanels) ──

    private IEnumerable<int> VisibleCharacters => _panels.VisibleCharacters(key => Localize(key));

    private async Task InsertCharacterAsync(string character)
    {
        _panels.Close();
        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await Visual.ExecuteAsync("inserttext", character);
        }
        else
        {
            await SourceFace.WrapSelectionAsync(WebUtility.HtmlEncode(character), string.Empty);
        }
    }

    private string TableAccept => HtmlEditorPanels.TableAccept(_extensionSet.TableReaders);

    private Task ReadTableFileAsync(IReadOnlyList<IBrowserFile> files) => _panels.ReadTableFileAsync(files, _extensionSet.TableReaders);

    private async Task InsertTableAsync()
    {
        if (_panels.TakeTable() is { } html)
        {
            await InsertHtmlAsync(html);
        }
    }

    // ── Context menu (HtmlEditorContextMenu): opened at the pointer over the surface and drawn in place ──

    private HtmlEditorContextMenu Menu => _menu ??= new(this, JSRuntime, () => Localize("HtmlEditorContextMenu"));

    private bool IsMenuOpen => _menu is { IsOpen: true };

    /// <summary>The <c>role="menu"</c> list around the given rows, drawn by the shared engine.</summary>
    private RenderFragment<RenderFragment> ContextMenuList => Menu.List;

    /// <summary>
    /// Opens the context menu: on a passage a proofreader flagged, its corrections and actions come first and are asked
    /// for once the menu shows; elsewhere, the extensions' entries alone, and no menu when they have none.
    /// </summary>
    internal async Task HandleContextMenuAsync(double x, double y, string? selection, string? issue = null)
    {
        var flagged = _mode == OmniHtmlEditorMode.Visual && Proofreading.Begin(issue);
        if ((_extensionSet.ContextMenu.Count == 0 && !flagged) || _mode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        if (selection is not null)
        {
            _caret = OmniHtmlEditorSelection.Parse(selection);
        }

        await Menu.OpenAsync(x, y);
        if (flagged)
        {
            await Proofreading.LoadSuggestionsAsync();
            // The corrections came after the menu was placed with one row: placed again, it stays in the window.
            Menu.Replace();
        }
    }

    // The entries of a flagged passage: the item has closed the menu and given the focus back to the surface.
    private async Task ApplyCorrectionAsync(string text)
    {
        Proofreading.Forget();
        await Visual.ProofreadReplaceAsync(text);
    }

    private async Task IgnoreIssueAsync()
    {
        Proofreading.Forget();
        await Visual.ProofreadIgnoreAsync();
    }

    private async Task RecordIssueAsync(bool dictionary)
    {
        await Proofreading.RecordAsync(dictionary);
        await Visual.ProofreadRecheckAsync();
    }

    /// <summary>Closes the context menu; the focus goes back to the surface with <paramref name="restoreFocus"/>.</summary>
    internal Task CloseMenuAsync(bool restoreFocus) => _menu is null ? Task.CompletedTask : _menu.CloseAsync(restoreFocus);

    /// <summary>
    /// Runs an entry of the context menu where the menu was opened. The item has already closed the menu
    /// and given the focus back to the surface; the selection the menu opened on is put back first.
    /// </summary>
    private async Task RunFromMenuAsync(OmniHtmlEditorCommand command)
    {
        if (_visual is not null)
        {
            await _visual.RestoreMenuSelectionAsync();
        }

        await RunAsync(command);
    }

    private async Task SwitchModeAsync(OmniHtmlEditorMode target, bool notify)
    {
        if (_mode == target)
        {
            return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            // The context menu belongs to the visual surface: it must not come back with it.
            await CloseMenuAsync(restoreFocus: false);
            await CaptureVisualAsync();
            if (_visual is not null)
            {
                await _visual.UnmountAsync();
            }
        }

        _mode = target;
        _selection = HtmlEditorFormatState.Empty;
        _caret = null;
        _panels.HideLink();
        // The parameter is only followed when the parent changes it: a parent that re-renders
        // without binding Mode must not switch the face back.
        if (notify)
        {
            await ModeChanged.InvokeAsync(target);
        }
    }

    internal Task OpenLinkAsync()
    {
        if (!IsLocked)
        {
            _panels.OpenLink();
        }

        return Task.CompletedTask;
    }

    private async Task ApplyLinkAsync()
    {
        var url = _panels.LinkUrl.Trim();
        if (url.Length == 0)
        {
            _panels.CloseLink();
            return;
        }

        if (_panels.SafeLinkUrl(url) is not { } safe)
        {
            return;
        }

        _panels.HideLink();
        await RunBuiltInAsync(OmniHtmlEditorAction.Link, safe);
    }

    private Task CloseLinkAsync()
    {
        _panels.CloseLink();
        return Task.CompletedTask;
    }

    private Task HandleLinkKeyAsync(KeyboardEventArgs args) => args.Key switch
    {
        "Enter" => ApplyLinkAsync(),
        "Escape" => CloseLinkAsync(),
        _ => Task.CompletedTask
    };

    private bool IsDisabled(OmniHtmlEditorCommand command) =>
        HtmlEditorToolbar.IsDisabled(command, IsLocked, _mode, _caret, _selection, _undo.Count > 0, _redo.Count > 0);

    private string LabelOf(OmniHtmlEditorCommand command) =>
        !string.IsNullOrWhiteSpace(command.Label) ? command.Label! : Localize(HtmlEditorToolbar.LabelKey(command.Action));

    private static OmniIconName? IconOf(OmniHtmlEditorCommand command) => HtmlEditorToolbar.IconOf(command);

    /// <summary>The pressed state of a toggle, from the formatting at the caret; null for a plain action.</summary>
    private string? PressedOf(OmniHtmlEditorCommand command) => HtmlEditorToolbar.PressedOf(command, _mode, _caret, _selection, _showBlocks);

    /// <summary>Takes the text as the value once sanitised with the editor's policy; parsing never fails.</summary>
    /// <param name="value">The HTML to parse.</param>
    /// <param name="result">The sanitised HTML.</param>
    /// <param name="validationErrorMessage">Always null.</param>
    /// <returns>Always true.</returns>
    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = Clean(value);
        validationErrorMessage = null!;
        return true;
    }

    /// <summary>
    /// Releases the form subscription, disposes the visual surface, detaches the script from a context
    /// menu still open, and releases the script modules and the script's reference to the component; a
    /// lost circuit is ignored.
    /// </summary>
    /// <returns>A task that completes once everything is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
        if (_visual is not null)
        {
            // Disposes the surface and its module (a lost circuit ignored), then the script's reference.
            await _visual.DisposeAsync();
        }

        if (_menu is not null)
        {
            // Detaches the script from a menu still open and releases its module; a lost circuit is ignored there.
            await _menu.DisposeAsync();
        }

        _proofreading?.Dispose();

        if (_toolbarFit is not null)
        {
            // Stops the row limit and releases this editor's share of the tooltips; a lost circuit is ignored there.
            await _toolbarFit.DisposeAsync();
        }

        if (_downloadModule is not null)
        {
            try
            {
                await _downloadModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the module with it.
            }
        }

        GC.SuppressFinalize(this);
    }
}
