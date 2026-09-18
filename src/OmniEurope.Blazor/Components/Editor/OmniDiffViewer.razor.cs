using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Two versions of a text compared: Monaco's diff editor, side by side or inline, with the changes
/// marked line by line and within lines, over a plain two-pane view that is always there to fall back on.
/// </summary>
/// <remarks>
/// <para>
/// It shares the Monaco of <see cref="OmniCodeEditor"/>, with the same conditions: the host serves the
/// <c>min/vs</c> folder of the <c>monaco-editor</c> package at <see cref="MonacoPath"/> and allows
/// <c>style-src 'unsafe-inline'</c>. Under the strict policy, set <see cref="Engine"/> to
/// <see cref="OmniCodeEditorEngine.PlainText"/>: the two texts are then shown in two panes, without
/// the changes marked, which is also what is shown while Monaco loads and if it cannot be loaded.
/// </para>
/// <para>
/// The theme follows the light or dark appearance of the enclosing <see cref="OmniThemeScope"/>, as
/// the code editor does.
/// </para>
/// </remarks>
public partial class OmniDiffViewer
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-code-editor.js";
    private const int LoadTimeoutMilliseconds = 30000;

    private ElementReference _root;
    private ElementReference _host;
    private IJSObjectReference? _module;
    private DotNetObjectReference<DiffViewerInteropBridge>? _bridge;
    private DiffPhase _phase = DiffPhase.Loading;
    private OmniCodeEditorEngine? _startedEngine;
    private bool _mounted;
    private string? _shownOriginal;
    private string? _shownModified;
    private string? _appliedOptions;
    private string? _appliedHeight;
    private bool _disposed;

    /// <summary>The text before the change, on the left or struck through.</summary>
    [Parameter] public string Original { get; set; } = string.Empty;

    /// <summary>The text after the change, on the right or inserted.</summary>
    [Parameter] public string Modified { get; set; } = string.Empty;

    /// <summary>Raised with the new modified text when the reader edits it (<see cref="ReadOnly"/> off).</summary>
    [Parameter] public EventCallback<string> ModifiedChanged { get; set; }

    /// <summary>The Monaco language identifier of both texts: <c>yaml</c>, <c>json</c>, <c>csharp</c>...</summary>
    [Parameter] public string Language { get; set; } = "plaintext";

    /// <summary>Whether the modified text is locked; on by default, the original text is never editable.</summary>
    [Parameter] public bool ReadOnly { get; set; } = true;

    /// <summary>One column with the removed and added lines interleaved, instead of the two texts side by side.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>What draws the comparison; Monaco by default.</summary>
    [Parameter] public OmniCodeEditorEngine Engine { get; set; }

    /// <summary>Where the host serves the <c>min/vs</c> folder of monaco-editor, relative to the base address of the page.</summary>
    [Parameter] public string MonacoPath { get; set; } = "lib/monaco-editor/min/vs";

    /// <summary>The height, as a CSS length (<c>20rem</c>, <c>320px</c>, <c>50vh</c>). Null keeps 20rem.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>The accessible name of the comparison; the localized "comparison" when empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The name of the original text; the localized "before" when empty.</summary>
    [Parameter] public string? OriginalLabel { get; set; }

    /// <summary>The name of the modified text; the localized "after" when empty.</summary>
    [Parameter] public string? ModifiedLabel { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("DiffViewerLabel") : Label;

    private string EffectiveOriginalLabel => string.IsNullOrWhiteSpace(OriginalLabel) ? Localize("DiffViewerOriginal") : OriginalLabel;

    private string EffectiveModifiedLabel => string.IsNullOrWhiteSpace(ModifiedLabel) ? Localize("DiffViewerModified") : ModifiedLabel;

    private string PhaseName => _phase.ToString().ToLowerInvariant();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Height is not null && !CssLength().IsMatch(Height))
        {
            throw new ArgumentException($"'{Height}' is not a CSS length {nameof(OmniDiffViewer)} accepts for {nameof(Height)} (a number followed by px, rem, em, vh or %).", nameof(Height));
        }

        if (MonacoPath.Contains("://", StringComparison.Ordinal) || MonacoPath.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{nameof(MonacoPath)} must be a path on the origin of the page: the package never loads a script from another origin.", nameof(MonacoPath));
        }

        if (Engine == OmniCodeEditorEngine.PlainText)
        {
            _phase = DiffPhase.PlainText;
        }
        else if (_phase == DiffPhase.PlainText)
        {
            _phase = DiffPhase.Loading;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!string.Equals(Height, _appliedHeight, StringComparison.Ordinal))
            {
                _appliedHeight = Height;
                if (!await EnsureModuleAsync())
                {
                    return;
                }

                await _module!.InvokeVoidAsync("setHeight", _root, Height);
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

            if (_disposed || _phase != DiffPhase.Ready || _module is null)
            {
                return;
            }

            if (!_mounted)
            {
                // Claimed before the await, so a render arriving meanwhile does not mount a second editor.
                _mounted = true;
                _bridge ??= DotNetObjectReference.Create(new DiffViewerInteropBridge(HandleModifiedChangedAsync));
                (_shownOriginal, _shownModified) = (Original ?? string.Empty, Modified ?? string.Empty);
                _appliedOptions = OptionsSignature();
                if (!await _module.InvokeAsync<bool>("mountDiff", _host, _bridge, Options(_shownOriginal, _shownModified)))
                {
                    _mounted = false;
                    _phase = DiffPhase.Failed;
                    StateHasChanged();
                }

                return;
            }

            if (!string.Equals(Original ?? string.Empty, _shownOriginal, StringComparison.Ordinal)
                || !string.Equals(Modified ?? string.Empty, _shownModified, StringComparison.Ordinal))
            {
                (_shownOriginal, _shownModified) = (Original ?? string.Empty, Modified ?? string.Empty);
                await _module.InvokeVoidAsync("setDiff", _host, _shownOriginal, _shownModified);
            }

            var signature = OptionsSignature();
            if (!string.Equals(signature, _appliedOptions, StringComparison.Ordinal))
            {
                _appliedOptions = signature;
                await _module.InvokeVoidAsync("configureDiff", _host, Options(null, null));
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the editor with it.
        }
    }

    private async Task LoadAsync()
    {
        _phase = DiffPhase.Loading;
        if (!await EnsureModuleAsync())
        {
            return;
        }

        var loaded = await _module!.InvokeAsync<bool>("load", MonacoPath, CultureInfo.CurrentUICulture.Name, LoadTimeoutMilliseconds);
        if (_disposed || Engine != OmniCodeEditorEngine.Monaco)
        {
            return;
        }

        _phase = loaded ? DiffPhase.Ready : DiffPhase.Failed;
        StateHasChanged();
    }

    /// <summary>
    /// Imports the script once. A viewer disposed while the import was pending releases the module at
    /// once rather than keeping one nothing will dispose; false then tells the caller to stop.
    /// </summary>
    private async Task<bool> EnsureModuleAsync()
    {
        if (_module is not null)
        {
            return !_disposed;
        }

        var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        if (_disposed)
        {
            await module.DisposeAsync();
            return false;
        }

        _module = module;
        return true;
    }

    private async Task UnmountAsync()
    {
        if (_mounted && _module is not null)
        {
            _mounted = false;
            await _module.InvokeVoidAsync("disposeDiff", _host);
        }

        _mounted = false;
    }

    private Task HandleModifiedChangedAsync(string value) => InvokeAsync(async () =>
    {
        if (_disposed || ReadOnly)
        {
            return;
        }

        _shownModified = value;
        await ModifiedChanged.InvokeAsync(value);
    });

    private Task HandleFallbackInputAsync(ChangeEventArgs args) =>
        ReadOnly ? Task.CompletedTask : ModifiedChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private object Options(string? original, string? modified) => new
    {
        original,
        modified,
        language = string.IsNullOrWhiteSpace(Language) ? "plaintext" : Language,
        readOnly = ReadOnly,
        inline = Inline,
        originalLabel = EffectiveOriginalLabel,
        modifiedLabel = EffectiveModifiedLabel
    };

    private string OptionsSignature() => string.Join(
        '\u001F',
        Language, ReadOnly.ToString(), Inline.ToString(), EffectiveOriginalLabel, EffectiveModifiedLabel);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                if (_mounted)
                {
                    await _module.InvokeVoidAsync("disposeDiff", _host);
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

    private enum DiffPhase
    {
        PlainText,
        Loading,
        Ready,
        Failed
    }
}