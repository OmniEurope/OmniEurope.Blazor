namespace OmniEurope.Blazor.Components;

/// <summary>
/// A backdrop of the catalogue: the field drawn behind the page of a theme scope (Givre's pastel orbs,
/// Trou noir's accretion disc), given as the tokens it lays over a theme, with no other shape.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Backdrop"/> to draw that scope's theme over it, or combine them
/// yourself with <see cref="OmniThemePreset.With(OmniThemeBackdrop)"/>; null keeps the theme's own,
/// which <see cref="OmniThemePresets.DefaultBackdropFor"/> names. <see cref="OmniThemeBackdrops.None"/>
/// draws the plain page. A backdrop paints from the colour tokens of the palette in force, so it follows
/// any palette, and it moves only while <see cref="OmniThemeScope.BackdropMotion"/> lets it and the
/// system does not ask for reduced motion.
/// </remarks>
/// <param name="Name">The backdrop name as shown in a picker.</param>
/// <param name="Description">
/// One line describing the backdrop. An entry of the catalogue reads it from the package resources in the current UI
/// culture (see <see cref="Description"/>); its name stays French, never translated.
/// </param>
/// <param name="Shape">The tokens laid over both modes (<c>--omni-backdrop</c>, its stops, its motion).</param>
/// <param name="DarkShape">The tokens laid over the dark mode only, after <paramref name="Shape"/>.</param>
public sealed record OmniThemeBackdrop(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Shape,
    IReadOnlyDictionary<string, string> DarkShape)
{
    /// <summary>
    /// One line describing the backdrop. A backdrop of the catalogue gives it in the current UI culture
    /// (<see cref="System.Globalization.CultureInfo.CurrentUICulture"/>), read from the package resources in the 24 languages
    /// each time it is asked for; a backdrop built by hand, or given another description through <c>with</c>, keeps the text it was given.
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

    /// <summary>The resource key of the description of a catalogue backdrop; null for a backdrop built by hand.</summary>
    internal string? DescriptionKey { get; init; }

    /// <summary>The names of every token the backdrop sets, in either mode.</summary>
    internal IEnumerable<string> TokenNames => Shape.Keys.Concat(DarkShape.Keys).Distinct(StringComparer.Ordinal);
}
