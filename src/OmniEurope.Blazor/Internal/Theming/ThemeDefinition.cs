namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a theme of the catalogue decides by hand, before the generator derives the full colour set
/// of each mode from it.
/// </summary>
/// <param name="Name">The theme name.</param>
/// <param name="Description">One line describing the theme.</param>
/// <param name="Accent">The accent colour.</param>
/// <param name="Success">The success severity.</param>
/// <param name="Info">The informational severity.</param>
/// <param name="Warning">The warning severity.</param>
/// <param name="Danger">The danger severity.</param>
/// <param name="LightSurface">The panel colour in light mode.</param>
/// <param name="LightText">The body text colour in light mode.</param>
/// <param name="DarkSurface">The panel colour in dark mode.</param>
/// <param name="DarkText">The body text colour in dark mode.</param>
/// <param name="Shape">
/// Everything a theme changes beyond colour: radii, border width, shadows, fonts and the way buttons
/// are drawn. Applied to both modes; a value may name a colour token, so one shadow follows the mode.
/// </param>
/// <param name="DarkAccent">An accent of its own for dark mode, when the light one would not carry the theme there.</param>
internal sealed record ThemeDefinition(
    string Name,
    string Description,
    string Accent,
    string Success,
    string Info,
    string Warning,
    string Danger,
    string LightSurface,
    string LightText,
    string DarkSurface,
    string DarkText,
    IReadOnlyDictionary<string, string> Shape,
    string? DarkAccent = null);
