namespace OmniEurope.Blazor.Components;

/// <summary>
/// A font of the catalogue: a named stack of families, laid over a theme's own font.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Font"/> to set the text and the headings of that scope;
/// null keeps the theme's default font, which <see cref="OmniThemePresets.DefaultFontFor"/> names.
/// </remarks>
/// <param name="Name">The font name as shown in a picker.</param>
/// <param name="Description">
/// One line describing the font. An entry of the catalogue reads it from the package resources in the current UI
/// culture (see <see cref="Description"/>); its name stays French, never translated.
/// </param>
/// <param name="Family">The CSS <c>font-family</c> stack, ending with a generic family.</param>
public sealed record OmniThemeFont(string Name, string Description, string Family)
{
    /// <summary>
    /// One line describing the font. A font of the catalogue gives it in the current UI culture
    /// (<see cref="System.Globalization.CultureInfo.CurrentUICulture"/>), read from the package resources in the 24 languages
    /// each time it is asked for; a font built by hand, or given another description through <c>with</c>, keeps the text it was given.
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

    /// <summary>The resource key of the description of a catalogue font; null for a font built by hand.</summary>
    internal string? DescriptionKey { get; init; }
}
