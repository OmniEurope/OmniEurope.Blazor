using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ThemesDemo
{
    /// <summary>
    /// A theme with a palette that is not its own, the same in dark, a theme with its own palette,
    /// and a palette alone over the shipped shape. Looked up by name so a renamed catalogue entry
    /// fails loudly here instead of quietly showing something else.
    /// </summary>
    private static readonly Sample[] Samples =
    [
        new(Theme("Galet"), Palette("Braise"), OmniAppearance.Light),
        new(Theme("Néon"), Palette("Défaut"), OmniAppearance.Dark),
        new(Theme("Papier"), null, OmniAppearance.Light),
        new(null, Palette("Lagune"), OmniAppearance.System)
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string Caption(Sample sample) => (sample.Theme, sample.Palette) switch
    {
        ({ } theme, { } palette) => Text["ThemesThemeWithPalette", theme.Name, palette.Name],
        ({ } theme, null) => Text["ThemesThemeAlone", theme.Name],
        (null, { } palette) => Text["ThemesPaletteAlone", palette.Name],
        _ => Text["ThemesShipped"]
    };

    private static OmniThemePreset Theme(string name) => OmniThemePresets.All.Single(theme => theme.Name == name);

    private static OmniThemePalette Palette(string name) => OmniThemePalettes.All.Single(palette => palette.Name == name);

    /// <summary>One scope of the demonstration.</summary>
    private sealed record Sample(OmniThemePreset? Theme, OmniThemePalette? Palette, OmniAppearance Appearance);
}
