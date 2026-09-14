using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The themes shipped with the library. Each one changes colours and shape together (radii, borders,
/// shadows, fonts and the way buttons are drawn), in a light and a dark half whose colours clear the
/// WCAG contrast ratios.
/// </summary>
public static class OmniThemePresets
{
    /// <summary>Every theme of the catalogue.</summary>
    public static IReadOnlyList<OmniThemePreset> All { get; } = ThemePresetFactory.CreateAll(ThemeCatalog.All);
}
