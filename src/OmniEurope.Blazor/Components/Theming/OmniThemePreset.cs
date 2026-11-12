namespace OmniEurope.Blazor.Components;

/// <summary>
/// A theme of the catalogue, with the token values of its light and dark halves.
/// </summary>
/// <remarks>
/// Hand one to <see cref="OmniThemeScope.Preset"/> to repaint that scope: the values are custom
/// property overrides of the shipped stylesheet, colours and shape alike, so every component inside
/// follows at once. <see cref="With(OmniThemePalette)"/> keeps the shape and swaps the colours for another palette.
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
    /// Null when the theme holds the contrast thresholds of the library (WCAG 2.2 AA) with every palette.
    /// Otherwise the reason it does not: the theme favours its style, and an application that needs the
    /// guarantee picks another one. The focus ring stays visible in every theme.
    /// </summary>
    public string? ContrastWaiver { get; init; }

    /// <summary>
    /// True for a theme drawn in dark mode only: <see cref="OmniThemeScope"/> paints its dark
    /// half whatever <see cref="OmniThemeScope.Appearance"/> asks, and the appearance settings show the
    /// mode as fixed. False for every theme that has both halves.
    /// </summary>
    public bool DarkOnly { get; init; }

    /// <summary>The one mode a theme is always drawn in (<see cref="DarkOnly"/>), or null.</summary>
    internal OmniAppearance? FixedAppearance => DarkOnly ? OmniAppearance.Dark : null;

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

    /// <summary>
    /// The backdrop whose tokens <see cref="Shape"/> and <see cref="DarkShape"/> carry: a theme of the
    /// catalogue comes with its own (<see cref="OmniThemePresets.DefaultBackdropFor"/>), the one handed
    /// to <see cref="With(OmniThemeBackdrop)"/> after it. <see cref="OmniThemeBackdrops.None"/> for a
    /// preset built by hand.
    /// </summary>
    public OmniThemeBackdrop Backdrop { get; internal init; } = OmniThemeBackdrops.None;

    /// <summary>
    /// This theme drawn over another backdrop: the tokens of <see cref="Backdrop"/> leave both halves and
    /// both shapes, then those of <paramref name="backdrop"/> are laid over them, its dark shape over the
    /// dark half. Palette, name and every other token stay. <see cref="OmniThemeBackdrops.None"/> leaves
    /// the plain page. Which backdrops a theme offers in a picker is <see cref="OmniThemePresets.BackdropsFor"/>.
    /// </summary>
    public OmniThemePreset With(OmniThemeBackdrop backdrop)
    {
        ArgumentNullException.ThrowIfNull(backdrop);
        if (ReferenceEquals(backdrop, Backdrop))
        {
            return this;
        }

        var drawn = Backdrop.TokenNames.ToHashSet(StringComparer.Ordinal);
        return this with
        {
            Light = Merge(Without(Light, drawn), backdrop.Shape),
            Dark = Merge(Merge(Without(Dark, drawn), backdrop.Shape), backdrop.DarkShape),
            Shape = Merge(Without(Shape, drawn), backdrop.Shape),
            DarkShape = Merge(Without(DarkShape, drawn), backdrop.DarkShape),
            Backdrop = backdrop,
        };
    }

    private static Dictionary<string, string> Without(IReadOnlyDictionary<string, string> tokens, HashSet<string> names) =>
        tokens.Where(token => !names.Contains(token.Key)).ToDictionary(token => token.Key, token => token.Value, StringComparer.Ordinal);

    internal static Dictionary<string, string> Merge(IReadOnlyDictionary<string, string> tokens, IReadOnlyDictionary<string, string> over)
    {
        var merged = new Dictionary<string, string>(tokens, StringComparer.Ordinal);
        foreach (var (name, value) in over)
        {
            merged[name] = value;
        }

        return merged;
    }
}
