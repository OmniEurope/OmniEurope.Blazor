namespace OmniEurope.Blazor.Components;

/// <summary>
/// A palette of the catalogue: the colour tokens of its light and dark halves, with no shape.
/// </summary>
/// <remarks>
/// Any palette paints any theme: hand it to <see cref="OmniThemeScope.Palette"/> next to a
/// <see cref="OmniThemeScope.Preset"/>, or combine them yourself with <see cref="OmniThemePreset.With(OmniThemePalette)"/>.
/// Given alone to a scope, it changes the colours and keeps the shipped shape.
/// </remarks>
/// <param name="Name">The palette name as shown in a picker.</param>
/// <param name="Description">
/// One line describing the palette. An entry of the catalogue reads it from the package resources in the current UI
/// culture (see <see cref="Description"/>); its name stays French, never translated.
/// </param>
/// <param name="Light">Colour tokens applied in light mode.</param>
/// <param name="Dark">Colour tokens applied in dark mode.</param>
public sealed record OmniThemePalette(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Light,
    IReadOnlyDictionary<string, string> Dark)
{
    /// <summary>
    /// One line describing the palette. A palette of the catalogue gives it in the current UI culture
    /// (<see cref="System.Globalization.CultureInfo.CurrentUICulture"/>), read from the package resources in the 24 languages
    /// each time it is asked for; a palette built by hand, or given another description through <c>with</c>, keeps the text it was given.
    /// </summary>
    public string Description
    {
        get => DescriptionKey is { } key ? CatalogDescriptions.Describe(key, field) : field;
        init
        {
            field = value;
            DescriptionKey = null;
        }
    } = Description;

    /// <summary>The resource key of the description of a catalogue palette; null for a palette built by hand.</summary>
    internal string? DescriptionKey { get; init; }

    /// <summary>
    /// The tokens for one mode. <see cref="OmniAppearance.System"/> has no half of its own and yields
    /// the light one: a caller that follows the system resolves the mode first.
    /// </summary>
    public IReadOnlyDictionary<string, string> For(OmniAppearance mode) => mode is OmniAppearance.Dark ? Dark : Light;
}
