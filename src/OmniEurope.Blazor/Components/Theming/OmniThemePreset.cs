namespace OmniEurope.Blazor.Components;

/// <summary>
/// A theme of the catalogue, with the token values of its light and dark halves.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Preset"/> to repaint that scope: the values are custom
/// property overrides of the shipped stylesheet, colours and shape alike, so every component inside
/// follows at once.
/// </remarks>
/// <param name="Name">The theme name as shown in a picker.</param>
/// <param name="Description">One line describing the theme.</param>
/// <param name="Light">Token overrides applied in light mode.</param>
/// <param name="Dark">Token overrides applied in dark mode.</param>
public sealed record OmniThemePreset(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Light,
    IReadOnlyDictionary<string, string> Dark)
{
    /// <summary>
    /// The overrides for one mode. <see cref="OmniAppearance.System"/> has no half of its own and
    /// yields the light one: a caller that follows the system resolves the mode first.
    /// </summary>
    public IReadOnlyDictionary<string, string> For(OmniAppearance mode) => mode is OmniAppearance.Dark ? Dark : Light;
}
