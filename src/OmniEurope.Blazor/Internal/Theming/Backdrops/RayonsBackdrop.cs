namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Rétro's backdrop, the default of the Rétro theme: the rays of a seventies sun, twenty-four wedges of the
/// accent fanning out from a warm disc behind the page and fading before the edges of the window, turning
/// slowly, one turn in two minutes (<c>--omni-scope-motion</c>), in CSS alone: a repeating conic gradient
/// whose start angle is the scope's turning angle (<c>--omni-scope-turn</c>), under a veil that fades it
/// into the page and a disc of the warning hue with its halo.
/// </summary>
internal static class RayonsBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>The page behind the field: the deeper page a theme asks for, else the palette surface.</summary>
    private const string Page = "var(--omni-scope-page, var(--omni-color-surface))";

    /// <summary>The place of the sun, high on the window.</summary>
    private const string Centre = "50% 44%";

    private static readonly string Field = string.Join(", ",
        // The sun: a plateau and its halo.
        $"radial-gradient(circle at {Centre}, var(--omni-backdrop-sun) 0, var(--omni-backdrop-sun) 7vmax, transparent 16vmax)",
        // The veil: the rays fade into the page well before the edges.
        $"radial-gradient(circle at {Centre}, transparent 0, transparent 18vmax, {Page} 80vmax)",
        // Twenty-four wedges of 7.5 degrees, a turn per cycle; 360 / 15 is whole, so the turn has no seam.
        $"repeating-conic-gradient(from {Turn} at {Centre}, var(--omni-backdrop-ray) 0deg 7.5deg, transparent 7.5deg 15deg)");

    public static BackdropDefinition Definition { get; } = new(
        "Rayons",
        "Rayons de soleil rétro qui tournent lentement derrière la page.",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ray", "color-mix(in srgb, var(--omni-color-accent-fill) 6%, var(--omni-color-surface))"),
            ("--omni-backdrop-sun", "color-mix(in srgb, var(--omni-color-warning-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ray", "color-mix(in srgb, var(--omni-color-accent-fill) 10%, var(--omni-color-surface))"),
            ("--omni-backdrop-sun", "color-mix(in srgb, var(--omni-color-warning-fill) 14%, var(--omni-color-surface))")));
}
