namespace OmniEurope.Blazor.Components;

/// <summary>
/// A container that sets the appearance, the density and optionally the theme of everything it holds,
/// through <c>data-omni-theme</c>, <c>data-omni-density</c> and design tokens scoped to its element.
/// </summary>
public partial class OmniThemeScope
{
    private ElementReference _element;
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
    /// so the strict content security policy holds. A scope that never receives a preset nor a
    /// palette never loads the script.
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
    /// When <see cref="Preset"/>, <see cref="Palette"/>, <see cref="Font"/> or, with one of them set,
    /// <see cref="Appearance"/> changed, writes the resulting tokens on the scope through <c>omni-theme.js</c>,
    /// or clears them when none of the three is set any more. The script is loaded on first use only.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var tokensChanged = await ApplyTokensAsync();
        await SyncCanvasAsync(tokensChanged);
    }

    /// <summary>Writes or clears the tokens when the look changed; true when it did.</summary>
    private async Task<bool> ApplyTokensAsync()
    {
        var unchanged = ReferenceEquals(Preset, _appliedPreset) && ReferenceEquals(Palette, _appliedPalette) && ReferenceEquals(Font, _appliedFont);
        if (unchanged && ((Preset is null && Palette is null && Font is null) || EffectiveAppearance == _appliedAppearance))
        {
            return false;
        }

        _themeModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Theme);
        var (light, dark) = Resolve();
        if (light is null || dark is null)
        {
            await _themeModule.InvokeVoidAsync("clear", _element);
        }
        else
        {
            await _themeModule.InvokeVoidAsync("apply", _element, light, dark, EffectiveAppearance.ToString().ToLowerInvariant());
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
                _canvasRunning = false;
                _canvasDrawn = false;
                StateHasChanged();
            }

            return;
        }

        _canvasModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.BlackHole);
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
    internal OmniAppearance EffectiveAppearance => Preset?.DarkOnly == true ? OmniAppearance.Dark : Appearance;

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

    /// <summary>Clears the tokens written on the scope and releases the theme and canvas scripts, if they were loaded.</summary>
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
                await _themeModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
