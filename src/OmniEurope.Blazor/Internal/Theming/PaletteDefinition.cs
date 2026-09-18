namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a palette of the catalogue decides by hand, before the generator derives the colour tokens
/// of each mode from it. These are author values: the generator moves them until they clear the
/// contrast ratios, so they are never adjusted by hand to make a test pass.
/// </summary>
/// <param name="Name">The palette name.</param>
/// <param name="Description">One line describing the palette.</param>
/// <param name="Accent">The accent colour.</param>
/// <param name="Success">The success severity.</param>
/// <param name="Info">The informational severity.</param>
/// <param name="Warning">The warning severity.</param>
/// <param name="Danger">The danger severity.</param>
/// <param name="LightSurface">The panel colour in light mode.</param>
/// <param name="LightText">The body text colour in light mode.</param>
/// <param name="DarkSurface">The panel colour in dark mode.</param>
/// <param name="DarkText">The body text colour in dark mode.</param>
/// <param name="DarkAccent">
/// An accent of its own for dark mode. Without one, the light accent is lightened towards white until
/// it stands out, which washes it out: every palette of the catalogue carries one.
/// </param>
internal sealed record PaletteDefinition(
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
    string? DarkAccent = null);
