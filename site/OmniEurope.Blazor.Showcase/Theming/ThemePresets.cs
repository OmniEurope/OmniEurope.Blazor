namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The palettes the customizer offers, both modes built for each.
/// </summary>
public static class ThemePresets
{
    /// <summary>Every palette of the catalogue.</summary>
    public static IReadOnlyList<ThemePreset> All { get; } = ThemePresetFactory.CreateAll(BootswatchPalettes.All);
}
