using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The look choices the appearance suites start from, looked up by their shipped name, and the
/// theme, palette and font an appearance window is handed as parameters, and the flagged language list of the app menu.
/// </summary>
internal static class AppearanceTestChoices
{
    internal static OmniThemePreset Theme(string name) => ShippedThemes.Preset(name);

    internal static OmniThemePalette Palette(string name) => ShippedThemes.Palette(name);

    internal static OmniThemeFont Font(string name) => OmniThemeFonts.All.Single(item => item.Name == name);

    internal static ComponentParameterCollectionBuilder<OmniAppearanceWindow> WithLook(
        this ComponentParameterCollectionBuilder<OmniAppearanceWindow> parameters,
        OmniThemePreset? theme,
        OmniThemePalette? palette,
        OmniThemeFont? font) =>
        parameters
            .Add(component => component.Preset, theme)
            .Add(component => component.Palette, palette)
            .Add(component => component.Font, font);

    internal static ComponentParameterCollectionBuilder<OmniAppMenu> WithFlaggedFrenchAndEnglish(
        this ComponentParameterCollectionBuilder<OmniAppMenu> parameters) =>
        parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.ShowFlags, true);
}
