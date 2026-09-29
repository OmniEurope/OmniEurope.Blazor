namespace OmniEurope.Blazor.Components;

/// <summary>
/// The light or dark mode of a theme, chosen by <see cref="OmniThemeScope.Appearance"/> and
/// <see cref="OmniAppearanceSettings"/>.
/// </summary>
public enum OmniAppearance
{
    /// <summary>Follows the light or dark setting of the system, as it changes.</summary>
    System,

    /// <summary>Always the light half of the theme.</summary>
    Light,

    /// <summary>Always the dark half of the theme.</summary>
    Dark
}
