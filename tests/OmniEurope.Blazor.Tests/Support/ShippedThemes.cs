using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The shipped themes and palettes, looked up by the name the catalogue gives them.</summary>
internal static class ShippedThemes
{
    public static OmniThemePreset Preset(string name) => OmniThemePresets.All.Single(entry => entry.Name == name);

    public static OmniThemePalette Palette(string name) => OmniThemePalettes.All.Single(entry => entry.Name == name);
}
