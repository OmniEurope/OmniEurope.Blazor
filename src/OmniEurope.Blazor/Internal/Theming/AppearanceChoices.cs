using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The theme and palette lists of the appearance settings and of the appearance window, one source for
/// both: the option that stands for the default theme, the default palette of each theme and the
/// "(default)" suffix of the choice a theme is drawn with.
/// </summary>
internal static class AppearanceChoices
{
    /// <summary>The value of the first theme's option: choosing it clears the theme.</summary>
    internal const string DefaultTheme = "__omni_default__";

    internal static OmniThemePreset EffectivePreset(OmniThemePreset? preset) => preset ?? OmniThemePresets.All[0];

    internal static string ThemeName(OmniThemePreset? preset) =>
        preset is null || ReferenceEquals(preset, OmniThemePresets.All[0]) ? DefaultTheme : preset.Name;

    internal static string PaletteName(OmniThemePreset? preset, OmniThemePalette? palette) =>
        (palette ?? OmniThemePresets.DefaultPaletteFor(EffectivePreset(preset))).Name;

    internal static IReadOnlyList<OmniOption<string>> ThemeOptions(string defaultSuffix) =>
        [new(DefaultTheme, $"{OmniThemePresets.All[0].Name} ({defaultSuffix})"),
            .. OmniThemePresets.All.Skip(1).Select(theme => new OmniOption<string>(theme.Name, theme.Name))];

    internal static IReadOnlyList<OmniOption<string>> PaletteOptions(OmniThemePreset? preset, string defaultSuffix)
    {
        var fallback = OmniThemePresets.DefaultPaletteFor(EffectivePreset(preset)).Name;
        return [.. OmniThemePalettes.All.Select(palette => new OmniOption<string>(palette.Name,
            palette.Name == fallback ? $"{palette.Name} ({defaultSuffix})" : palette.Name))];
    }

    /// <summary>The theme an option names; null for the default one or an unknown name.</summary>
    internal static OmniThemePreset? Theme(string? name) =>
        name == DefaultTheme ? null : OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name);

    /// <summary>The palette an option names; null for the theme's own palette or an unknown name.</summary>
    internal static OmniThemePalette? Palette(OmniThemePreset? preset, string? name) =>
        name == OmniThemePresets.DefaultPaletteFor(EffectivePreset(preset)).Name
            ? null
            : OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name);
}
