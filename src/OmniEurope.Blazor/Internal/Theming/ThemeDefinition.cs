namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a theme of the catalogue decides: its shape. A theme writes no colour of its own; a coloured
/// shadow or border is expressed from a colour token so that it follows any palette.
/// </summary>
/// <param name="Name">The theme name.</param>
/// <param name="DescriptionKey">The resource key of the one line describing the theme (<c>AppStrings</c>, every language).</param>
/// <param name="DefaultPalette">The name of the palette the theme is shown with by default.</param>
/// <param name="Shape">
/// Radii, border widths, shadows, fonts and the way buttons are drawn, laid over both modes. The field
/// behind the page is not part of it: it is the theme's backdrop (<paramref name="DefaultBackdrop"/>).
/// </param>
/// <param name="DarkShape">
/// Shape tokens laid over the dark mode only, after <paramref name="Shape"/>: a black shadow at 10 %
/// or an accent glow at 16 % that carry a light page disappear on a near-black one.
/// </param>
/// <param name="ContrastWaiver">
/// Null when the theme holds every contrast threshold of the library with every palette. Otherwise the
/// owner-declared reason why it does not (decision of 2026-09-28: a style may win over WCAG): the
/// contrast checks keep measuring it and report its shortfalls as accepted instead of failing, except
/// the focus ring, which stays mandatory everywhere.
/// </param>
/// <param name="DarkOnly">True for a theme drawn in dark mode only, whatever mode the host asks.</param>
/// <param name="DefaultBackdrop">
/// The name of the backdrop of <see cref="BackdropCatalog"/> the theme is drawn over by default, or null
/// for the plain page (<see cref="BackdropCatalog.NoneName"/>).
/// </param>
internal sealed record ThemeDefinition(
    string Name,
    string DescriptionKey,
    string DefaultPalette,
    IReadOnlyDictionary<string, string> Shape,
    IReadOnlyDictionary<string, string> DarkShape,
    string? ContrastWaiver = null,
    bool DarkOnly = false,
    string? DefaultBackdrop = null);
