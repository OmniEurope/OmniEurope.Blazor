using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A code editor: Monaco, with syntax colouring, a theme that follows the light or dark appearance
/// of its scope and Ctrl+click links, over a plain text area that is always there to fall back on.
/// </summary>
/// <remarks>
/// <para>
/// The package does not ship Monaco. The host serves the <c>min/vs</c> folder of the
/// <c>monaco-editor</c> package from its own origin, at <see cref="MonacoPath"/>, and allows
/// <c>style-src 'unsafe-inline'</c>, which Monaco needs for the style elements it writes. That is the
/// one exception this component makes to the strict content security policy of the package; under the
/// strict policy, set <see cref="Engine"/> to <see cref="OmniCodeEditorEngine.PlainText"/>.
/// </para>
/// <para>
/// Until Monaco is ready, and for good if it cannot be loaded, the value is edited in the text area,
/// which carries <see cref="OmniInputBase{TValue}.Id"/> and the form state.
/// </para>
/// </remarks>
public partial class OmniCodeEditor
{
    private const int LoadTimeoutMilliseconds = 30000;

    private ElementReference _root;
    private ElementReference _host;
    private IJSObjectReference? _module;
    private DotNetObjectReference<CodeEditorInteropBridge>? _bridge;
    private CodePhase _phase = CodePhase.Loading;
    private OmniCodeEditorEngine? _startedEngine;
    private bool _mounted;
    private string? _editorValue;
    private string? _appliedOptions;
    private string? _appliedHeight;
    private int _line = 1;
    private int _column = 1;
    private bool _disposed;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>The Monaco language identifier: <c>yaml</c>, <c>json</c>, <c>csharp</c>, <c>javascript</c>, <c>html</c>, <c>sql</c>...</summary>
    [Parameter] public string Language { get; set; } = "plaintext";

    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The height, as a CSS length (<c>20rem</c>, <c>320px</c>, <c>50vh</c>). Null keeps 20rem.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Where the host serves the <c>min/vs</c> folder of monaco-editor, relative to the base address of the page.</summary>
    [Parameter] public string MonacoPath { get; set; } = "lib/monaco-editor/min/vs";

    /// <summary>Same-origin JavaScript module implementing editor interop. The default uses Monaco in this document.</summary>
    [Parameter] public string InteropModulePath { get; set; } = "./_content/OmniEurope.Blazor/omni-code-editor.js";

    [Parameter] public OmniCodeEditorEngine Engine { get; set; }

    [Parameter] public bool ShowLineNumbers { get; set; } = true;

    [Parameter] public bool WordWrap { get; set; }

    [Parameter] public int TabSize { get; set; } = 4;

    /// <summary>Whether the line, the column and the language are shown under the editor.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>Patterns whose matches become Ctrl+click links (Monaco only).</summary>
    [Parameter] public IReadOnlyList<OmniCodeEditorLink> Links { get; set; } = Array.Empty<OmniCodeEditorLink>();

    /// <summary>Raised when a link produced by <see cref="Links"/> is followed.</summary>
    [Parameter] public EventCallback<OmniCodeEditorLinkEventArgs> LinkActivated { get; set; }

    /// <summary>The accessible name. Empty uses the localized "code editor".</summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    [Parameter] public string? AriaDescribedBy { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("CodeEditorLabel") : Label;

