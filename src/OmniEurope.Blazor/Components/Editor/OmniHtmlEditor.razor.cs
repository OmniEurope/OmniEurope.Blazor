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
/// textarea and the tag wrapping this component always had.
/// </remarks>
public partial class OmniHtmlEditor
{
    private const string InteropModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";
    private const string VisualModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

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
    private OmniHtmlEditorSelection? _caret;
    private SelectionState _selection = SelectionState.Empty;
    private int _selectGeneration;
    private bool _linkOpen;
    private bool _focusLink;
    private string _linkUrl = string.Empty;
    private string? _linkError;
    private bool _disposed;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("HtmlEditorLabel")
        : Label;

    /// <summary>The height of the source textarea in rows, and the minimum height of the visual surface.</summary>
    [Parameter] public int Rows { get; set; } = 12;
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the source face shows a sanitised preview under the textarea. The visual face is its own preview.</summary>
    [Parameter] public bool ShowPreview { get; set; } = true;

    // The six switches below predate Commands. They still hide their command from whichever toolbar
    // is in use, so an application written against them keeps its toolbar.
    [Parameter] public bool EnableBold { get; set; } = true;
    [Parameter] public bool EnableItalic { get; set; } = true;
    [Parameter] public bool EnableSubscript { get; set; } = true;
    [Parameter] public bool EnableSuperscript { get; set; } = true;
    [Parameter] public bool EnableIndent { get; set; } = true;
    [Parameter] public bool EnableOutdent { get; set; } = true;
    [Parameter] public string? AriaDescribedBy { get; set; }

    /// <summary>Whole-value transforms shown after the toolbar, as text buttons. <see cref="Commands"/> is the richer successor.</summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorTool> CustomTools { get; set; } = Array.Empty<OmniHtmlEditorTool>();

    /// <summary>
    /// The toolbar, in order. Null uses <see cref="OmniHtmlEditorCommands.Default"/>; an application
    /// adds or reorders commands by passing its own list, typically built from that one.
    /// </summary>
    [Parameter] public IReadOnlyList<OmniHtmlEditorCommand>? Commands { get; set; }

    /// <summary>The face shown. The editor's own source button changes it and raises <see cref="ModeChanged"/>.</summary>
    [Parameter] public OmniHtmlEditorMode Mode { get; set; }
    [Parameter] public EventCallback<OmniHtmlEditorMode> ModeChanged { get; set; }

