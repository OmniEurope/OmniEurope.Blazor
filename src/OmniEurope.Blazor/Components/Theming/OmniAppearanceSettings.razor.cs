namespace OmniEurope.Blazor.Components;

/// <summary>Controlled appearance picker. The host owns persistence and applies the values to its theme scope.</summary>
public partial class OmniAppearanceSettings
{
    private bool _scaleOpen;
    private string ScaleTitle => $"{Localize("SettingsTextSize")} / {Localize("SettingsDensity")}";
    /// <summary>Notifies the host so an enclosing menu can release its outside-click shield.</summary>
    [Parameter] public EventCallback<bool> ScaleEditorOpenChanged { get; set; }
    private Task OpenScaleAsync() => SetScaleOpenAsync(true);
    private async Task SetScaleOpenAsync(bool open)
    {
        _scaleOpen = open;
        await ScaleEditorOpenChanged.InvokeAsync(open);
    }

    private const string DefaultThemeChoice = "__omni_default__";
    [Parameter] public bool Compact { get; set; }
    [Parameter] public OmniAppearance Appearance { get; set; } = OmniAppearance.System;
    [Parameter] public EventCallback<OmniAppearance> AppearanceChanged { get; set; }
    [Parameter] public OmniThemePreset? Preset { get; set; }
    [Parameter] public EventCallback<OmniThemePreset?> PresetChanged { get; set; }
    [Parameter] public OmniThemePalette? Palette { get; set; }
    [Parameter] public EventCallback<OmniThemePalette?> PaletteChanged { get; set; }
    /// <summary>The chosen font, or null for the one the theme is drawn with.</summary>
    [Parameter] public OmniThemeFont? Font { get; set; }
    [Parameter] public EventCallback<OmniThemeFont?> FontChanged { get; set; }
    [Parameter] public int TextSizeLevel { get; set; } = 5;
    [Parameter] public EventCallback<int> TextSizeLevelChanged { get; set; }
    [Parameter] public int DensityLevel { get; set; } = 5;
    [Parameter] public EventCallback<int> DensityLevelChanged { get; set; }

    private OmniThemePreset EffectivePreset => Preset ?? OmniThemePresets.All[0];
    private OmniThemePalette DefaultPalette => OmniThemePresets.DefaultPaletteFor(EffectivePreset);
    private string ThemeName => Preset is null || ReferenceEquals(Preset, OmniThemePresets.All[0])
        ? DefaultThemeChoice : Preset.Name;
    private string PaletteName => (Palette ?? DefaultPalette).Name;
    private double TextSizeValue => TextSizeLevel;
    private double DensityValue => DensityLevel;
    private IReadOnlyList<OmniOption<string>> ThemeOptions =>
        [new(DefaultThemeChoice, $"{OmniThemePresets.All[0].Name} ({Localize("SettingsDefaultSuffix")})"),
            .. OmniThemePresets.All.Skip(1).Select(theme => new OmniOption<string>(theme.Name, theme.Name))];
    private IReadOnlyList<OmniOption<string>> PaletteOptions =>
        [.. OmniThemePalettes.All.Select(palette => new OmniOption<string>(palette.Name,
            palette.Name == DefaultPalette.Name ? $"{palette.Name} ({Localize("SettingsDefaultSuffix")})" : palette.Name))];

    private OmniThemeFont DefaultFont => OmniThemePresets.DefaultFontFor(EffectivePreset);
    private string FontName => (Font ?? DefaultFont).Name;
    private IReadOnlyList<OmniOption<string>> FontOptions =>
        [.. OmniThemeFonts.All.Select(font => new OmniOption<string>(font.Name,
            font.Name == DefaultFont.Name ? $"{font.Name} ({Localize("SettingsDefaultSuffix")})" : font.Name))];

    private Task SetFontAsync(string? name) => FontChanged.InvokeAsync(
        name == DefaultFont.Name ? null : OmniThemeFonts.All.FirstOrDefault(font => font.Name == name));

    private OmniButtonVariant ModeVariant(OmniAppearance mode) =>
        Appearance == mode ? OmniButtonVariant.Primary : OmniButtonVariant.Secondary;

    private Task SetThemeAsync(string? name) =>
        ChangeThemeAsync(name == DefaultThemeChoice ? null : OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name));

    /// <summary>
    /// A new theme comes with its own palette and font: a palette or a font picked for the previous
    /// theme is dropped, so the new one shows as it was drawn.
    /// </summary>
    private async Task ChangeThemeAsync(OmniThemePreset? preset)
    {
        await PresetChanged.InvokeAsync(preset);
        if (Palette is not null)
        {
            await PaletteChanged.InvokeAsync(null);
        }

        if (Font is not null)
        {
            await FontChanged.InvokeAsync(null);
        }
    }

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(
        name == DefaultPalette.Name ? null : OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name));

    private Task OnTextSizeSlider(double value) => TextSizeLevelChanged.InvokeAsync((int)value);
    private Task OnDensitySlider(double value) => DensityLevelChanged.InvokeAsync((int)value);
}
