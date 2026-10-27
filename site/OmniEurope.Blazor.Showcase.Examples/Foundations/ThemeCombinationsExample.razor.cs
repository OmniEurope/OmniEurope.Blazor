using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Four scopes with a fixed look: a theme and a palette, or one of them alone, in light or dark.</summary>
public partial class ThemeCombinationsExample
{
    /// <summary>
    /// A theme with a palette that is not its own, the same in dark, a theme with its own palette, and a
    /// palette alone over the shipped shape. Looked up by name so a renamed catalogue entry fails loudly
    /// here instead of quietly showing something else.
    /// </summary>
    private static readonly Sample[] Samples =
    [
        new(Theme("Galet"), Palette("Braise"), OmniAppearance.Light, "Theme Galet, palette Braise"),
        new(Theme("Néon"), Palette("Essentiel"), OmniAppearance.Dark, "Theme Néon, palette Essentiel"),
        new(Theme("Papier"), null, OmniAppearance.Light, "Theme Papier, its own palette"),
        new(null, Palette("Lagune"), OmniAppearance.System, "Palette Lagune, shipped shape")
    ];

    private static OmniThemePreset Theme(string name) => OmniThemePresets.All.Single(theme => theme.Name == name);

    private static OmniThemePalette Palette(string name) => OmniThemePalettes.All.Single(palette => palette.Name == name);

    /// <summary>One scope of the example.</summary>
    private sealed record Sample(OmniThemePreset? Theme, OmniThemePalette? Palette, OmniAppearance Appearance, string Caption);
}
