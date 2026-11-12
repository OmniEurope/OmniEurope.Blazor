namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Octet's backdrop, the default of the Octet theme: a sky of pixel stars, squares of two CSS pixels that
/// twinkle by steps and scroll in three layers at three speeds, down and to the left, as in an eight-bit game
/// (a parallax). A background cannot draw it (squares snapped to a grid, a scroll that wraps, a twinkle of its
/// own for every star), so a script draws it on the scope's canvas (<c>omni-pixel-stars.js</c>, a 2D canvas);
/// it takes its colours from the two stops declared here (the text for most stars, the accent for one in
/// five), in both modes. Without a canvas the CSS field below shows: a handful of still stars at the same
/// colours.
/// </summary>
internal static class EtoilesBackdrop
{
    /// <summary>
    /// One still star of the field without a canvas, at (<paramref name="x"/> %, <paramref name="y"/> %): a
    /// dot of 2 px (the field is laid as images only, the scope sets no size or position of a layer, so a
    /// gradient is the one way to draw a point).
    /// </summary>
    private static string Star(int x, int y, string token) =>
        $"radial-gradient(circle at {x}% {y}%, {token} 0 1.2px, transparent 1.6px)";

    /// <summary>The field without a canvas, over the page: a few dozen still stars.</summary>
    private static readonly string Field = string.Join(", ",
        Star(6, 12, "var(--omni-backdrop-star)"), Star(14, 64, "var(--omni-backdrop-star)"), Star(19, 29, "var(--omni-backdrop-spark)"),
        Star(27, 83, "var(--omni-backdrop-star)"), Star(33, 9, "var(--omni-backdrop-star)"), Star(38, 47, "var(--omni-backdrop-star)"),
        Star(44, 72, "var(--omni-backdrop-spark)"), Star(49, 21, "var(--omni-backdrop-star)"), Star(55, 91, "var(--omni-backdrop-star)"),
        Star(61, 38, "var(--omni-backdrop-star)"), Star(66, 6, "var(--omni-backdrop-spark)"), Star(71, 58, "var(--omni-backdrop-star)"),
        Star(77, 27, "var(--omni-backdrop-star)"), Star(83, 79, "var(--omni-backdrop-star)"), Star(88, 15, "var(--omni-backdrop-star)"),
        Star(92, 52, "var(--omni-backdrop-spark)"), Star(96, 88, "var(--omni-backdrop-star)"), Star(3, 90, "var(--omni-backdrop-star)"),
        Star(23, 52, "var(--omni-backdrop-star)"), Star(58, 66, "var(--omni-backdrop-star)"), Star(80, 44, "var(--omni-backdrop-star)"));

    public static BackdropDefinition Definition { get; } = new(
        "Étoiles",
        "Étoiles en pixels qui scintillent et défilent en parallaxe derrière la page.",
        BackdropDefinition.Tokens(
            // The stops are drawn from the text and the accent over the surface (a backdrop writes no colour of
            // its own); the canvas never paints a star brighter than them, so the text reads over a star as it
            // reads on the stop, with every palette.
            ("--omni-backdrop-star", "color-mix(in srgb, var(--omni-color-text) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-spark", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            // The stars are drawn by a script on the scope's canvas; the field above is what shows without one.
            ("--omni-scope-canvas", "pixel-stars"),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-star", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop-spark", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))")),
        OmniModules.PixelStars);
}