    /// <summary>
    /// Elements, attributes and classes kept beyond the built-in allow-list, wherever the editor
    /// sanitises: the bound value, typing, a paste or a drop, an insertion and a command's result.
    /// Null keeps the built-in allow-list alone. A policy that names something never allowed (a
    /// script, an event handler, <c>style</c>) throws <see cref="ArgumentException"/>.
    /// </summary>
    [Parameter] public OmniHtmlSanitizerPolicy? SanitizerPolicy { get; set; }

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
            if (SanitizerPolicy is not null)
            {
                options["policy"] = true;
                options["classes"] = OmniHtmlSanitizer.ClassesOf(SanitizerPolicy);
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
    /// <see cref="SelectionChanged"/>.
    /// </summary>
    private bool TracksSelection => SelectionChanged.HasDelegate;

    private string Clean(string? html) => OmniHtmlSanitizer.Sanitize(html, SanitizerPolicy);

    internal string CleanPaste(string? html, string? text) => OmniHtmlSanitizer.SanitizePaste(html, text, SanitizerPolicy);
    internal OmniHtmlEditorMode CurrentMode => _mode;
    private string SurfaceId => Id ?? _generatedId;
    private string LinkInputId => SurfaceId + "-link";

    private IEnumerable<OmniHtmlEditorCommand> ToolbarCommands
    {
        get
        {
            var previousWasSeparator = true;
            OmniHtmlEditorCommand? pendingSeparator = null;
            foreach (var command in (Commands ?? OmniHtmlEditorCommands.Default).Where(IsShown))
            {
                if (command.Action == OmniHtmlEditorAction.Separator)
                {
                    if (!previousWasSeparator)
                    {
                        pendingSeparator = command;
                    }

                    previousWasSeparator = true;
                    continue;
                }

                if (pendingSeparator is not null)
                {
                    yield return pendingSeparator;
                    pendingSeparator = null;
                }

                previousWasSeparator = false;
                yield return command;
            }
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        OmniHtmlSanitizer.Validate(SanitizerPolicy);
        var incomplete = (Commands ?? []).FirstOrDefault(command => command.Action == OmniHtmlEditorAction.Custom && command.Execute is null);
        if (incomplete is not null)
        {
            throw new InvalidOperationException($"The custom command '{incomplete.Name}' of {nameof(OmniHtmlEditor)} has no {nameof(OmniHtmlEditorCommand.Execute)} handler.");
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
                _mountedPolicy = SanitizerPolicy;
                _mountedTracking = TracksSelection;
                await _visualModule.InvokeVoidAsync("mount", _surface, _bridge, Clean(CurrentValue), SurfaceOptions);
            }
            else if (_visualModule is not null)
            {
                if (!string.Equals(CurrentValue, _visualValue, StringComparison.Ordinal))
                {
                    _visualValue = CurrentValue;
                    await _visualModule!.InvokeVoidAsync("setHtml", _surface, Clean(CurrentValue));
                }

                if (_mountedRows != Rows || !ReferenceEquals(_mountedPolicy, SanitizerPolicy) || _mountedTracking != TracksSelection)
                {
                    _mountedRows = Rows;
                    _mountedPolicy = SanitizerPolicy;
                    _mountedTracking = TracksSelection;
                    await _visualModule!.InvokeVoidAsync("configure", _surface, SurfaceOptions);
                }
            }
        }

        if (_focusLink)
        {
            _focusLink = false;
            await _linkInput.FocusAsync();
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

    internal Task HandleSelectionAsync(string json)
    {
        if (_mode != OmniHtmlEditorMode.Visual)
        {
            return Task.CompletedTask;
        }

        _caret = OmniHtmlEditorSelection.Parse(json);
        return SelectionChanged.InvokeAsync(_caret);
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

    private async Task ApplyToolAsync(OmniHtmlEditorTool tool)
    {
        if (Disabled)
        {
            return;
        }

        await CaptureVisualAsync();
        await ApplyAsync(tool.Transform);
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

    /// <summary>The value, once what the visual surface still holds back has been taken in.</summary>
    internal async Task<string> CaptureAsync()
    {
        await CaptureVisualAsync();
        return CurrentHtml;
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

    private bool IsShown(OmniHtmlEditorCommand command) => command.Action switch
    {
        OmniHtmlEditorAction.Bold => EnableBold,
        OmniHtmlEditorAction.Italic => EnableItalic,
        OmniHtmlEditorAction.Subscript => EnableSubscript,
        OmniHtmlEditorAction.Superscript => EnableSuperscript,
        OmniHtmlEditorAction.Indent => EnableIndent,
        OmniHtmlEditorAction.Outdent => EnableOutdent,
        _ => true
    };

    private bool IsDisabled(OmniHtmlEditorCommand command) => Disabled || command.Action switch
    {
        OmniHtmlEditorAction.Undo => _undo.Count == 0,
        OmniHtmlEditorAction.Redo => _redo.Count == 0,
        OmniHtmlEditorAction.Unlink or OmniHtmlEditorAction.ClearFormatting => _mode == OmniHtmlEditorMode.Source,
        _ => false
    };

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
        _ => null
    };

    /// <summary>The pressed state of a toggle, from the formatting at the caret; null for a plain action.</summary>
    private string? PressedOf(OmniHtmlEditorCommand command)
    {
        if (command.Action == OmniHtmlEditorAction.ToggleSource)
        {
            return _mode == OmniHtmlEditorMode.Source ? "true" : "false";
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
