namespace OmniEurope.Blazor.Components;

/// <summary>
/// A container that sets the appearance, the density and optionally the theme of everything it holds,
/// through <c>data-omni-theme</c>, <c>data-omni-density</c> and design tokens scoped to its element.
/// The mode actually drawn, light or dark, is carried by <c>data-omni-theme-resolved</c>, also when
/// <see cref="Appearance"/> follows the system.
/// </summary>
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
    private OmniAppearance _appliedAppearance;
    private ElementReference _canvas;
    private IJSObjectReference? _canvasModule;
    private bool _canvasRunning;
    private bool _canvasDrawn;
    private bool _canvasMotion;
    private string? _canvasKind;

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
    /// (<see cref="OmniThemePreset.With"/>); without one, only the colours change and the shipped
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
    /// Whether a theme that moves its colour field (Givre, Trou noir) may move it; true by default.
    /// False holds the field still (<c>data-omni-backdrop-motion="off"</c>). A theme without a moving
    /// field ignores it, and a system set to reduce motion holds the field still whatever the value.
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
        var tokensChanged = await ApplyTokensAsync();
        await SyncResolvedModeAsync();
        await WriteSnapshotAsync();
        await SyncCanvasAsync(tokensChanged);
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
        _themeModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Theme);

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

        var state = new SnapshotState(key, EffectiveAppearance, Density, BackdropMotion, Preset, Palette, Font);
        if (state == _writtenSnapshot)
        {
            return;
        }

        var (light, dark) = Resolve();
        await (await LoadThemeModuleAsync()).InvokeVoidAsync(
            "snapshot", _element, key, SnapshotVersion, AppearanceValue, Density.ToString().ToLowerInvariant(), BackdropMotion, light, dark);
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
        OmniThemeFont? Font);

    /// <summary>Writes or clears the tokens when the look changed; true when it did.</summary>
    private async Task<bool> ApplyTokensAsync()
    {
        var unchanged = ReferenceEquals(Preset, _appliedPreset) && ReferenceEquals(Palette, _appliedPalette) && ReferenceEquals(Font, _appliedFont);
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
        _appliedAppearance = EffectiveAppearance;
        return true;
    }

    /// <summary>
    /// The field a theme draws on a canvas rather than in CSS (`--omni-scope-canvas`, Trou noir's black
    /// hole through `omni-black-hole.js`), or null for every other theme.
    /// </summary>
    private string? CanvasField => Preset?.Shape.GetValueOrDefault("--omni-scope-canvas") is { } kind && kind != "none" ? kind : null;

    /// <summary>
    /// Starts the canvas field when the theme asks for one, restarts it when the motion setting
    /// changes, repaints it with the new colours when the tokens changed, and stops it when the theme
    /// no longer asks. While it draws, the scope drops the CSS field it replaces
    /// (`omni-theme-scope--canvas`); without WebGL the CSS field stays.
    /// </summary>
    private async Task SyncCanvasAsync(bool tokensChanged)
    {
        if (CanvasField is null)
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

        _canvasModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.BlackHole);
        if (!_canvasRunning || _canvasMotion != BackdropMotion || _canvasKind != CanvasField)
        {
            var drawn = await _canvasModule.InvokeAsync<bool>("start", _canvas, _element, BackdropMotion, CanvasField);
            _canvasRunning = true;
            _canvasMotion = BackdropMotion;
            _canvasKind = CanvasField;
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
    /// dark or only ever light (<see cref="OmniThemePreset.DarkOnly"/>, <see cref="OmniThemePreset.LightOnly"/>),
    /// which draws that half whatever the mode asked.
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
        if (Preset is not null)
        {
            var painted = Palette is null ? Preset : Preset.With(Palette);
            return (painted.Light, painted.Dark);
        }

        return Palette is null ? (null, null) : (Palette.Light, Palette.Dark);
    }

    /// <summary>Clears the tokens written on the scope, stops following the system and releases the theme and canvas scripts, if they were loaded.</summary>
    public async ValueTask DisposeAsync()
    {
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
