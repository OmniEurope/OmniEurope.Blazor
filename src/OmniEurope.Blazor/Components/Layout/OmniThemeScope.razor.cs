namespace OmniEurope.Blazor.Components;

public partial class OmniThemeScope
{
    private ElementReference _element;
    private IJSObjectReference? _themeModule;
    private OmniThemePreset? _appliedPreset;
    private OmniThemePalette? _appliedPalette;
    private OmniThemeFont? _appliedFont;
    private OmniAppearance _appliedAppearance;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAppearance Appearance { get; set; }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var unchanged = ReferenceEquals(Preset, _appliedPreset) && ReferenceEquals(Palette, _appliedPalette) && ReferenceEquals(Font, _appliedFont);
        if (unchanged && ((Preset is null && Palette is null && Font is null) || Appearance == _appliedAppearance))
        {
            return;
        }

        _themeModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-theme.js");
        var (light, dark) = Resolve();
        if (light is null || dark is null)
        {
            await _themeModule.InvokeVoidAsync("clear", _element);
        }
        else
        {
            await _themeModule.InvokeVoidAsync("apply", _element, light, dark, Appearance.ToString().ToLowerInvariant());
        }

        _appliedPreset = Preset;
        _appliedPalette = Palette;
        _appliedFont = Font;
        _appliedAppearance = Appearance;
    }

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

    public async ValueTask DisposeAsync()
    {
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
