using System.Net;
using System.Text.Json;
using OmniEurope.Blazor.Internal;

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
    private const string InteropModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";
    private const string VisualModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";
    private const string DownloadModulePath = "./_content/OmniEurope.Blazor/omni-document-editor.js";

    private static readonly (string Value, string Key)[] BlockFormats =
    [
        ("p", "HtmlEditorParagraph"), ("h1", "HtmlEditorHeading1"), ("h2", "HtmlEditorHeading2"),
        ("h3", "HtmlEditorHeading3"), ("h4", "HtmlEditorHeading4")
    ];

    private static readonly (string Value, string Key)[] FontSizes =
    [
        ("small", "HtmlEditorFontSizeSmall"), ("normal", "HtmlEditorFontSizeNormal"),
        ("large", "HtmlEditorFontSizeLarge"), ("xlarge", "HtmlEditorFontSizeXLarge")
    ];

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly string _generatedId = $"omni-html-editor-{Guid.NewGuid():N}";
    private ElementReference _source;
    private ElementReference _surface;
    private ElementReference _linkInput;
    private IJSObjectReference? _visualModule;
    private DotNetObjectReference<HtmlEditorInteropBridge>? _bridge;
    private OmniHtmlEditorMode _mode;
    private bool _modeInitialized;
    private OmniHtmlEditorMode _modeParameter;
    private bool _mounted;
    private string? _visualValue;
    private int _mountedRows;
    private OmniHtmlSanitizerPolicy? _mountedPolicy;
    private bool _mountedTracking;
    private HtmlEditorExtensionSet _extensionSet = HtmlEditorExtensionSet.Empty;
    private HtmlEditorExtensionSet? _mountedSet;
    private OmniHtmlEditorSelection? _caret;
    private SelectionState _selection = SelectionState.Empty;
    private int _selectGeneration;
    private bool _linkOpen;
    private bool _focusLink;
    private string _linkUrl = string.Empty;
    private string? _linkError;
    private bool _disposed;
    private IJSObjectReference? _downloadModule;
    private string? _countedValue;
    private bool _counted;
    private (int Words, int Characters) _statistics;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// The accessible name of the editor. Empty uses the localized "HTML editor", or "word processor"
    /// when <see cref="Sheet"/> is set.
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize(Sheet ? "DocumentEditorLabel" : "HtmlEditorLabel")
        : Label;

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

    private string FrameClass => Sheet ? "omni-document-editor omni-document-editor--sheet" : "omni-document-editor";

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
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the source face shows a sanitised preview under the textarea. The visual face is its own preview.</summary>
    [Parameter] public bool ShowPreview { get; set; } = true;

    [Parameter] public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// The toolbar, in order. Null uses <see cref="OmniHtmlEditorCommands.Default"/>; an application
    /// adds or reorders commands by passing its own list, typically built from that one.
    /// </summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorCommand>? Commands { get; set; }

    /// <summary>The face shown. The editor's own source button changes it and raises <see cref="ModeChanged"/>.</summary>
    [Parameter] public OmniHtmlEditorMode Mode { get; set; }
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

    /// <summary>
    /// What the surface script needs besides the rows: the classes it keeps while tidying (null
    /// for any), and whether a policy is in force, in which case a span carrying an allowed
    /// attribute is kept rather than unwrapped.
    /// </summary>
    private Dictionary<string, object?> SurfaceOptions
    {
        get
        {
            var options = new Dictionary<string, object?>(StringComparer.Ordinal) { ["rows"] = Rows };
            if (EffectivePolicy is not null)
            {
                options["policy"] = true;
                options["classes"] = OmniHtmlSanitizer.ClassesOf(EffectivePolicy);
            }

            if (_extensionSet.ShortcutKeys.Count > 0)
            {
                options["shortcuts"] = _extensionSet.ShortcutKeys;
            }

            if (_extensionSet.InlineElements.Count > 0)
            {
                options["inline"] = _extensionSet.InlineElements.Select(element => element.Selector).ToArray();
            }

            if (_extensionSet.ContextMenu.Count > 0)
            {
                options["menu"] = true;
            }

            if (_extensionSet.Source.Any(extension => extension.SuggestsText))
            {
                options["suggest"] = true;
            }

            if (TracksSelection)
            {
                options["selection"] = true;
            }

            return options;
        }
    }

    /// <summary>
    /// Whether the surface reports where the selection is: only when someone listens, through
    /// <see cref="SelectionChanged"/> or a command whose <see cref="OmniHtmlEditorCommand.Pressed"/> or
    /// <see cref="OmniHtmlEditorCommand.Enabled"/> depends on it.
    /// </summary>
    private bool TracksSelection => SelectionChanged.HasDelegate
        || _extensionSet.TracksSelection
        || ArrangedCommands.Any(command => command.Pressed is not null || command.Enabled is not null);

    private string Clean(string? html) => OmniHtmlSanitizer.Sanitize(html, EffectivePolicy);

    internal string CleanPaste(string? html, string? text) => OmniHtmlSanitizer.SanitizePaste(html, text, EffectivePolicy);

    /// <summary>The toolbar before the tidying of separators: the editor's own, arranged by each extension.</summary>
    private IReadOnlyList<OmniHtmlEditorCommand> ArrangedCommands => _extensionSet.Arrange(Commands ?? OmniHtmlEditorCommands.Default);
    internal OmniHtmlEditorMode CurrentMode => _mode;
    private string SurfaceId => Id ?? _generatedId;
    private string LinkInputId => SurfaceId + "-link";

    /// <summary>
    /// The toolbar cut at its separators into groups of commands, none empty: a separator at either end
    /// or next to another one draws nothing. Each group after the first is drawn with its separator
    /// before it, so a separator always stays with the group it opens and never ends a row of a wrapped
    /// toolbar; the one that would start a row falls outside the toolbar and is clipped.
    /// </summary>
    private List<List<OmniHtmlEditorCommand>> ToolbarGroups
    {
        get
        {
            var groups = new List<List<OmniHtmlEditorCommand>>();
            var current = new List<OmniHtmlEditorCommand>();
            foreach (var command in ArrangedCommands)
            {
                if (command.Action != OmniHtmlEditorAction.Separator)
                {
                    current.Add(command);
                }
                else if (current.Count > 0)
                {
                    groups.Add(current);
                    current = [];
                }
            }

            if (current.Count > 0)
            {
                groups.Add(current);
            }

            return groups;
        }
    }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            if (!_mounted)
            {
                // Claimed before the first await: a render arriving while the module loads must not
                // mount the surface a second time.
                _mounted = true;
                _mountedRows = Rows;
                _visualModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", VisualModulePath);
                _bridge ??= DotNetObjectReference.Create(new HtmlEditorInteropBridge(this));
                _visualValue = CurrentValue;
                _mountedPolicy = EffectivePolicy;
                _mountedTracking = TracksSelection;
                _mountedSet = _extensionSet;
                await _visualModule.InvokeVoidAsync("mount", _surface, _bridge, Clean(CurrentValue), SurfaceOptions);
            }
            else if (_visualModule is not null)
            {
                if (!string.Equals(CurrentValue, _visualValue, StringComparison.Ordinal))
                {
                    _visualValue = CurrentValue;
                    await _visualModule!.InvokeVoidAsync("setHtml", _surface, Clean(CurrentValue));
                }

                if (_mountedRows != Rows || !ReferenceEquals(_mountedPolicy, EffectivePolicy) || _mountedTracking != TracksSelection
                    || !ReferenceEquals(_mountedSet, _extensionSet))
                {
                    _mountedRows = Rows;
                    _mountedPolicy = EffectivePolicy;
                    _mountedTracking = TracksSelection;
                    _mountedSet = _extensionSet;
                    await _visualModule!.InvokeVoidAsync("configure", _surface, SurfaceOptions);
                }
            }
        }

        if (_focusLink)
        {
            _focusLink = false;
            await _linkInput.FocusAsync();
        }

        if (_menuOpen && !_menuPlaced)
        {
            await PlaceMenuAsync();
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

    internal Task HandleVisualInputAsync(string html)
    {
        if (Disabled || _mode != OmniHtmlEditorMode.Visual)
        {
            return Task.CompletedTask;
        }

        var clean = Clean(html);
        _visualValue = clean;
        return CommitAsync(clean);
    }

    internal void HandleVisualState(string state) => _selection = SelectionState.Parse(state);

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

    private Task HandleInputAsync(ChangeEventArgs args) => Disabled
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
        if (Disabled)
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
                OpenPanel(EditorPanel.Characters);
                return;
            case OmniHtmlEditorAction.InsertSpecialCharacter:
                await InsertCharacterAsync(argument!);
                return;
            case OmniHtmlEditorAction.ImportTable:
                OpenPanel(EditorPanel.Table);
                return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await ExecuteVisualAsync(ScriptName(action), argument);
        }
        else
        {
            await ExecuteSourceAsync(action, argument);
        }
    }

    private async Task ExecuteVisualAsync(string action, string? argument)
    {
        if (!_mounted || _visualModule is null)
        {
            return;
        }

        var html = await _visualModule.InvokeAsync<string?>("exec", _surface, action, argument);
        if (html is not null)
        {
            var clean = Clean(html);
            _visualValue = clean;
            await CommitAsync(clean);
        }
    }

    private Task ExecuteSourceAsync(OmniHtmlEditorAction action, string? argument)
    {
        switch (action)
        {
            case OmniHtmlEditorAction.Bold: return WrapSelectionAsync("<strong>", "</strong>");
            case OmniHtmlEditorAction.Italic: return WrapSelectionAsync("<em>", "</em>");
            case OmniHtmlEditorAction.Underline: return WrapSelectionAsync("<u>", "</u>");
            case OmniHtmlEditorAction.Strikethrough: return WrapSelectionAsync("<s>", "</s>");
            case OmniHtmlEditorAction.Highlight: return WrapSelectionAsync("<mark>", "</mark>");
            case OmniHtmlEditorAction.Subscript: return WrapSelectionAsync("<sub>", "</sub>");
            case OmniHtmlEditorAction.Superscript: return WrapSelectionAsync("<sup>", "</sup>");
            case OmniHtmlEditorAction.InlineCode: return WrapSelectionAsync("<code>", "</code>");
            case OmniHtmlEditorAction.CodeBlock: return WrapSelectionAsync("<pre>", "</pre>");
            case OmniHtmlEditorAction.Quote:
            case OmniHtmlEditorAction.Indent: return WrapSelectionAsync("<blockquote>", "</blockquote>");
            case OmniHtmlEditorAction.Outdent: return ApplyAsync(value => value.StartsWith("<blockquote>", StringComparison.OrdinalIgnoreCase) && value.EndsWith("</blockquote>", StringComparison.OrdinalIgnoreCase) ? value[12..^13] : value);
            case OmniHtmlEditorAction.BulletList: return WrapSelectionAsync("<ul><li>", "</li></ul>");
            case OmniHtmlEditorAction.NumberedList: return WrapSelectionAsync("<ol><li>", "</li></ol>");
            case OmniHtmlEditorAction.BlockFormat:
                var tag = BlockFormats.Any(format => format.Value == argument) ? argument! : "p";
                return WrapSelectionAsync($"<{tag}>", $"</{tag}>");
            case OmniHtmlEditorAction.FontSize:
                return argument is "small" or "large" or "xlarge"
                    ? WrapSelectionAsync($"<span class=\"omni-font-size-{argument}\">", "</span>")
                    : Task.CompletedTask;
            case OmniHtmlEditorAction.AlignLeft: return WrapSelectionAsync("<p>", "</p>");
            case OmniHtmlEditorAction.AlignCenter: return WrapSelectionAsync("<p class=\"omni-align-center\">", "</p>");
            case OmniHtmlEditorAction.AlignRight: return WrapSelectionAsync("<p class=\"omni-align-right\">", "</p>");
            case OmniHtmlEditorAction.AlignJustify: return WrapSelectionAsync("<p class=\"omni-align-justify\">", "</p>");
            case OmniHtmlEditorAction.InsertTable: return WrapSelectionAsync(TableSource, string.Empty);
            case OmniHtmlEditorAction.Link when !string.IsNullOrEmpty(argument):
                return WrapSelectionAsync($"<a href=\"{WebUtility.HtmlEncode(argument)}\">", "</a>");
            default:
                return Task.CompletedTask;
        }
    }

    private const string TableSource =
        "<table><tbody><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr></tbody></table>";

    private async Task WrapSelectionAsync(string prefix, string suffix)
    {
        if (Disabled)
        {
            return;
        }

        await using var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
        var result = await module.InvokeAsync<JsonElement>("wrapTextSelection", _source, prefix, suffix);
        var value = result.GetProperty("value").GetString() ?? string.Empty;
        var start = result.GetProperty("selectionStart").GetInt32();
        var end = result.GetProperty("selectionEnd").GetInt32();
        await CommitAsync(Clean(value));
        await InvokeAsync(StateHasChanged);
        await module.InvokeVoidAsync("restoreTextSelection", _source, start, end);
    }

    private Task ApplyAsync(Func<string, string> transform) => Disabled
        ? Task.CompletedTask
        : CommitAsync(Clean(transform(CurrentValue ?? string.Empty)));

    internal async Task InsertHtmlAsync(string html)
    {
        if (Disabled || string.IsNullOrEmpty(html))
        {
            return;
        }

        var clean = Clean(html);
        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await ExecuteVisualAsync("inserthtml", clean);
        }
        else
        {
            await WrapSelectionAsync(clean, string.Empty);
        }
    }

    internal async Task ReplaceHtmlAsync(string html)
    {
        if (Disabled)
        {
            return;
        }

        await CaptureVisualAsync();
        await CommitAsync(Clean(html));
    }

    private Task CommitAsync(string value)
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
        if (Disabled)
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
        if (Disabled)
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
    private async Task CaptureVisualAsync()
    {
        if (_mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return;
        }

        var html = await _visualModule.InvokeAsync<string?>("read", _surface);
        if (html is not null)
        {
            var clean = Clean(html);
            _visualValue = clean;
            await CommitAsync(clean);
        }
    }

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
    internal async Task CommitSurfaceAsync()
    {
        if (Disabled || _mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return;
        }

        var html = await _visualModule.InvokeAsync<string?>("read", _surface);
        if (html is null)
        {
            return;
        }

        var clean = Clean(html);
        // A surface that differs from its sanitised value is redrawn after the next render.
        _visualValue = html;
        await CommitAsync(clean);
        if (!string.Equals(clean, html, StringComparison.Ordinal))
        {
            StateHasChanged();
        }
    }

    /// <summary>The surface element of the visual face, or null in the source face.</summary>
    internal ElementReference? SurfaceReference => _mode == OmniHtmlEditorMode.Visual && _mounted ? _surface : null;

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

    internal async Task<bool> ReplaceClosestAsync(string selector, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        if (Disabled || _mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return false;
        }

        await CaptureVisualAsync();
        var result = await _visualModule.InvokeAsync<string?>("replaceClosest", _surface, selector, Clean(html));
        if (result is null)
        {
            return false;
        }

        await CommitVisualResultAsync(result);
        return true;
    }

    internal async Task ReplaceActivatedAsync(string html)
    {
        if (Disabled || _mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return;
        }

        await CaptureVisualAsync();
        var result = await _visualModule.InvokeAsync<string?>("replaceActivated", _surface, Clean(html));
        if (result is not null)
        {
            await CommitVisualResultAsync(result);
        }
    }

    /// <summary>The first proposal of the extensions that suggest text, or null.</summary>
    internal async Task<string?> SuggestAsync(string textBeforeCaret)
    {
        if (Disabled || _mode != OmniHtmlEditorMode.Visual || string.IsNullOrWhiteSpace(textBeforeCaret))
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

    internal async Task SetActivatedTextAsync(string text)
    {
        if (Disabled || _mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return;
        }

        await CaptureVisualAsync();
        var result = await _visualModule.InvokeAsync<string?>("setActivatedText", _surface, text ?? string.Empty);
        if (result is not null)
        {
            await CommitVisualResultAsync(result);
        }
    }

    internal async Task<OmniHtmlCaretSplit> GetHtmlAroundCaretAsync()
    {
        if (_mode != OmniHtmlEditorMode.Visual || !_mounted || _visualModule is null)
        {
            return new(CurrentValue ?? string.Empty, string.Empty);
        }

        var json = await _visualModule.InvokeAsync<string?>("aroundCaret", _surface);
        if (string.IsNullOrEmpty(json))
        {
            return new(CurrentValue ?? string.Empty, string.Empty);
        }

        using var parts = JsonDocument.Parse(json);
        return new(Clean(parts.RootElement.GetProperty("before").GetString()), Clean(parts.RootElement.GetProperty("after").GetString()));
    }

    internal async Task<string> GetSelectedTextAsync() =>
        _mode == OmniHtmlEditorMode.Visual && _mounted && _visualModule is not null
            ? await _visualModule.InvokeAsync<string?>("selectedText", _surface) ?? string.Empty
            : string.Empty;

    internal Task InsertTextAsync(string text) =>
        string.IsNullOrEmpty(text) || _mode != OmniHtmlEditorMode.Visual
            ? Task.CompletedTask
            : ExecuteVisualAsync("inserttext", text);

    private Task CommitVisualResultAsync(string html)
    {
        var clean = Clean(html);
        _visualValue = clean;
        return CommitAsync(clean);
    }

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

    // ── Panels of the built-in special characters and table import ──

    private enum EditorPanel { None, Characters, Table }

    private EditorPanel _panel;
    private bool _showBlocks;
    private string _charCategory = HtmlEditorCharacters.DefaultCategory;
    private string _charSearch = string.Empty;
    private IReadOnlyList<IReadOnlyList<string>>? _tableRows;
    private bool _tableHeader;
    private string? _tableError;
    private bool _tableBusy;

    private void OpenPanel(EditorPanel panel)
    {
        _linkOpen = false;
        _panel = panel;
        _charSearch = string.Empty;
        _tableRows = null;
        _tableError = null;
    }

    private void ClosePanel() => _panel = EditorPanel.None;

    private IEnumerable<int> VisibleCharacters
    {
        get
        {
            var search = _charSearch.Trim();
            if (search.Length == 0)
            {
                return HtmlEditorCharacters.All.Where(entry => entry.Category == _charCategory).Select(entry => entry.CodePoint);
            }

            return HtmlEditorCharacters.All.Select(entry => entry.CodePoint).Distinct().Where(codePoint =>
                HtmlEditorCharacters.Text(codePoint).Contains(search, StringComparison.Ordinal)
                || Localize(HtmlEditorCharacters.NameKey(codePoint)).Contains(search, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    private async Task InsertCharacterAsync(string character)
    {
        _panel = EditorPanel.None;
        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await ExecuteVisualAsync("inserttext", character);
        }
        else
        {
            await WrapSelectionAsync(WebUtility.HtmlEncode(character), string.Empty);
        }
    }

    private string TableAccept => string.Join(',', HtmlEditorTableFile.OwnExtensions
        .Concat(_extensionSet.TableReaders.SelectMany(reader => reader.Extensions))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    private async Task ReadTableFileAsync(IReadOnlyList<IBrowserFile> files)
    {
        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        _tableRows = null;
        _tableError = null;
        _tableBusy = true;
        try
        {
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            await using var upload = file.OpenReadStream(HtmlEditorTableFile.MaxBytes);
            using var content = new MemoryStream();
            await upload.CopyToAsync(content);
            content.Position = 0;
            IReadOnlyList<IReadOnlyList<string>> rows;
            if (extension is ".csv" or ".tsv")
            {
                using var reader = new StreamReader(content, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                rows = HtmlEditorTableFile.ReadDelimited(await reader.ReadToEndAsync(), extension == ".tsv" ? '\t' : ',');
            }
            else if (_extensionSet.TableReaders.FirstOrDefault(candidate => candidate.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) is { } tableReader)
            {
                rows = HtmlEditorTableFile.Shape(await tableReader.Read(file.Name, content));
            }
            else
            {
                _tableError = Localize("HtmlEditorImportTableUnsupported");
                return;
            }

            if (rows.Count == 0)
            {
                _tableError = Localize("HtmlEditorImportTableEmpty");
                return;
            }

            _tableRows = rows;
            // A first row with no number in it reads as a header, as a spreadsheet's usually does.
            _tableHeader = rows.Count > 1 && rows[0].All(cell => !double.TryParse(cell, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _));
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or HttpRequestException or FormatException or NotSupportedException)
        {
            _tableError = Localize("HtmlEditorImportTableFailed");
        }
        finally
        {
            _tableBusy = false;
        }
    }

    private async Task InsertTableAsync()
    {
        if (_tableRows is null)
        {
            return;
        }

        var html = HtmlEditorTableFile.ToHtml(_tableRows, _tableHeader);
        _panel = EditorPanel.None;
        _tableRows = null;
        await InsertHtmlAsync(html);
    }

    // ── Context menu: the popup and its placement are those of OmniContextMenu (omni-focus.js) ──

    private const string FocusModulePath = "./_content/OmniEurope.Blazor/omni-focus.js";
    private bool _menuOpen;
    private bool _menuPlaced;
    private double _menuX;
    private double _menuY;
    private IJSObjectReference? _focusModule;
    private DotNetObjectReference<HtmlEditorMenuDismissBridge>? _menuDismiss;
    private string MenuId => SurfaceId + "-menu";
    private string MenuKey => SurfaceId + "-menu-focus";

    internal void HandleContextMenu(double x, double y, string? selection)
    {
        if (_extensionSet.ContextMenu.Count == 0 || _mode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        if (selection is not null)
        {
            _caret = OmniHtmlEditorSelection.Parse(selection);
        }

        _menuX = x;
        _menuY = y;
        _menuOpen = true;
        _menuPlaced = false;
    }

    private async Task PlaceMenuAsync()
    {
        _menuPlaced = true;
        _focusModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
        _menuDismiss ??= DotNetObjectReference.Create(new HtmlEditorMenuDismissBridge(this));
        await _focusModule.InvokeVoidAsync("openContextMenu", MenuId, MenuKey, _surface, _menuX, _menuY, _menuDismiss);
    }

    internal async Task CloseMenuAsync()
    {
        if (!_menuOpen)
        {
            return;
        }

        _menuOpen = false;
        _menuPlaced = false;
        if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("closeContextMenu", MenuKey);
        }
    }

    /// <summary>
    /// Runs an entry of the context menu where the menu was opened, then lets the menu go. The command does
    /// not wait for the focus to come back: that waits for a frame, which a page not being drawn never has.
    /// </summary>
    private async Task RunFromMenuAsync(OmniHtmlEditorCommand command)
    {
        _menuOpen = false;
        _menuPlaced = false;
        if (_mounted && _visualModule is not null)
        {
            await _visualModule.InvokeVoidAsync("restoreMenuSelection", _surface);
        }

        await RunAsync(command);
        if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("closeContextMenu", MenuKey);
        }
    }

    private async Task HandleMenuKeyAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            await CloseMenuAsync();
        }
        else if (args.Key is "ArrowDown" or "ArrowUp" or "Home" or "End" && _focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("moveContextMenuFocus", MenuId, args.Key);
        }
    }

    private async Task SwitchModeAsync(OmniHtmlEditorMode target, bool notify)
    {
        if (_mode == target)
        {
            return;
        }

        if (_mode == OmniHtmlEditorMode.Visual)
        {
            await CaptureVisualAsync();
            await UnmountAsync();
        }

        _mode = target;
        _selection = SelectionState.Empty;
        _caret = null;
        _linkOpen = false;
        // The parameter is only followed when the parent changes it: a parent that re-renders
        // without binding Mode must not switch the face back.
        if (notify)
        {
            await ModeChanged.InvokeAsync(target);
        }
    }

    private async Task UnmountAsync()
    {
        if (_mounted && _visualModule is not null)
        {
            _mounted = false;
            await _visualModule.InvokeVoidAsync("dispose", _surface);
        }

        _mounted = false;
    }

    internal Task OpenLinkAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        _panel = EditorPanel.None;
        _linkOpen = true;
        _focusLink = true;
        _linkError = null;
        _linkUrl = string.Empty;
        return Task.CompletedTask;
    }

    private async Task ApplyLinkAsync()
    {
        var url = _linkUrl.Trim();
        if (url.Length == 0)
        {
            await CloseLinkAsync();
            return;
        }

        try
        {
            url = OmniUriPolicy.EnsureSafe(url, nameof(OmniHtmlEditorAction.Link))!;
        }
        catch (InvalidOperationException)
        {
            _linkError = Localize("HtmlEditorLinkInvalid");
            return;
        }

        _linkOpen = false;
        await RunBuiltInAsync(OmniHtmlEditorAction.Link, url);
    }

    private Task CloseLinkAsync()
    {
        _linkOpen = false;
        _linkError = null;
        return Task.CompletedTask;
    }

    private Task HandleLinkKeyAsync(KeyboardEventArgs args) => args.Key switch
    {
        "Enter" => ApplyLinkAsync(),
        "Escape" => CloseLinkAsync(),
        _ => Task.CompletedTask
    };

    private bool IsDisabled(OmniHtmlEditorCommand command) => Disabled
        || (command.Enabled is { } enabled && !enabled(_mode == OmniHtmlEditorMode.Visual ? _caret : null))
        || command.Action switch
    {
        OmniHtmlEditorAction.Undo => _undo.Count == 0,
        OmniHtmlEditorAction.Redo => _redo.Count == 0,
        OmniHtmlEditorAction.Unlink or OmniHtmlEditorAction.ClearFormatting
            or OmniHtmlEditorAction.ChangeCase or OmniHtmlEditorAction.ShowBlocks => _mode == OmniHtmlEditorMode.Source,
        OmniHtmlEditorAction.Cut or OmniHtmlEditorAction.Copy or OmniHtmlEditorAction.Paste
            or OmniHtmlEditorAction.InsertParagraph => _mode == OmniHtmlEditorMode.Source,
        _ when IsTableAction(command.Action) => _mode == OmniHtmlEditorMode.Source || !_selection.Marks.Contains("intable"),
        _ => false
    };

    private static bool IsTableAction(OmniHtmlEditorAction action) =>
        action is >= OmniHtmlEditorAction.AddRowAbove and <= OmniHtmlEditorAction.SetCellSpan;

    private string LabelOf(OmniHtmlEditorCommand command) =>
        !string.IsNullOrWhiteSpace(command.Label) ? command.Label! : Localize(command.Action switch
        {
            OmniHtmlEditorAction.Bold => "HtmlEditorBold",
            OmniHtmlEditorAction.Italic => "HtmlEditorItalic",
            OmniHtmlEditorAction.Underline => "HtmlEditorUnderline",
            OmniHtmlEditorAction.Strikethrough => "HtmlEditorStrikethrough",
            OmniHtmlEditorAction.Subscript => "HtmlEditorSubscript",
            OmniHtmlEditorAction.Superscript => "HtmlEditorSuperscript",
            OmniHtmlEditorAction.InlineCode => "HtmlEditorInlineCode",
            OmniHtmlEditorAction.BlockFormat => "HtmlEditorBlockFormat",
            OmniHtmlEditorAction.FontSize => "HtmlEditorFontSize",
            OmniHtmlEditorAction.BulletList => "HtmlEditorBulletList",
            OmniHtmlEditorAction.NumberedList => "HtmlEditorNumberedList",
            OmniHtmlEditorAction.Indent => "HtmlEditorIndent",
            OmniHtmlEditorAction.Outdent => "HtmlEditorOutdent",
            OmniHtmlEditorAction.Quote => "HtmlEditorQuote",
            OmniHtmlEditorAction.CodeBlock => "HtmlEditorCodeBlock",
            OmniHtmlEditorAction.Link => "HtmlEditorLink",
            OmniHtmlEditorAction.Unlink => "HtmlEditorUnlink",
            OmniHtmlEditorAction.AlignLeft => "HtmlEditorAlignLeft",
            OmniHtmlEditorAction.AlignCenter => "HtmlEditorAlignCenter",
            OmniHtmlEditorAction.AlignRight => "HtmlEditorAlignRight",
            OmniHtmlEditorAction.AlignJustify => "HtmlEditorAlignJustify",
            OmniHtmlEditorAction.InsertTable => "HtmlEditorInsertTable",
            OmniHtmlEditorAction.ClearFormatting => "HtmlEditorClearFormatting",
            OmniHtmlEditorAction.Undo => "HtmlEditorUndo",
            OmniHtmlEditorAction.Redo => "HtmlEditorRedo",
            OmniHtmlEditorAction.ToggleSource => "HtmlEditorSource",
            OmniHtmlEditorAction.Cut => "HtmlEditorCut",
            OmniHtmlEditorAction.Copy => "HtmlEditorCopy",
            OmniHtmlEditorAction.Paste => "HtmlEditorPaste",
            OmniHtmlEditorAction.InsertParagraph => "HtmlEditorInsertParagraph",
            OmniHtmlEditorAction.AddRowAbove => "HtmlEditorAddRowAbove",
            OmniHtmlEditorAction.AddRowBelow => "HtmlEditorAddRowBelow",
            OmniHtmlEditorAction.DeleteRow => "HtmlEditorDeleteRow",
            OmniHtmlEditorAction.AddColumnBefore => "HtmlEditorAddColumnBefore",
            OmniHtmlEditorAction.AddColumnAfter => "HtmlEditorAddColumnAfter",
            OmniHtmlEditorAction.DeleteColumn => "HtmlEditorDeleteColumn",
            OmniHtmlEditorAction.MergeCellRight => "HtmlEditorMergeCellRight",
            OmniHtmlEditorAction.MergeCellDown => "HtmlEditorMergeCellDown",
            OmniHtmlEditorAction.SplitCell => "HtmlEditorSplitCell",
            OmniHtmlEditorAction.SetCellSpan => "HtmlEditorSetCellSpan",
            OmniHtmlEditorAction.ChangeCase => "HtmlEditorChangeCase",
            OmniHtmlEditorAction.InsertSpecialCharacter => "HtmlEditorInsertSpecialCharacter",
            OmniHtmlEditorAction.ImportTable => "HtmlEditorImportTable",
            OmniHtmlEditorAction.ShowBlocks => "HtmlEditorShowBlocks",
            OmniHtmlEditorAction.Highlight => "HtmlEditorHighlight",
            _ => "HtmlEditorCustomCommand"
        });

    private static OmniIconName? IconOf(OmniHtmlEditorCommand command) => command.Icon ?? command.Action switch
    {
        OmniHtmlEditorAction.Bold => OmniIconName.TextB,
        OmniHtmlEditorAction.Italic => OmniIconName.TextItalic,
        OmniHtmlEditorAction.Underline => OmniIconName.TextUnderline,
        OmniHtmlEditorAction.Strikethrough => OmniIconName.TextStrikethrough,
        OmniHtmlEditorAction.Subscript => OmniIconName.TextSubscript,
        OmniHtmlEditorAction.Superscript => OmniIconName.TextSuperscript,
        OmniHtmlEditorAction.InlineCode => OmniIconName.Code,
        OmniHtmlEditorAction.BulletList => OmniIconName.ListBullets,
        OmniHtmlEditorAction.NumberedList => OmniIconName.NumberedList,
        OmniHtmlEditorAction.Indent => OmniIconName.TextIndent,
        OmniHtmlEditorAction.Outdent => OmniIconName.TextOutdent,
        OmniHtmlEditorAction.Quote => OmniIconName.Quotes,
        OmniHtmlEditorAction.CodeBlock => OmniIconName.CodeBlock,
        OmniHtmlEditorAction.Link => OmniIconName.Link,
        OmniHtmlEditorAction.Unlink => OmniIconName.LinkBreak,
        OmniHtmlEditorAction.AlignLeft => OmniIconName.TextAlignLeft,
        OmniHtmlEditorAction.AlignCenter => OmniIconName.TextAlignCenter,
        OmniHtmlEditorAction.AlignRight => OmniIconName.TextAlignRight,
        OmniHtmlEditorAction.AlignJustify => OmniIconName.TextAlignJustify,
        OmniHtmlEditorAction.InsertTable => OmniIconName.Table,
        OmniHtmlEditorAction.ClearFormatting => OmniIconName.Eraser,
        OmniHtmlEditorAction.Undo => OmniIconName.ArrowUUpLeft,
        OmniHtmlEditorAction.Redo => OmniIconName.ArrowUUpRight,
        OmniHtmlEditorAction.ToggleSource => OmniIconName.FileHtml,
        OmniHtmlEditorAction.Copy => OmniIconName.Copy,
        OmniHtmlEditorAction.InsertSpecialCharacter => OmniIconName.Smiley,
        OmniHtmlEditorAction.ImportTable => OmniIconName.Upload,
        OmniHtmlEditorAction.ShowBlocks => OmniIconName.Rows,
        OmniHtmlEditorAction.Highlight => OmniIconName.Highlighter,
        _ => null
    };

    /// <summary>The pressed state of a toggle, from the formatting at the caret; null for a plain action.</summary>
    private string? PressedOf(OmniHtmlEditorCommand command)
    {
        if (command.Pressed is { } pressed)
        {
            return pressed(_mode == OmniHtmlEditorMode.Visual ? _caret : null) ? "true" : "false";
        }

        if (command.Action == OmniHtmlEditorAction.ToggleSource)
        {
            return _mode == OmniHtmlEditorMode.Source ? "true" : "false";
        }

        if (command.Action == OmniHtmlEditorAction.ShowBlocks)
        {
            return _showBlocks ? "true" : "false";
        }

        if (_mode != OmniHtmlEditorMode.Visual)
        {
            return null;
        }

        return command.Action switch
        {
            OmniHtmlEditorAction.AlignLeft => Pressed(_selection.Align == "left"),
            OmniHtmlEditorAction.AlignCenter => Pressed(_selection.Align == "center"),
            OmniHtmlEditorAction.AlignRight => Pressed(_selection.Align == "right"),
            OmniHtmlEditorAction.AlignJustify => Pressed(_selection.Align == "justify"),
            OmniHtmlEditorAction.Bold or OmniHtmlEditorAction.Italic or OmniHtmlEditorAction.Underline
                or OmniHtmlEditorAction.Strikethrough or OmniHtmlEditorAction.Subscript or OmniHtmlEditorAction.Superscript
                or OmniHtmlEditorAction.InlineCode or OmniHtmlEditorAction.BulletList or OmniHtmlEditorAction.NumberedList
                or OmniHtmlEditorAction.Quote or OmniHtmlEditorAction.CodeBlock or OmniHtmlEditorAction.Link
                or OmniHtmlEditorAction.Highlight
                => Pressed(_selection.Marks.Contains(ScriptName(command.Action))),
            _ => null
        };

        static string Pressed(bool value) => value ? "true" : "false";
    }

    private static string ScriptName(OmniHtmlEditorAction action) => action.ToString().ToLowerInvariant();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = Clean(value);
        validationErrorMessage = null!;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
        if (_visualModule is not null)
        {
            try
            {
                if (_mounted)
                {
                    await _visualModule.InvokeVoidAsync("dispose", _surface);
                }

                await _visualModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the listeners with it.
            }
        }

        if (_focusModule is not null)
        {
            try
            {
                if (_menuOpen)
                {
                    await _focusModule.InvokeVoidAsync("closeContextMenu", MenuKey);
                }

                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the menu listener with it.
            }
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

        _menuDismiss?.Dispose();
        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>The formatting at the caret, as reported by the visual surface.</summary>
    private sealed record SelectionState(IReadOnlySet<string> Marks, string Block, string Align, string Size)
    {
        public static SelectionState Empty { get; } = new(new HashSet<string>(StringComparer.Ordinal), "p", "left", "normal");

        public static SelectionState Parse(string? state)
        {
            var parts = (state ?? string.Empty).Split('|');
            if (parts.Length != 4)
            {
                return Empty;
            }

            return new(
                parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal),
                parts[1].Length == 0 ? "p" : parts[1],
                parts[2].Length == 0 ? "left" : parts[2],
                parts[3].Length == 0 ? "normal" : parts[3]);
        }
    }
}
