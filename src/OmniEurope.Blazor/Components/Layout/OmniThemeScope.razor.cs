namespace OmniEurope.Blazor.Components;

/// <summary>
/// A container that sets the appearance, the density and optionally the theme of everything it holds,
/// through <c>data-omni-theme</c>, <c>data-omni-density</c> and design tokens scoped to its element.
/// The mode actually drawn, light or dark, is carried by <c>data-omni-theme-resolved</c>, also when
/// <see cref="Appearance"/> follows the system.
/// </summary>
/// <remarks>
/// A scope whose markup writes none of <see cref="Appearance"/>, <see cref="Density"/>, <see cref="Preset"/>,
/// <see cref="Palette"/>, <see cref="Font"/>, <see cref="Backdrop"/> and <see cref="BackdropMotion"/> follows the user's
/// <see cref="OmniAppearanceState"/>: it takes those seven from it, repaints when it changes, lays its text
/// size and size of the controls on the document root (<c>data-oe-text-size</c>, <c>data-oe-control-size</c>)
/// and keeps both levels in its snapshot. It draws nothing until the preference is read. A scope that
/// writes any of the seven is controlled by its host, as before, and leaves the state alone.
/// </remarks>
public partial class OmniThemeScope
{
    /// <summary>The version of the snapshot format <c>omni-boot.js</c> replays.</summary>
    internal const int SnapshotVersion = 1;

    private ElementReference _element;
    private bool _followingSystem;
    private SnapshotState? _writtenSnapshot;
    private IJSObjectReference? _themeModule;
    private OmniThemePreset? _appliedPreset;
    private OmniThemePalette? _appliedPalette;
    private OmniThemeFont? _appliedFont;
    private OmniThemeBackdrop? _appliedBackdrop;
    private string? _canvasScript;
    private OmniAppearance _appliedAppearance;
    private ElementReference _canvas;
    private IJSObjectReference? _canvasModule;
    private bool _canvasRunning;
    private bool _canvasDrawn;
    private bool _canvasMotion;
    private bool _canvasSyncing;
    private bool _canvasSyncAgain;
    private bool _canvasTokensChanged;
    private AppearanceFollower? _shared;
    private bool _decided;
    private bool _waiting;
    private bool _rootAttached;

    /// <summary>The content the scope themes. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Light, dark, or following the system; <see cref="OmniAppearance.System"/> by default.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; }

    /// <summary>How tightly the content is laid out; <see cref="OmniDensity.Comfortable"/> by default.</summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>
    /// A theme from <see cref="OmniThemePresets"/> to paint this scope with, or null for the shipped
    /// look. Only the scope and what it contains change; the half used follows
    /// <see cref="Appearance"/>, and <see cref="OmniAppearance.System"/> follows the system setting
    /// as it changes.
    /// </summary>
    /// <remarks>
    /// The values are written through the CSSOM by <c>omni-theme.js</c>, never as a style attribute,
    /// so the strict content security policy holds. A light or dark scope that never receives a
    /// preset, a palette, a font nor a <see cref="SnapshotKey"/> never loads the script; a scope that
    /// follows the system loads it to resolve the mode, without painting any token.
    /// </remarks>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>
    /// A palette from <see cref="OmniThemePalettes"/> to paint this scope with. With a
    /// <see cref="Preset"/>, the theme keeps its shape and takes these colours
    /// (<see cref="OmniThemePreset.With(OmniThemePalette)"/>); without one, only the colours change and the shipped
    /// shape stays. Null keeps the preset's own palette.
    /// </summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>
    /// A font from <see cref="OmniThemeFonts"/> for the text and the headings of this scope, laid over
    /// the theme's own. Null keeps the font the theme is drawn with.
    /// </summary>
    [Parameter]
    public OmniThemeFont? Font { get; set; }

    /// <summary>
    /// A backdrop from <see cref="OmniThemeBackdrops"/> to draw the <see cref="Preset"/> over, in place
    /// of its own (<see cref="OmniThemePreset.With(OmniThemeBackdrop)"/>); <see cref="OmniThemeBackdrops.None"/>
    /// leaves the plain page. Null keeps the theme's own backdrop. Every theme offers every backdrop of the catalogue
    /// (<see cref="OmniThemePresets.BackdropsFor"/>), Essentiel included; without a <see cref="Preset"/> no backdrop is drawn.
    /// </summary>
    [Parameter]
    public OmniThemeBackdrop? Backdrop { get; set; }

    /// <summary>
    /// Whether a backdrop that moves (Givre, Trou noir) may move; true by default. False holds the
    /// field still (<c>data-omni-backdrop-motion="off"</c>). A still backdrop ignores it, and a system
    /// set to reduce motion holds the field still whatever the value.
    /// </summary>
    [Parameter]
    public bool BackdropMotion { get; set; } = true;

