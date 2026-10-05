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

    /// <summary>
    /// Whether the code can be read, selected and copied but not changed: Monaco is read-only and the
    /// fallback text area <c>readonly</c>, both keep the focus, and the editor is not dimmed.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether the editor is disabled: Monaco is read-only and the fallback text area is disabled, so
    /// the value can no longer be changed. The editor is drawn dimmed.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The height, as a CSS length (<c>20rem</c>, <c>320px</c>, <c>50vh</c>). Null keeps 20rem.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Where the host serves the <c>min/vs</c> folder of monaco-editor, relative to the base address of the page.</summary>
    [Parameter] public string MonacoPath { get; set; } = "lib/monaco-editor/min/vs";

    /// <summary>Same-origin JavaScript module implementing editor interop. The default uses Monaco in this document.</summary>
    [Parameter] public string InteropModulePath { get; set; } = Internal.OmniModules.CodeEditor;

    /// <summary>
    /// What edits the code: <see cref="OmniCodeEditorEngine.Monaco"/> (the default) or the plain text area
    /// alone with <see cref="OmniCodeEditorEngine.PlainText"/>. Changing it unmounts the current editor.
    /// </summary>
    [Parameter] public OmniCodeEditorEngine Engine { get; set; }

    /// <summary>Whether Monaco shows the line numbers in its margin; true by default. The fallback text area has none.</summary>
    [Parameter] public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Whether long lines wrap instead of scrolling sideways. Off by default.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Width of a tab in spaces, in Monaco; 4 by default, and a value of zero or less also gives 4.</summary>
    [Parameter] public int TabSize { get; set; } = 4;

    /// <summary>Whether the line, the column and the language are shown under the editor.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>Patterns whose matches become Ctrl+click links (Monaco only).</summary>
    [Parameter] public IReadOnlyList<OmniCodeEditorLink> Links { get; set; } = Array.Empty<OmniCodeEditorLink>();

    /// <summary>Raised when a link produced by <see cref="Links"/> is followed.</summary>
    [Parameter] public EventCallback<OmniCodeEditorLinkEventArgs> LinkActivated { get; set; }

    /// <summary>
    /// The accessible name. On the fallback text area it is written as <c>aria-label</c> only when set, so
    /// that the <c>label</c> of an enclosing <see cref="OmniFormField"/> names it otherwise. Monaco draws a
    /// text area of its own that no <c>label for</c> reaches: it takes this name, or the localized "code
    /// editor" when null.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Identifiers of the elements that describe the editor, written as <c>aria-describedby</c> on the fallback text area; none when null.</summary>
    [Parameter] public string? AriaDescribedBy { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("CodeEditorLabel") : Label;

    /// <summary>Whether the value is closed to editing, read-only or disabled.</summary>
    private bool IsLocked => ReadOnly || Disabled;

    private string PhaseName => _phase.ToString().ToLowerInvariant();

    /// <summary>
    /// Checks <see cref="Height"/>, <see cref="MonacoPath"/> and <see cref="InteropModulePath"/>, then
    /// switches to the plain text area for <see cref="OmniCodeEditorEngine.PlainText"/>, or back to
    /// loading when Monaco is chosen again.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <see cref="Height"/> is not a number followed by px, rem, em, vh or %; <see cref="MonacoPath"/> or
    /// <see cref="InteropModulePath"/> is not a path on the origin of the page.
    /// </exception>
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

        // The text checks first: whether //host or \\host reads as an absolute URI depends on the system.
        if (string.IsNullOrWhiteSpace(InteropModulePath) || InteropModulePath.StartsWith("//", StringComparison.Ordinal)
            || InteropModulePath.Contains('\\') || Uri.TryCreate(InteropModulePath, UriKind.Absolute, out _))
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

    /// <summary>
    /// Applies a new height, loads Monaco when the engine turned to it, mounts the editor once Monaco is
    /// ready (falling back to the text area when mounting fails), then pushes a value or options that
    /// changed since the last render.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the editor is up to date.</returns>
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
            await MountAsync(_module);
            return;
        }

        await PushChangesAsync(_module);
    }

    /// <summary>Mounts Monaco with the value and the options; falls back to the text area when it cannot.</summary>
    private async Task MountAsync(IJSObjectReference module)
    {
        // Claimed before the await, so a render arriving meanwhile does not mount a second editor.
        _mounted = true;
        _bridge ??= DotNetObjectReference.Create(new CodeEditorInteropBridge(this));
        _editorValue = CurrentValue ?? string.Empty;
        _appliedOptions = OptionsSignature();
        if (!await module.InvokeAsync<bool>("mount", _host, _bridge, Options(_editorValue)))
        {
            _mounted = false;
            _phase = CodePhase.Failed;
            StateHasChanged();
        }
    }

    /// <summary>Pushes to the mounted editor a value or options that changed since they were last given.</summary>
    private async Task PushChangesAsync(IJSObjectReference module)
    {
        var value = CurrentValue ?? string.Empty;
        if (!string.Equals(value, _editorValue, StringComparison.Ordinal))
        {
            _editorValue = value;
            await module.InvokeVoidAsync("setValue", _host, _editorValue);
        }

        var signature = OptionsSignature();
        if (!string.Equals(signature, _appliedOptions, StringComparison.Ordinal))
        {
            _appliedOptions = signature;
            await module.InvokeVoidAsync("configure", _host, Options(null));
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
        if (IsLocked)
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
        if (!IsLocked)
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
        readOnly = IsLocked,
        lineNumbers = ShowLineNumbers,
        wordWrap = Wrap,
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
        new[] { Language, IsLocked.ToString(), ShowLineNumbers.ToString(), Wrap.ToString(), TabSize.ToString(CultureInfo.InvariantCulture), EffectiveLabel }
            .Concat(Links.Select(link => $"{link.Name}{link.Pattern}{link.Tooltip}")));

    /// <summary>Takes any text as the value, null as an empty string; parsing never fails.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The text, or an empty string for null.</param>
    /// <param name="validationErrorMessage">Always null.</param>
    /// <returns>Always true.</returns>
    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }

    /// <summary>
    /// Releases the form subscription, disposes the Monaco editor and its module, and the script's
    /// reference to the component; a lost circuit is ignored.
    /// </summary>
    /// <returns>A task that completes once the editor is released.</returns>
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
