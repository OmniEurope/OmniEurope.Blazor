using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The fonts shipped with the library. Each theme is drawn with one of them, and any of them can
/// replace it through <see cref="OmniThemeScope.Font"/>.
/// </summary>
public static class OmniThemeFonts
{
    /// <summary>Every font of the catalogue, the default theme's one first.</summary>
    public static IReadOnlyList<OmniThemeFont> All { get; } =
        [.. FontCatalog.All.Select(font => new OmniThemeFont(font.Name, font.Description, font.Family))];
}