    /// <summary>
    /// The <c>localStorage</c> key under which the scope keeps a snapshot of what it painted (mode,
    /// density, backdrop motion and both halves of its tokens), for <c>omni-boot.js</c> to replay on
    /// the document before Blazor starts (<c>data-theme-snapshot-key</c> on that script). Null, the
    /// default, writes nothing. Give it to the one scope that paints the application.
    /// </summary>
    /// <remarks>
    /// The snapshot is rewritten whenever the look changes. When the scope paints for the first time,
    /// it takes over from the boot copy: the tokens <c>omni-boot.js</c> laid on the document root and on
    /// this scope are removed, the root ones once the boot splash has left the page.
    /// </remarks>
    [Parameter]
    public string? SnapshotKey { get; set; }

    /// <summary>
    /// When <see cref="Preset"/>, <see cref="Palette"/>, <see cref="Font"/> or, with one of them set,
    /// <see cref="Appearance"/> changed, writes the resulting tokens on the scope through <c>omni-theme.js</c>,
    /// or clears them when none of the three is set any more. Under <see cref="OmniAppearance.System"/>,
    /// the script also keeps <c>data-omni-theme-resolved</c> on the system setting, and with a
    /// <see cref="SnapshotKey"/> it stores the snapshot of the look. The script is loaded on first use only.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_waiting)
        {
            return;
        }

