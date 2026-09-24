namespace OmniEurope.Blazor.Components;

/// <summary>
/// A font of the catalogue: a named stack of families, laid over a theme's own font.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Font"/> to set the text and the headings of that scope;
/// null keeps the theme's default font, which <see cref="OmniThemePresets.DefaultFontFor"/> names.
/// </remarks>
/// <param name="Name">The font name as shown in a picker.</param>
/// <param name="Description">One line describing the font.</param>
/// <param name="Family">The CSS <c>font-family</c> stack, ending with a generic family.</param>
public sealed record OmniThemeFont(string Name, string Description, string Family);
