namespace OmniEurope.Blazor.Components;

/// <summary>Controlled appearance picker. The host owns persistence and applies the values to its theme scope.</summary>
public partial class OmniAppearanceSettings
{
    private bool _scaleOpen;
    private string ScaleTitle => ShowsControlSize
        ? $"{Localize("SettingsTextSize")} / {Localize("SettingsDensity")} / {Localize("SettingsControlSizeShort")}"
        : $"{Localize("SettingsTextSize")} / {Localize("SettingsDensity")}";
    private string ScaleSummary => ShowsControlSize
        ? $"{Localize("SettingsTextSizeShort")} {TextSizeLevel} · {Localize("SettingsDensityShort")} {DensityLevel} · {Localize("SettingsControlSizeShort")} {ControlSizeLevel}"
        : $"{Localize("SettingsTextSizeShort")} {TextSizeLevel} · {Localize("SettingsDensityShort")} {DensityLevel}";
    /// <summary>Notifies the host so an enclosing menu can release its outside-click shield.</summary>
    [Parameter] public EventCallback<bool> ScaleEditorOpenChanged { get; set; }
    private Task OpenScaleAsync() => SetScaleOpenAsync(true);
    private async Task SetScaleOpenAsync(bool open)
    {
        _scaleOpen = open;
        await ScaleEditorOpenChanged.InvokeAsync(open);
    }

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

    /// <summary>
    /// Size of the controls (buttons, fields, lists), 1 to 10 with 5 as drawn. The host applies it, for
    /// example through <c>data-oe-control-size</c> on the document root, which the control tokens read.
    /// The setting only shows once <see cref="ControlSizeLevelChanged"/> is bound, so a host that does not
    /// apply it never offers a control that does nothing.
    /// </summary>
    [Parameter] public int ControlSizeLevel { get; set; } = 5;
    [Parameter] public EventCallback<int> ControlSizeLevelChanged { get; set; }
    private bool ShowsControlSize => ControlSizeLevelChanged.HasDelegate;

    private OmniThemePreset EffectivePreset => AppearanceChoices.EffectivePreset(Preset);
    private string ThemeName => AppearanceChoices.ThemeName(Preset);
    private string PaletteName => AppearanceChoices.PaletteName(Preset, Palette);
    private IReadOnlyList<OmniOption<string>> ThemeOptions => AppearanceChoices.ThemeOptions(Localize("SettingsDefaultSuffix"));
    private IReadOnlyList<OmniOption<string>> PaletteOptions => AppearanceChoices.PaletteOptions(Preset, Localize("SettingsDefaultSuffix"));

    /// <summary>The window gets the control size change only when the host binds it, as the list does.</summary>
    private EventCallback<int> WindowControlSizeChanged => ShowsControlSize
        ? EventCallback.Factory.Create<int>(this, level => ControlSizeLevelChanged.InvokeAsync(level))
        : default;

    private OmniThemeFont DefaultFont => OmniThemePresets.DefaultFontFor(EffectivePreset);
    private string FontName => (Font ?? DefaultFont).Name;
    private IReadOnlyList<OmniOption<string>> FontOptions =>
        [.. OmniThemeFonts.All.Select(font => new OmniOption<string>(font.Name,
            font.Name == DefaultFont.Name ? $"{font.Name} ({Localize("SettingsDefaultSuffix")})" : font.Name))];

    private Task SetFontAsync(string? name) => FontChanged.InvokeAsync(
        name == DefaultFont.Name ? null : OmniThemeFonts.All.FirstOrDefault(font => font.Name == name));

    private OmniButtonVariant ModeVariant(OmniAppearance mode) =>
        Appearance == mode ? OmniButtonVariant.Primary : OmniButtonVariant.Secondary;

    private Task SetThemeAsync(string? name) => ChangeThemeAsync(AppearanceChoices.Theme(name));

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

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(AppearanceChoices.Palette(Preset, name));
}