        var tokensChanged = await ApplyTokensAsync();
        await SyncResolvedModeAsync();
        await WriteSnapshotAsync();
        await SyncCanvasAsync(tokensChanged);
        await SyncDocumentRootAsync();
    }

    /// <summary>
    /// Decides, from the parameters of the first render, whether the scope follows the user's
    /// <see cref="OmniAppearanceState"/> (none of its seven appearance parameters written), then applies them.
    /// </summary>
    /// <param name="parameters">The parameters given by the renderer.</param>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_decided)
        {
            _decided = true;
            var hostControls = AppearanceFollower.Sets(parameters, nameof(Appearance), nameof(Density), nameof(Preset),
                nameof(Palette), nameof(Font), nameof(Backdrop), nameof(BackdropMotion));
            _shared = AppearanceFollower.Start(SharedAppearance, hostControls, () => InvokeAsync(() =>
            {
                FollowState();
                StateHasChanged();
            }));
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>Following the state, waits for the preference before the first frame with content.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (_shared is null)
        {
            return;
        }

        var loading = _shared.State.LoadAsync();
        if (!loading.IsCompleted)
        {
            _waiting = true;
            try
            {
                await loading;
            }
            finally
            {
                _waiting = false;
            }
        }
    }

    /// <summary>Following the state, takes the appearance from it.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FollowState();
    }

    private void FollowState()
    {
        if (_shared?.State is not { } state)
        {
            return;
        }

        Appearance = state.Appearance;
        Density = state.Density;
        Preset = state.Preset;
        Palette = state.Palette;
        Font = state.Font;
        Backdrop = state.Backdrop;
        BackdropMotion = state.BackdropMotion;
    }

    /// <summary>Following the state, lays its text size and size of the controls on the document root.</summary>
    private async Task SyncDocumentRootAsync()
    {
        if (_shared?.State is not { } state)
        {
            return;
        }

        if (!_rootAttached)
        {
            state.DocumentRoot.Attach();
            _rootAttached = true;
        }

        await state.DocumentRoot.SyncAsync(state.TextSizeLevel, state.ControlSizeLevel);
    }

    /// <summary>The value of <c>data-omni-theme</c>: the mode asked, or dark under a dark-only theme.</summary>
    private string AppearanceValue => EffectiveAppearance.ToString().ToLowerInvariant();

    /// <summary>
    /// The value of <c>data-omni-theme-resolved</c> rendered with the markup: the mode itself when it is
    /// light or dark. Under <see cref="OmniAppearance.System"/> only the browser knows it, so the
    /// attribute is left out of the markup and <c>omni-theme.js</c> writes it and follows the system.
    /// </summary>
    private string? ResolvedValue => EffectiveAppearance == OmniAppearance.System ? null : AppearanceValue;

    /// <summary>The snapshot key, or null when none is usable.</summary>
    private string? SnapshotStorageKey => string.IsNullOrWhiteSpace(SnapshotKey) ? null : SnapshotKey;

    private async Task<IJSObjectReference> LoadThemeModuleAsync() =>
        _themeModule ??= await Internal.OmniScriptModule.ImportAsync(JavaScript, Internal.OmniModules.Theme);

    /// <summary>Starts or stops following the system setting for <c>data-omni-theme-resolved</c>.</summary>
    private async Task SyncResolvedModeAsync()
    {
        var follow = EffectiveAppearance == OmniAppearance.System;
        if (follow == _followingSystem)
        {
            return;
        }

        if (follow)
        {
            await (await LoadThemeModuleAsync()).InvokeVoidAsync("followSystem", _element);
        }
        else
        {
            // Light or dark: the markup carries the value again, the script only stops listening.
            await _themeModule!.InvokeVoidAsync("unfollowSystem", _element);
        }

        _followingSystem = follow;
    }

    /// <summary>Stores the snapshot of the look when a key is given and the look changed since the last one.</summary>
    private async Task WriteSnapshotAsync()
    {
        var key = SnapshotStorageKey;
        if (key is null)
        {
            return;
        }

        var shared = _shared?.State;
        var state = new SnapshotState(key, EffectiveAppearance, Density, BackdropMotion, Preset, Palette, Font, DrawnBackdrop,
            shared?.TextSizeLevel, shared?.ControlSizeLevel);
        if (state == _writtenSnapshot)
        {
            return;
        }

        var (light, dark) = Resolve();
        var module = await LoadThemeModuleAsync();
        var density = Density.ToString().ToLowerInvariant();
        if (shared is null)
        {
            await module.InvokeVoidAsync("snapshot", _element, key, SnapshotVersion, AppearanceValue, density, BackdropMotion, light, dark);
        }
        else
        {
            // Following the state, the scope also owns the root levels: the boot copy lays them before the first paint.
            await module.InvokeVoidAsync("snapshot", _element, key, SnapshotVersion, AppearanceValue, density, BackdropMotion, light, dark,
                shared.TextSizeLevel, shared.ControlSizeLevel);
        }

        _writtenSnapshot = state;
    }

    /// <summary>What a snapshot was written from; a change in any member writes it again.</summary>
    private sealed record SnapshotState(
        string Key,
        OmniAppearance Appearance,
        OmniDensity Density,
        bool BackdropMotion,
        OmniThemePreset? Preset,
        OmniThemePalette? Palette,
        OmniThemeFont? Font,
        OmniThemeBackdrop? Backdrop,
        int? TextSizeLevel,
        int? ControlSizeLevel);

    /// <summary>Writes or clears the tokens when the look changed; true when it did.</summary>
    private async Task<bool> ApplyTokensAsync()
    {
        var unchanged = ReferenceEquals(Preset, _appliedPreset) && ReferenceEquals(Palette, _appliedPalette) && ReferenceEquals(Font, _appliedFont)
            && ReferenceEquals(DrawnBackdrop, _appliedBackdrop);
        if (unchanged && ((Preset is null && Palette is null && Font is null) || EffectiveAppearance == _appliedAppearance))
        {
            return false;
        }

        var module = await LoadThemeModuleAsync();
        // The scope that keeps the snapshot takes over from the boot copy in the same call that paints it,
        // so no frame shows the scope between the two.
        var handOver = SnapshotStorageKey is not null;
        var (light, dark) = Resolve();
        if (light is null || dark is null)
        {
            await module.InvokeVoidAsync("clear", _element, handOver);
        }
        else
        {
            await module.InvokeVoidAsync("apply", _element, light, dark, AppearanceValue, handOver);
        }

        _appliedPreset = Preset;
        _appliedPalette = Palette;
        _appliedFont = Font;
        _appliedBackdrop = DrawnBackdrop;
        _appliedAppearance = EffectiveAppearance;
        return true;
    }

    /// <summary>
    /// The backdrop the scope draws its preset over: <see cref="Backdrop"/> when the theme offers it, the
    /// one the preset carries otherwise (its own); null without a preset, which paints no backdrop.
    /// </summary>
    private OmniThemeBackdrop? DrawnBackdrop => Preset is null ? null : AppearanceChoices.EffectiveBackdrop(Preset, Backdrop);

    /// <summary>The preset as the scope draws it, over <see cref="DrawnBackdrop"/>; null without a preset.</summary>
    private OmniThemePreset? DrawnPreset => Preset?.With(DrawnBackdrop!);

    /// <summary>
    /// The script of the field a backdrop draws on a canvas rather than in CSS (`--omni-scope-canvas`,
    /// Trou noir's black hole through `omni-black-hole.js`), or null for every other one. The script reads
    /// the kind of the mode drawn (black-hole, white-hole) from the scope's tokens, so a change of mode
    /// only repaints. The canvas element is keyed by this script: a canvas keeps the first kind of context it
    /// gave (WebGL for Trou noir, 2D for the pixel stars of Octet), so another canvas backdrop gets a canvas of its own.
    /// </summary>
    private string? CanvasField => BackdropCatalog.CanvasScriptFor(DrawnPreset?.Shape.GetValueOrDefault("--omni-scope-canvas"));

    /// <summary>
    /// Starts the canvas field when the theme asks for one, restarts it when the motion setting
    /// changes, repaints it with the new colours when the tokens changed, and stops it when the theme
    /// no longer asks. While it draws, the scope drops the CSS field it replaces
    /// (`omni-theme-scope--canvas`); without WebGL the CSS field stays. A render that comes while a
    /// previous one is still talking to the script (draws in a row) does not run alongside it: the
    /// running pass goes round once more and syncs the latest parameters, so the old script is swept
    /// and disposed once.
    /// </summary>
    private async Task SyncCanvasAsync(bool tokensChanged)
    {
        _canvasTokensChanged |= tokensChanged;
        if (_canvasSyncing)
        {
            _canvasSyncAgain = true;
            return;
        }

        _canvasSyncing = true;
        try
        {
            do
            {
                _canvasSyncAgain = false;
                var changed = _canvasTokensChanged;
                _canvasTokensChanged = false;
                await SyncCanvasOnceAsync(changed);
            }
            while (_canvasSyncAgain);
        }
        finally
        {
            _canvasSyncing = false;
        }
    }

    private async Task SyncCanvasOnceAsync(bool tokensChanged)
    {
        var script = CanvasField;
        if (script is null)
        {
            if (_canvasRunning)
            {
                // The canvas is already out of the page, so it cannot be named: the module stops every
                // canvas it draws that the page dropped (a still field has no loop that would notice).
                await _canvasModule!.InvokeVoidAsync("sweep");
                _canvasRunning = false;
                _canvasDrawn = false;
                StateHasChanged();
            }

            return;
        }

        if (_canvasModule is not null && _canvasScript != script)
        {
            // Another canvas backdrop draws on a new canvas (the element is keyed by its script), so the old one
            // is already out of the page and cannot be named: the previous script stops every canvas it draws
            // that the page dropped.
            await _canvasModule.InvokeVoidAsync("sweep");
            await _canvasModule.DisposeAsync();
            _canvasModule = null;
            _canvasRunning = false;
        }

        _canvasScript = script;
        _canvasModule ??= await Internal.OmniScriptModule.ImportAsync(JavaScript, script);
        if (!_canvasRunning || _canvasMotion != BackdropMotion)
        {
            var drawn = await _canvasModule.InvokeAsync<bool>("start", _canvas, _element, BackdropMotion);
            _canvasRunning = true;
            _canvasMotion = BackdropMotion;
            if (drawn != _canvasDrawn)
            {
                _canvasDrawn = drawn;
                StateHasChanged();
            }
        }
        else if (tokensChanged)
        {
            await _canvasModule.InvokeVoidAsync("refresh", _canvas);
        }
    }

    /// <summary>
    /// The mode the scope is drawn in: <see cref="Appearance"/>, except under a theme that is only ever
    /// dark (<see cref="OmniThemePreset.DarkOnly"/>), which draws its dark half whatever the mode asked.
    /// </summary>
    internal OmniAppearance EffectiveAppearance => Preset?.FixedAppearance ?? Appearance;

    private (IReadOnlyDictionary<string, string>? Light, IReadOnlyDictionary<string, string>? Dark) Resolve()
    {
        var (light, dark) = ResolveColours();
        if (Font is null)
        {
            return (light, dark);
        }

        return (WithFont(light), WithFont(dark));
    }

    /// <summary>The chosen font sets the text and the headings, over whatever the theme said.</summary>
    private Dictionary<string, string> WithFont(IReadOnlyDictionary<string, string>? tokens)
    {
        var merged = tokens is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(tokens, StringComparer.Ordinal);
        merged["--omni-font-family"] = Font!.Family;
        merged["--omni-heading-font-family"] = Font.Family;
        return merged;
    }

    private (IReadOnlyDictionary<string, string>? Light, IReadOnlyDictionary<string, string>? Dark) ResolveColours()
    {
        if (DrawnPreset is { } drawn)
        {
            var painted = Palette is null ? drawn : drawn.With(Palette);
            return (painted.Light, painted.Dark);
        }

        return Palette is null ? (null, null) : (Palette.Light, Palette.Dark);
    }

    /// <summary>Clears the tokens written on the scope, stops following the system and releases the theme and canvas scripts, if they were loaded.</summary>
    public async ValueTask DisposeAsync()
    {
        _shared?.Dispose();
        if (_rootAttached)
        {
            await _shared!.State.DocumentRoot.DetachAsync();
        }

        if (_canvasModule is not null)
        {
            try
            {
                await _canvasModule.InvokeVoidAsync("stop", _canvas);
                await _canvasModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (_themeModule is not null)
        {
            try
            {
                await _themeModule.InvokeVoidAsync("clear", _element);
                if (_followingSystem)
                {
                    await _themeModule.InvokeVoidAsync("unfollowSystem", _element);
                }

                await _themeModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
