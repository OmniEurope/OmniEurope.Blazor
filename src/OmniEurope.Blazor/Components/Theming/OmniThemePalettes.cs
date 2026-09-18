using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The palettes shipped with the library. Each one sets the accent, the four severities, the surface
/// and the text of a light and a dark half, derived until they clear the WCAG contrast ratios.
/// </summary>
public static class OmniThemePalettes
{
    /// <summary>Every palette of the catalogue, the default one first.</summary>
    public static IReadOnlyList<OmniThemePalette> All { get; } = [.. PaletteCatalog.All.Select(ThemePresetFactory.CreatePalette)];
}
