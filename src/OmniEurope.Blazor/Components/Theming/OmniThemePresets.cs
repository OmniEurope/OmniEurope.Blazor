using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The themes shipped with the library. A theme decides the shape (radii, borders, shadows, fonts and
/// the way buttons are drawn); each one comes painted with its default palette and drawn over its
/// default backdrop. <see cref="OmniThemePreset.With(OmniThemePalette)"/> repaints it with any other of
/// <see cref="OmniThemePalettes.All"/>, and <see cref="OmniThemePreset.With(OmniThemeBackdrop)"/> draws it
/// over any of <see cref="BackdropsFor"/>.
/// </summary>
public static class OmniThemePresets
{
    /// <summary>The palette shipped with the selected theme, or with the first theme when unset.</summary>
    public static OmniThemePalette DefaultPaletteFor(OmniThemePreset? theme)
    {
        var name = ThemeCatalog.All.First(item => item.Name == (theme ?? All[0]).Name).DefaultPalette;
        return OmniThemePalettes.All.First(item => item.Name == name);
    }

    /// <summary>
    /// The font the selected theme is drawn with, or the first theme's when unset; the first font of
    /// <see cref="OmniThemeFonts.All"/> for a theme that is not in the catalogue. The catalogue font
    /// whose stack the theme sets as its text font.
    /// </summary>
    public static OmniThemeFont DefaultFontFor(OmniThemePreset? theme)
    {
        var definition = Definition(theme);
        return definition is not null && definition.Shape.TryGetValue("--omni-font-family", out var family)
            && OmniThemeFonts.All.FirstOrDefault(font => font.Family == family) is { } font
                ? font
                : OmniThemeFonts.All[0];
    }

    /// <summary>
    /// The backdrop the selected theme is drawn over, or the first theme's when unset:
    /// <see cref="OmniThemeBackdrops.None"/> for Essentiel, for every theme without a backdrop of its own
    /// and for a theme that is not in the catalogue.
    /// </summary>
    public static OmniThemeBackdrop DefaultBackdropFor(OmniThemePreset? theme)
    {
        var name = Definition(theme)?.DefaultBackdrop;
        return name is null ? OmniThemeBackdrops.None : OmniThemeBackdrops.All.First(item => item.Name == name);
    }

    /// <summary>
    /// The backdrops a picker offers under the selected theme, or under the first one when unset: every
    /// backdrop of <see cref="OmniThemeBackdrops.All"/>, Essentiel included (owner decision of 2026-11-12,
    /// which replaces the one that offered the plain page alone under Essentiel). Essentiel is still drawn
    /// over the plain page by default (<see cref="DefaultBackdropFor"/>).
    /// </summary>
    /// <param name="theme">The selected theme; null for the first one of the catalogue.</param>
    /// <returns>The whole catalogue of backdrops, whatever the theme.</returns>
    public static IReadOnlyList<OmniThemeBackdrop> BackdropsFor(OmniThemePreset? theme) => OmniThemeBackdrops.All;

    /// <summary>Every theme of the catalogue with its default palette and backdrop, the default one first.</summary>
    public static IReadOnlyList<OmniThemePreset> All { get; } =
    [
        .. ThemeCatalog.All.Select(theme => ThemePresetFactory.CreatePreset(
            theme,
            OmniThemePalettes.All.Single(palette => palette.Name == theme.DefaultPalette),
            theme.DefaultBackdrop is null ? OmniThemeBackdrops.None : OmniThemeBackdrops.All.Single(backdrop => backdrop.Name == theme.DefaultBackdrop))),
    ];

    /// <summary>The catalogue entry of a theme, by its name; null for a theme built by hand under another name.</summary>
    private static ThemeDefinition? Definition(OmniThemePreset? theme) =>
        ThemeCatalog.All.FirstOrDefault(item => item.Name == (theme ?? All[0]).Name);
}
