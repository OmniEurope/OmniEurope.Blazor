using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Reads the tokens a shipped theme and palette give, for the theme test classes.</summary>
internal static class ThemeTokens
{
    /// <summary>The tokens of the theme <paramref name="themeName"/> with the palette <paramref name="paletteName"/> in <paramref name="mode"/>.</summary>
    public static IReadOnlyDictionary<string, string> Of(string themeName, string paletteName, OmniAppearance mode) =>
        OmniThemePresets.All.Single(entry => entry.Name == themeName)
            .With(OmniThemePalettes.All.Single(entry => entry.Name == paletteName))
            .For(mode);
}