    private string PhaseName => _phase.ToString().ToLowerInvariant();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Height is not null && !CssLength().IsMatch(Height))
        {
            throw new ArgumentException($"'{Height}' is not a CSS length {nameof(OmniCodeEditor)} accepts for {nameof(Height)} (a number followed by px, rem, em, vh or %).", nameof(Height));
        }

        if (MonacoPath.Contains("://", StringComparison.Ordinal) || MonacoPath.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{nameof(MonacoPath)} must be a path on the origin of the page: the package never loads a script from another origin.", nameof(MonacoPath));
        }

        if (string.IsNullOrWhiteSpace(InteropModulePath) || Uri.TryCreate(InteropModulePath, UriKind.Absolute, out _) || InteropModulePath.StartsWith("//", StringComparison.Ordinal) || InteropModulePath.Contains('\\'))
        {
            throw new ArgumentException($"{nameof(InteropModulePath)} must be a path on the origin of the page.", nameof(InteropModulePath));
        }

        if (Engine == OmniCodeEditorEngine.PlainText)
        {
            _phase = CodePhase.PlainText;
        }
        else if (_phase == CodePhase.PlainText)
        {
            _phase = CodePhase.Loading;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (!string.Equals(Height, _appliedHeight, StringComparison.Ordinal))
        {
            _appliedHeight = Height;
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
            await _module.InvokeVoidAsync("setHeight", _root, Height);
        }

        if (_startedEngine != Engine)
        {
            _startedEngine = Engine;
            await UnmountAsync();
            if (Engine == OmniCodeEditorEngine.Monaco)
            {
                await LoadAsync();
                return;
            }
        }

        if (_phase != CodePhase.Ready || _module is null)
        {
            return;
        }

        if (!_mounted)
        {
            // Claimed before the await, so a render arriving meanwhile does not mount a second editor.
            _mounted = true;
            _bridge ??= DotNetObjectReference.Create(new CodeEditorInteropBridge(this));
            _editorValue = CurrentValue ?? string.Empty;
            _appliedOptions = OptionsSignature();
            if (!await _module.InvokeAsync<bool>("mount", _host, _bridge, Options(_editorValue)))
            {
                _mounted = false;
                _phase = CodePhase.Failed;
                StateHasChanged();
            }

            return;
        }

        if (!string.Equals(CurrentValue ?? string.Empty, _editorValue, StringComparison.Ordinal))
        {
            _editorValue = CurrentValue ?? string.Empty;
            await _module.InvokeVoidAsync("setValue", _host, _editorValue);
        }

        var signature = OptionsSignature();
        if (!string.Equals(signature, _appliedOptions, StringComparison.Ordinal))
        {
            _appliedOptions = signature;
            await _module.InvokeVoidAsync("configure", _host, Options(null));
        }
    }

    private async Task LoadAsync()
    {
        _phase = CodePhase.Loading;
        _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
        var loaded = await _module.InvokeAsync<bool>("load", MonacoPath, CultureInfo.CurrentUICulture.Name, LoadTimeoutMilliseconds);
        if (_disposed || Engine != OmniCodeEditorEngine.Monaco)
        {
            return;
        }

        _phase = loaded ? CodePhase.Ready : CodePhase.Failed;
        StateHasChanged();
    }

    private async Task UnmountAsync()
    {
        if (_mounted && _module is not null)
        {
            _mounted = false;
            await _module.InvokeVoidAsync("dispose", _host);
        }

        _mounted = false;
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

    internal Task HandleCodeChangedAsync(string value)
    {
        if (ReadOnly)
        {
            return Task.CompletedTask;
        }

        _editorValue = value;
        CurrentValue = value;
        return Task.CompletedTask;
    }

    internal void HandleCursorChanged(int line, int column)
    {
        _line = line;
        _column = column;
    }

    internal Task HandleLinkActivatedAsync(OmniCodeEditorLinkEventArgs args) => LinkActivated.InvokeAsync(args);

    private Task HandleFallbackInputAsync(ChangeEventArgs args)
    {
        if (!ReadOnly)
        {
            CurrentValue = args.Value?.ToString() ?? string.Empty;
        }

        return Task.CompletedTask;
    }

    private object Options(string? value) => new
    {
        value,
        monacoPath = MonacoPath,
        culture = CultureInfo.CurrentUICulture.Name,
        loadTimeoutMilliseconds = LoadTimeoutMilliseconds,
        language = string.IsNullOrWhiteSpace(Language) ? "plaintext" : Language,
        readOnly = ReadOnly,
        lineNumbers = ShowLineNumbers,
        wordWrap = WordWrap,
        tabSize = TabSize,
        label = EffectiveLabel,
        links = Links.Select(link => new
        {
            name = link.Name,
            pattern = link.Pattern,
            tooltip = link.Tooltip ?? Localize("CodeEditorLinkHint")
        }).ToArray()
    };

    private string OptionsSignature() => string.Join(
        '',
        new[] { Language, ReadOnly.ToString(), ShowLineNumbers.ToString(), WordWrap.ToString(), TabSize.ToString(CultureInfo.InvariantCulture), EffectiveLabel }
            .Concat(Links.Select(link => $"{link.Name}{link.Pattern}{link.Tooltip}")));

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
        if (_module is not null)
        {
            try
            {
                if (_mounted)
                {
                    await _module.InvokeVoidAsync("dispose", _host);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the editor with it.
            }
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("^\\d+(\\.\\d+)?(px|rem|em|vh|%)$", RegexOptions.CultureInvariant)]
    private static partial Regex CssLength();

    private enum CodePhase
    {
        PlainText,
        Loading,
        Ready,
        Failed
    }
}
