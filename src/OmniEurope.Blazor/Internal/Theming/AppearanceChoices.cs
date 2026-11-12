using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The theme, palette, font and backdrop lists of the appearance settings and of the appearance window, one
/// source for both: the option that stands for the default theme, the default palette, font and backdrop of each
/// theme and the "(default)" suffix of the choice a theme is drawn with.
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

    /// <summary>
    /// The random look of the appearance window and of the application menu: a theme other than the one in
    /// force, and with it a palette and a font other than those in force when their change is bound (any
    /// palette paints any theme, any font sets it). What equals the new theme's own comes back as null, as
    /// when it is picked in the list. The backdrop is not drawn: the new theme comes over its own one, as
    /// when it is picked in the list (null when the change is bound and another backdrop was chosen).
    /// </summary>
    internal static async Task RandomLookAsync(
        OmniThemePreset? currentPreset,
        OmniThemePalette? currentPalette,
        OmniThemeFont? currentFont,
        EventCallback<OmniThemePreset?> presetChanged,
        EventCallback<OmniThemePalette?> paletteChanged,
        EventCallback<OmniThemeFont?> fontChanged,
        OmniThemeBackdrop? currentBackdrop = null,
        EventCallback<OmniThemeBackdrop?> backdropChanged = default)
    {
        if (currentBackdrop is not null && backdropChanged.HasDelegate)
        {
            await backdropChanged.InvokeAsync(null);
        }

        var theme = RandomOther(OmniThemePresets.All, EffectivePreset(currentPreset));
        var preset = Theme(ThemeName(theme));
        var paletteInForce = PaletteName(currentPreset, currentPalette);
        var fontInForce = FontName(currentPreset, currentFont);
        await presetChanged.InvokeAsync(preset);
        if (paletteChanged.HasDelegate)
        {
            var palette = RandomOther(OmniThemePalettes.All, OmniThemePalettes.All.FirstOrDefault(item => item.Name == paletteInForce));
            await paletteChanged.InvokeAsync(Palette(preset, palette.Name));
        }

        if (fontChanged.HasDelegate)
        {
            var font = RandomOther(OmniThemeFonts.All, OmniThemeFonts.All.FirstOrDefault(item => item.Name == fontInForce));
            await fontChanged.InvokeAsync(Font(preset, font.Name));
        }
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

    /// <summary>
    /// The backdrop a theme is drawn over: <paramref name="backdrop"/> when the theme offers it
    /// (<see cref="OmniThemePresets.BackdropsFor"/>), the one the theme carries otherwise (its own for a theme
    /// of the catalogue, <see cref="OmniThemePreset.Backdrop"/>). Under Essentiel, which offers
    /// none, a backdrop chosen for another theme is never drawn.
    /// </summary>
    internal static OmniThemeBackdrop EffectiveBackdrop(OmniThemePreset? preset, OmniThemeBackdrop? backdrop) =>
        backdrop is not null && OmniThemePresets.BackdropsFor(EffectivePreset(preset)).Contains(backdrop)
            ? backdrop
            : EffectivePreset(preset).Backdrop;

    /// <summary>The theme in force drawn over the backdrop in force, for what it then moves or draws.</summary>
    internal static OmniThemePreset Drawn(OmniThemePreset? preset, OmniThemeBackdrop? backdrop) =>
        backdrop is null ? EffectivePreset(preset) : EffectivePreset(preset).With(EffectiveBackdrop(preset, backdrop));

    /// <summary>Whether the theme moves its colour field over the backdrop in force, so a setting that holds it still has something to hold.</summary>
    internal static bool MovesBackdrop(OmniThemePreset? preset, OmniThemeBackdrop? backdrop) =>
        Drawn(preset, backdrop).Shape.ContainsKey("--omni-scope-motion");

    /// <summary>The name of the backdrop a theme is drawn over when <paramref name="backdrop"/> is null or not offered.</summary>
    internal static string BackdropName(OmniThemePreset? preset, OmniThemeBackdrop? backdrop) => EffectiveBackdrop(preset, backdrop).Name;

    /// <summary>
    /// The backdrops the theme offers, its own one suffixed as the default; the plain page reads as
    /// <paramref name="noneLabel"/> (a word of the user's language) rather than by its catalogue name.
    /// </summary>
    internal static IReadOnlyList<OmniOption<string>> BackdropOptions(OmniThemePreset? preset, string defaultSuffix, string noneLabel)
    {
        var fallback = OmniThemePresets.DefaultBackdropFor(EffectivePreset(preset));
        return [.. OmniThemePresets.BackdropsFor(EffectivePreset(preset)).Select(backdrop =>
        {
            var label = ReferenceEquals(backdrop, OmniThemeBackdrops.None) ? noneLabel : backdrop.Name;
            return new OmniOption<string>(backdrop.Name, ReferenceEquals(backdrop, fallback) ? $"{label} ({defaultSuffix})" : label);
        })];
    }

    /// <summary>The backdrop an option names; null for the theme's own backdrop or a name the theme does not offer.</summary>
    internal static OmniThemeBackdrop? Backdrop(OmniThemePreset? preset, string? name) =>
        name == OmniThemePresets.DefaultBackdropFor(EffectivePreset(preset)).Name
            ? null
            : OmniThemePresets.BackdropsFor(EffectivePreset(preset)).FirstOrDefault(backdrop => backdrop.Name == name);

    /// <summary>The palette an option names; null for the theme's own palette or an unknown name.</summary>
    internal static OmniThemePalette? Palette(OmniThemePreset? preset, string? name) =>
        name == OmniThemePresets.DefaultPaletteFor(EffectivePreset(preset)).Name
            ? null
            : OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name);
}
