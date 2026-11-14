namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Épure's backdrop, the default of the Épure theme: a fine dot screen, like a halftone proof, in two
/// grids (a close one and a wide one of slightly larger dots). Every dot swells and shrinks by a few
/// tenths of a pixel, the two grids in opposite phase, one breath in twenty seconds: a gentle pulse,
/// no change of luminance anywhere near three times a second. Each grid is one layer of the field in its
/// own tile (<c>--omni-scope-backdrop-size</c>); the pulse reads the scope's turning angle
/// (<c>--omni-scope-turn</c>, one turn in two minutes), so the Animation switch and the reduced-motion
/// setting freeze it. All in CSS.
/// </summary>
internal static class EpureBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// A dot at the centre of its tile, soft at its edge. The radius is a length of the tile, so a
    /// radius that follows the angle pulses every dot of the grid at once.
    /// </summary>
    private static string Dots(string hue, double radius, double swing, string phase)
    {
        var r = $"calc({radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}px + {swing.ToString(System.Globalization.CultureInfo.InvariantCulture)}px * sin({Turn} * 6{phase}))";
        return $"radial-gradient(circle at 50% 50%, var(--omni-backdrop-{hue}) 0, var(--omni-backdrop-{hue}) {r}, transparent calc({r} + 0.8px))";
    }

    private static readonly string Field = string.Join(
        ", ",
        Dots("dot", 1.0, 0.4, string.Empty),
        Dots("mark", 1.5, 0.45, " + 180deg"));

    public static BackdropDefinition Definition { get; } = new(
        "Épure",
        "BackdropDescriptionEpure",
        BackdropDefinition.Tokens(
            // The dots are a faint tint of the text over the surface, the wide grid a touch firmer: the
            // black on white of the theme, a screen only a close look picks out.
            ("--omni-backdrop-dot", "color-mix(in srgb, var(--omni-color-text) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-mark", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-backdrop-size", "14px 14px, 42px 42px"),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-dot", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop-mark", "color-mix(in srgb, var(--omni-color-text) 10%, var(--omni-color-surface))")));
}
