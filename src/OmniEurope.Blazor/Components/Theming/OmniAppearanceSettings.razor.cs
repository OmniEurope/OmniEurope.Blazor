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

    private OmniButtonVariant ModeVariant(OmniAppearance mode) =>
        Appearance == mode ? OmniButtonVariant.Primary : OmniButtonVariant.Secondary;

    private Task SetThemeAsync(string? name) => PresetChanged.InvokeAsync(
        name == DefaultThemeChoice ? null : OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name));

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(
        name == DefaultPalette.Name ? null : OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name));

    private Task OnTextSizeSlider(double value) => TextSizeLevelChanged.InvokeAsync((int)value);
    private Task OnDensitySlider(double value) => DensityLevelChanged.InvokeAsync((int)value);
}
