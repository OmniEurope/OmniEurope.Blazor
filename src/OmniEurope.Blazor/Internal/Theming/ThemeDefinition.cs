namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a theme of the catalogue decides: its shape. A theme writes no colour of its own; a coloured
/// shadow or border is expressed from a colour token so that it follows any palette.
/// </summary>
/// <param name="Name">The theme name.</param>
/// <param name="Description">One line describing the theme.</param>
/// <param name="DefaultPalette">The name of the palette the theme is shown with by default.</param>
/// <param name="Shape">
/// Radii, border widths, shadows, fonts and the way buttons are drawn, laid over both modes.
/// </param>
/// <param name="DarkShape">
/// Shape tokens laid over the dark mode only, after <paramref name="Shape"/>: a black shadow at 10 %
/// or an accent glow at 16 % that carry a light page disappear on a near-black one.
/// </param>
internal sealed record ThemeDefinition(
    string Name,
    string Description,
    string DefaultPalette,
    IReadOnlyDictionary<string, string> Shape,
    IReadOnlyDictionary<string, string> DarkShape);
