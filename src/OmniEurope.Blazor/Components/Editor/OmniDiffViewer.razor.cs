namespace OmniEurope.Blazor.Components;

/// <summary>
/// Two versions of a text compared: Monaco's diff editor, side by side or inline, with the changes
/// marked line by line and within lines, over a plain two-pane view that is always there to fall back on.
/// </summary>
/// <remarks>
/// <para>
/// It shares the Monaco of <see cref="OmniCodeEditor"/>, with the same conditions: the host serves the
/// <c>min/vs</c> folder of the <c>monaco-editor</c> package at <see cref="MonacoPath"/> and allows
/// <c>style-src 'unsafe-inline'</c>. Under the strict policy, either hand through <see cref="InteropModulePath"/> a
/// module that mounts Monaco in a frame with a policy of its own, or set <see cref="Engine"/> to
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
    private ElementReference _root;
    private ElementReference _host;
    private Internal.MonacoHost? _monaco;
    private DotNetObjectReference<DiffViewerInteropBridge>? _bridge;
    private DiffPhase _phase = DiffPhase.Loading;
    private OmniCodeEditorEngine? _startedEngine;
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

    /// <summary>
    /// The language identifier of both texts (<c>yaml</c>, <c>json</c>, <c>csharp</c>...), with the same meaning as on
    /// <see cref="OmniCodeEditor"/>. Null or blank means <c>plaintext</c>.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    private string EffectiveLanguage => string.IsNullOrWhiteSpace(Language) ? "plaintext" : Language;

    /// <summary>
    /// Whether the modified text can be read, selected and copied but not changed; on by default, since a
    /// comparison is first a view: an editing screen sets it to false. The original text is never editable. The comparison
    /// is not dimmed.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; } = true;

    /// <summary>
    /// Whether the comparison is disabled: the modified text cannot be changed even with
    /// <see cref="ReadOnly"/> off (Monaco read-only, the fallback text area disabled) and the whole
    /// comparison is dimmed.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the modified text is closed to editing, read-only or disabled.</summary>
    private bool IsLocked => ReadOnly || Disabled;

    /// <summary>Whether the comparison is unified: one column with the removed and added lines interleaved, instead of the two texts side by side.</summary>
    [Parameter] public bool Unified { get; set; }

    /// <summary>What draws the comparison; Monaco by default.</summary>
    [Parameter] public OmniCodeEditorEngine Engine { get; set; }

    /// <summary>
    /// Where the host serves the <c>min/vs</c> folder of monaco-editor, relative to the base address of the page
    /// (<c>assets/monaco/vs</c>) or from the root of the origin (<c>/_content/x/vs</c>). It must stay on the
    /// origin of the page: a scheme (<c>https:</c>, <c>data:</c>, <c>blob:</c>), a leading <c>//</c>, a backslash,
    /// a control character or a blank value is refused. A <c>..</c> segment is accepted, as it cannot leave the origin.
    /// </summary>
    [Parameter] public string MonacoPath { get; set; } = Internal.MonacoHost.DefaultPath;

    /// <summary>
    /// The same-origin JavaScript module that drives the diff editor, as on <see cref="OmniCodeEditor.InteropModulePath"/>.
    /// The default mounts Monaco in this document. A host with a strict style policy hands a module of its own that
    /// mounts Monaco in a frame with a policy of its own; that module exports <c>setHeight(root, height)</c>,
    /// <c>load(basePath, culture, timeout)</c> (a promise of whether Monaco is available), <c>mountDiff(host, dotnet,
    /// options)</c> (whether it mounted), <c>setDiff(host, original, modified)</c>, <c>configureDiff(host, options)</c>,
    /// <c>goToDiff(host, direction)</c> and <c>disposeDiff(host)</c>, and reports the edits of the modified text through
    /// <c>dotnet.invokeMethodAsync('OnModifiedChanged', value)</c>. The rule of <see cref="MonacoPath"/> applies: a path
    /// leaving the origin of the page is refused.
    /// </summary>
    [Parameter] public string InteropModulePath { get; set; } = Internal.OmniModules.CodeEditor;

    /// <summary>
    /// The height, as a CSS length (<c>20rem</c>, <c>320px</c>, <c>50vh</c>). Null keeps 20rem. With
    /// <see cref="FillAvailableHeight"/> on, it is the minimum height of the comparison instead.
    /// </summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// Whether the viewer takes the free height of its parent instead of a height of its own, like
    /// <see cref="OmniDataGrid{TItem}.FillAvailableHeight"/>: off by default. The parent is a block with a height or a
    /// column flex container; the comparison, the two-pane view it falls back on and the optional
    /// <see cref="ShowNavigation"/> bar are laid out in a column, the bar on top and the comparison taking the rest.
    /// <see cref="Height"/> then becomes the minimum height of the comparison (20rem when null), which it never goes under:
    /// when the parent leaves less, the viewer keeps that minimum and the parent scrolls. Monaco follows the size of its
    /// box when it changes.
    /// </summary>
    [Parameter] public bool FillAvailableHeight { get; set; }

    /// <summary>
    /// Whether a small bar above the comparison offers two icon-only buttons, previous change and next change, that
    /// do what <see cref="GoToPreviousChangeAsync"/> and <see cref="GoToNextChangeAsync"/> do. Off by default: a page
    /// that draws the buttons itself, in its own top bar, calls the two methods. The buttons are disabled while
    /// Monaco is not mounted (loading, <see cref="OmniCodeEditorEngine.PlainText"/>, or failed).
    /// </summary>
    [Parameter] public bool ShowNavigation { get; set; }

    /// <summary>The accessible name of the comparison; the localized "comparison" when empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The name of the original text; the localized "before" when empty.</summary>
    [Parameter] public string? OriginalLabel { get; set; }

    /// <summary>The name of the modified text; the localized "after" when empty.</summary>
    [Parameter] public string? ModifiedLabel { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("DiffViewerLabel") : Label;

    private string EffectiveOriginalLabel => string.IsNullOrWhiteSpace(OriginalLabel) ? Localize("DiffViewerOriginal") : OriginalLabel;

    private string EffectiveModifiedLabel => string.IsNullOrWhiteSpace(ModifiedLabel) ? Localize("DiffViewerModified") : ModifiedLabel;

    private Internal.MonacoHost Monaco => _monaco ??= new(JavaScript, () => InteropModulePath, () => _disposed);

    private string PhaseName => _phase.ToString().ToLowerInvariant();

    /// <summary>Whether Monaco's diff editor is mounted, so that the navigation has something to move in.</summary>
    private bool NavigationReady => _phase == DiffPhase.Ready && Monaco.Mounted;

    /// <summary>
    /// Moves to the next change of the comparison and reveals it, as Monaco's diff editor does (<c>goToDiff('next')</c>).
    /// It does nothing, and does not throw, when Monaco's diff editor is not mounted (still loading, the
    /// <see cref="OmniCodeEditorEngine.PlainText"/> engine, or Monaco failed to load or mount), when the viewer is disposed
    /// and when the circuit is gone.
    /// </summary>
    /// <returns>A task that completes once the editor has moved, or at once when there is nothing to move.</returns>
    public Task GoToNextChangeAsync() => GoToChangeAsync("next");

    /// <summary>
    /// Moves to the previous change of the comparison and reveals it, as Monaco's diff editor does (<c>goToDiff('previous')</c>).
    /// It does nothing, and does not throw, when Monaco's diff editor is not mounted (still loading, the
    /// <see cref="OmniCodeEditorEngine.PlainText"/> engine, or Monaco failed to load or mount), when the viewer is disposed
    /// and when the circuit is gone.
    /// </summary>
    /// <returns>A task that completes once the editor has moved, or at once when there is nothing to move.</returns>
    public Task GoToPreviousChangeAsync() => GoToChangeAsync("previous");

    private async Task GoToChangeAsync(string direction)
    {
        if (_disposed || !NavigationReady || Monaco.Module is null)
        {
            return;
        }

        try
        {
            await Monaco.Module.InvokeVoidAsync("goToDiff", _host, direction);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the editor with it.
        }
    }

    /// <summary>
    /// Checks <see cref="Height"/> and <see cref="MonacoPath"/>, then switches to the plain text view
    /// for <see cref="OmniCodeEditorEngine.PlainText"/>, or back to loading when Monaco is chosen again.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <see cref="Height"/> is not a number followed by px, rem, em, vh or %, or <see cref="MonacoPath"/>
    /// is not a path on the origin of the page.
    /// </exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Internal.MonacoHost.RequireParameters(nameof(OmniDiffViewer), Height, MonacoPath);
        Internal.MonacoHost.RequireInteropModulePath(InteropModulePath);

        if (Engine == OmniCodeEditorEngine.PlainText)
        {
            _phase = DiffPhase.PlainText;
        }
        else if (_phase == DiffPhase.PlainText)
        {
            _phase = DiffPhase.Loading;
        }
    }

    /// <summary>
    /// Applies a new height, loads Monaco when the engine turned to it, mounts the diff editor once
    /// Monaco is ready (falling back to the plain view when mounting fails), then pushes texts or options
    /// that changed since the last render. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the diff editor is up to date.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await SyncAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the editor with it.
        }
    }

    private async Task SyncAsync()
    {
        if (!await SyncHeightAsync())
        {
            return;
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

        if (_disposed || _phase != DiffPhase.Ready || Monaco.Module is null)
        {
            return;
        }

        if (!Monaco.Mounted)
        {
            await MountAsync(Monaco.Module);
            return;
        }

        await PushChangesAsync(Monaco.Module);
    }

    /// <summary>Applies a new height; false when the viewer went while its script loaded.</summary>
    private async Task<bool> SyncHeightAsync()
    {
        if (string.Equals(Height, _appliedHeight, StringComparison.Ordinal))
        {
            return true;
        }

        _appliedHeight = Height;
        return await Monaco.SetHeightAsync(_root, Height);
    }

    /// <summary>Mounts the diff editor with both texts; falls back to the plain view when it cannot.</summary>
    private async Task MountAsync(IJSObjectReference module)
    {
        // Claimed before the await, so a render arriving meanwhile does not mount a second editor.
        Monaco.Mounted = true;
        _bridge ??= DotNetObjectReference.Create(new DiffViewerInteropBridge(HandleModifiedChangedAsync));
        (_shownOriginal, _shownModified) = (Original ?? string.Empty, Modified ?? string.Empty);
        _appliedOptions = OptionsSignature();
        if (!await module.InvokeAsync<bool>("mountDiff", _host, _bridge, Options(_shownOriginal, _shownModified)))
        {
            Monaco.Mounted = false;
            _phase = DiffPhase.Failed;
            StateHasChanged();
        }
        else if (ShowNavigation)
        {
            // The navigation buttons are enabled once there is a mounted editor to move in.
            StateHasChanged();
        }
    }

    /// <summary>Pushes to the mounted editor texts or options that changed since they were last given.</summary>
    private async Task PushChangesAsync(IJSObjectReference module)
    {
        var (original, modified) = (Original ?? string.Empty, Modified ?? string.Empty);
        if (!string.Equals(original, _shownOriginal, StringComparison.Ordinal) || !string.Equals(modified, _shownModified, StringComparison.Ordinal))
        {
            (_shownOriginal, _shownModified) = (original, modified);
            await module.InvokeVoidAsync("setDiff", _host, _shownOriginal, _shownModified);
        }

        var signature = OptionsSignature();
        if (!string.Equals(signature, _appliedOptions, StringComparison.Ordinal))
        {
            _appliedOptions = signature;
            await module.InvokeVoidAsync("configureDiff", _host, Options(null, null));
        }
    }

    private async Task LoadAsync()
    {
        _phase = DiffPhase.Loading;
        var loaded = await Monaco.LoadAsync(MonacoPath);
        if (loaded is null || _disposed || Engine != OmniCodeEditorEngine.Monaco)
        {
            return;
        }

        _phase = loaded.Value ? DiffPhase.Ready : DiffPhase.Failed;
        StateHasChanged();
    }

    private Task UnmountAsync() => Monaco.UnmountAsync("disposeDiff", _host);

    private Task HandleModifiedChangedAsync(string value) => InvokeAsync(async () =>
    {
        if (_disposed || IsLocked)
        {
            return;
        }

        _shownModified = value;
        await ModifiedChanged.InvokeAsync(value);
    });

    private Task HandleFallbackInputAsync(ChangeEventArgs args) =>
        IsLocked ? Task.CompletedTask : ModifiedChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private object Options(string? original, string? modified) => new
    {
        original,
        modified,
        language = EffectiveLanguage,
        readOnly = IsLocked,
        inline = Unified,
        originalLabel = EffectiveOriginalLabel,
        modifiedLabel = EffectiveModifiedLabel
    };

    private string OptionsSignature() => string.Join(
        '\u001F',
        EffectiveLanguage, IsLocked.ToString(), Unified.ToString(), EffectiveOriginalLabel, EffectiveModifiedLabel);

    /// <summary>Disposes the Monaco diff editor and its module, and the script's reference to the component; a lost circuit is ignored.</summary>
    /// <returns>A task that completes once the editor is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await Monaco.ReleaseAsync("disposeDiff", _host);

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    private enum DiffPhase
    {
        PlainText,
        Loading,
        Ready,
        Failed
    }
}