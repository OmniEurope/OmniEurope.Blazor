namespace OmniEurope.Blazor.Components;

/// <summary>
/// A palette of the catalogue: the colour tokens of its light and dark halves, with no shape.
/// </summary>
/// <remarks>
/// Any palette paints any theme: hand it to <see cref="OmniThemeScope.Palette"/> next to a
/// <see cref="OmniThemeScope.Preset"/>, or combine them yourself with <see cref="OmniThemePreset.With"/>.
/// Given alone to a scope, it changes the colours and keeps the shipped shape.
/// </remarks>
/// <param name="Name">The palette name as shown in a picker.</param>
/// <param name="Description">One line describing the palette.</param>
/// <param name="Light">Colour tokens applied in light mode.</param>
/// <param name="Dark">Colour tokens applied in dark mode.</param>
public sealed record OmniThemePalette(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Light,
    IReadOnlyDictionary<string, string> Dark)
{
    /// <summary>
    /// The tokens for one mode. <see cref="OmniAppearance.System"/> has no half of its own and yields
    /// the light one: a caller that follows the system resolves the mode first.
    /// </summary>
    public IReadOnlyDictionary<string, string> For(OmniAppearance mode) => mode is OmniAppearance.Dark ? Dark : Light;
}
