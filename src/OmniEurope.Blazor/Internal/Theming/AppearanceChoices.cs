using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The theme, palette and font lists of the appearance settings and of the appearance window, one
/// source for both: the option that stands for the default theme, the default palette and font of each
/// theme and the "(default)" suffix of the choice a theme is drawn with.
/// </summary>
internal static class AppearanceChoices
{
    /// <summary>The value of the first theme's option: choosing it clears the theme.</summary>
    internal const string DefaultTheme = "__omni_default__";

    internal static OmniThemePreset EffectivePreset(OmniThemePreset? preset) => preset ?? OmniThemePresets.All[0];

    /// <summary>Whether the theme moves its colour field, so a setting that holds it still has something to hold.</summary>
    internal static bool MovesBackdrop(OmniThemePreset? preset) => EffectivePreset(preset).Shape.ContainsKey("--omni-scope-motion");

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

    /// <summary>
    /// One entry of <paramref name="all"/> drawn at random, never <paramref name="current"/> when the
    /// list holds another: each of the others is as likely as the next.
    /// </summary>
    internal static T RandomOther<T>(IReadOnlyList<T> all, T? current)
        where T : class
    {
        var skipped = -1;
        for (var index = 0; index < all.Count && skipped < 0; index++)
        {
            skipped = ReferenceEquals(all[index], current) ? index : -1;
        }

        if (skipped < 0 || all.Count < 2)
        {
            return all[Random.Shared.Next(all.Count)];
        }

        // One draw among the others: the indexes from the current one on are shifted past it.
        var drawn = Random.Shared.Next(all.Count - 1);
        return all[drawn >= skipped ? drawn + 1 : drawn];
    }

    /// <summary>The theme an option names; null for the default one or an unknown name.</summary>
    internal static OmniThemePreset? Theme(string? name) =>
        name == DefaultTheme ? null : OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name);

    /// <summary>The name of the font a theme is drawn with when <paramref name="font"/> is null.</summary>
    internal static string FontName(OmniThemePreset? preset, OmniThemeFont? font) =>
        (font ?? OmniThemePresets.DefaultFontFor(EffectivePreset(preset))).Name;

    internal static IReadOnlyList<OmniOption<string>> FontOptions(OmniThemePreset? preset, string defaultSuffix)
    {
        var fallback = OmniThemePresets.DefaultFontFor(EffectivePreset(preset)).Name;
        return [.. OmniThemeFonts.All.Select(font => new OmniOption<string>(font.Name,
            font.Name == fallback ? $"{font.Name} ({defaultSuffix})" : font.Name))];
    }

    /// <summary>The font an option names; null for the theme's own font or an unknown name.</summary>
    internal static OmniThemeFont? Font(OmniThemePreset? preset, string? name) =>
        name == OmniThemePresets.DefaultFontFor(EffectivePreset(preset)).Name
            ? null
            : OmniThemeFonts.All.FirstOrDefault(font => font.Name == name);

    /// <summary>The palette an option names; null for the theme's own palette or an unknown name.</summary>
    internal static OmniThemePalette? Palette(OmniThemePreset? preset, string? name) =>
        name == OmniThemePresets.DefaultPaletteFor(EffectivePreset(preset)).Name
            ? null
            : OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name);
}
