using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The themes shipped with the library. A theme decides the shape (radii, borders, shadows, fonts and
/// the way buttons are drawn); each one comes painted with its default palette, and
/// <see cref="OmniThemePreset.With"/> repaints it with any other of <see cref="OmniThemePalettes.All"/>.
/// </summary>
public static class OmniThemePresets
{
    /// <summary>Every theme of the catalogue with its default palette, the default one first.</summary>
    public static IReadOnlyList<OmniThemePreset> All { get; } =
    [
        .. ThemeCatalog.All.Select(theme => ThemePresetFactory.CreatePreset(
            theme,
            OmniThemePalettes.All.Single(palette => palette.Name == theme.DefaultPalette))),
    ];
}
