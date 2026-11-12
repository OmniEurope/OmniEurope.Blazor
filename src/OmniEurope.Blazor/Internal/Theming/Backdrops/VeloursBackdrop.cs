namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Velours's backdrop, the default of the Velours theme: a drape of the accent hue, two sets of vertical
/// folds of different widths that sway a few pixels, under two very fine line gratings laid at an angle
/// that breathes (their beats are the moiré of a watered silk) and a wide soft sheen that slides across
/// the cloth. All in CSS, moved by the scope's turning angle (<c>--omni-scope-turn</c>, one turn in two
/// minutes), so the Animation switch and the reduced-motion setting freeze it.
/// </summary>
internal static class VeloursBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// A set of vertical folds, lit then shaded then lit again over one pitch that swings by a few pixels,
    /// so the folds sway. The second set is laid translucent over the first.
    /// </summary>
    private static string Folds(int pitch, int swing, int speed, string opacity)
    {
        var p = $"calc({pitch}px + {swing}px * sin({Turn} * {speed}))";
        return $"repeating-linear-gradient(90deg, color-mix(in srgb, var(--omni-backdrop-lit) {opacity}, transparent) 0, "
            + $"color-mix(in srgb, var(--omni-backdrop-shade) {opacity}, transparent) calc({p} * 0.5), "
            + $"color-mix(in srgb, var(--omni-backdrop-lit) {opacity}, transparent) {p})";
    }

    /// <summary>
    /// A grating of hairlines. Two of them at nearly the same pitch and a nearly equal angle beat into
    /// wide fringes; the angle of each swings by a degree or two, so the fringes slide.
    /// </summary>
    private static string Grating(string angle, string pitch) =>
        $"repeating-linear-gradient({angle}, var(--omni-backdrop-weave) 0, var(--omni-backdrop-weave) 1px, transparent 2px, transparent {pitch})";

    /// <summary>The sheen: a wide diagonal band of light that sweeps the cloth from side to side.</summary>
    private static readonly string Sheen =
        $"linear-gradient(112deg, transparent calc(44% + 64% * sin({Turn}) - 22%), "
        + $"color-mix(in srgb, var(--omni-backdrop-sheen) 80%, transparent) calc(44% + 64% * sin({Turn})), "
        + $"transparent calc(44% + 64% * sin({Turn}) + 22%))";

    private static readonly string Field = string.Join(
        ", ",
        Sheen,
        Grating($"calc(7deg + 1.8deg * sin({Turn}))", "5px"),
        Grating($"calc(9deg - 1.8deg * sin({Turn}))", "5.4px"),
        Folds(262, 7, 3, "62%"),
        Folds(176, 6, 2, "100%"));

    public static BackdropDefinition Definition { get; } = new(
        "Velours",
        "Reflet moiré qui glisse sur un drapé de velours.",
        BackdropDefinition.Tokens(
            // The cloth is the accent over the surface, the folds the same hue a little deeper, the sheen
            // the page itself and the hairlines a faint tint of the text.
            ("--omni-backdrop-lit", "color-mix(in srgb, var(--omni-color-accent-fill) 6%, var(--omni-color-surface))"),
            ("--omni-backdrop-shade", "color-mix(in srgb, var(--omni-color-accent-fill) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop-sheen", "var(--omni-color-surface)"),
            ("--omni-backdrop-weave", "color-mix(in srgb, var(--omni-color-text) 7%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            // At night the lit side of a fold is the lighter one, the sheen a pale tint of the accent.
            ("--omni-backdrop-lit", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-shade", "color-mix(in srgb, var(--omni-color-accent-fill) 5%, var(--omni-color-surface))"),
            ("--omni-backdrop-sheen", "color-mix(in srgb, var(--omni-color-accent-fill) 13%, var(--omni-color-surface))"),
            ("--omni-backdrop-weave", "color-mix(in srgb, var(--omni-color-text) 7%, var(--omni-color-surface))")));
}
