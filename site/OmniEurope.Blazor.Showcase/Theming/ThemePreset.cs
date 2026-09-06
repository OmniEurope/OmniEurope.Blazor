namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// A ready-made palette offered next to the free-form editor.
/// </summary>
/// <param name="Name">The palette name as shown in the picker.</param>
/// <param name="Description">One line describing the mood of the palette.</param>
/// <param name="Light">Token overrides applied in light mode.</param>
/// <param name="Dark">Token overrides applied in dark mode.</param>
/// <param name="Origin">Where the palette comes from, kept for attribution.</param>
/// <param name="DerivedMode">
/// The mode this project had to derive because the upstream palette does not ship it. Null when
/// both modes come from upstream. Surfaced in the picker so a derived palette is never passed off
/// as an original.
/// </param>
public sealed record ThemePreset(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Light,
    IReadOnlyDictionary<string, string> Dark,
    string Origin,
    ThemeMode? DerivedMode)
{
    /// <summary>
    /// The overrides for one mode.
    /// </summary>
    public IReadOnlyDictionary<string, string> For(ThemeMode mode) => mode is ThemeMode.Dark ? Dark : Light;
}
