namespace OmniEurope.Blazor.Components;

/// <summary>
/// A theme of the catalogue, with the token values of its light and dark halves.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Preset"/> to repaint that scope: the values are custom
/// property overrides of the shipped stylesheet, colours and shape alike, so every component inside
/// follows at once. <see cref="With"/> keeps the shape and swaps the colours for another palette.
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
    private static readonly IReadOnlyDictionary<string, string> NoTokens = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// The shape tokens of the theme (radii, borders, shadows, fonts, buttons), laid over both halves.
    /// Empty for a preset built by hand.
    /// </summary>
    public IReadOnlyDictionary<string, string> Shape { get; init; } = NoTokens;

    /// <summary>
    /// The shape tokens laid over the dark half only, after <see cref="Shape"/>. Empty when the theme
    /// draws both modes alike.
    /// </summary>
    public IReadOnlyDictionary<string, string> DarkShape { get; init; } = NoTokens;

    /// <summary>
    /// The overrides for one mode. <see cref="OmniAppearance.System"/> has no half of its own and
    /// yields the light one: a caller that follows the system resolves the mode first.
    /// </summary>
    public IReadOnlyDictionary<string, string> For(OmniAppearance mode) => mode is OmniAppearance.Dark ? Dark : Light;

    /// <summary>
    /// This theme painted with another palette: the palette's colour tokens, then <see cref="Shape"/>
    /// over both halves, then <see cref="DarkShape"/> over the dark one. Name and description stay
    /// those of the theme.
    /// </summary>
    public OmniThemePreset With(OmniThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        return this with
        {
            Light = Merge(palette.Light, Shape),
            Dark = Merge(Merge(palette.Dark, Shape), DarkShape),
        };
    }

    private static Dictionary<string, string> Merge(IReadOnlyDictionary<string, string> tokens, IReadOnlyDictionary<string, string> over)
    {
        var merged = new Dictionary<string, string>(tokens, StringComparer.Ordinal);
        foreach (var (name, value) in over)
        {
            merged[name] = value;
        }

        return merged;
    }
}
