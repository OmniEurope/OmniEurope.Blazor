namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Nénuphar's backdrop, the default of the Nénuphar theme: a pool seen from above. The water is a soft
/// wash from shallow to deep, three families of thin bright rings centred on points that wander (their
/// pitch breathes too) cross each other and weave the moving net of light a lagoon throws on its bed, and
/// four lily pads, flattened ellipses with a pale rim, drift on a small orbit. All in CSS, moved by the
/// scope's turning angle (<c>--omni-scope-turn</c>, one turn in two minutes), so the Animation switch and
/// the reduced-motion setting freeze it.
/// </summary>
internal static class NenupharBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// A family of bright rings around a wandering point. The pitch swings by a few pixels, which makes
    /// every ring flow slowly outward then back, faster the farther it is from the centre; where two
    /// families cross, the light gathers in a net, the way caustics do.
    /// </summary>
    private static string Caustics(string x, string y, int pitch, int swing, int speed)
    {
        var p = $"calc({pitch}px + {swing}px * sin({Turn} * {speed}))";
        return $"repeating-radial-gradient(circle at {x} {y}, transparent 0, transparent calc({p} - 6px), "
            + $"color-mix(in srgb, var(--omni-backdrop-glint) 78%, transparent) calc({p} - 2.5px), transparent {p})";
    }

    /// <summary>A lily pad: an ellipse of leaf green, paler towards its rim, with soft edges.</summary>
    private static string Pad(string x, string y, string radiusX, string radiusY) =>
        $"radial-gradient(ellipse {radiusX} {radiusY} at {x} {y}, var(--omni-backdrop-leaf) 0, var(--omni-backdrop-leaf) 86%, var(--omni-backdrop-rim) 93%, transparent 100%)";

    /// <summary>
    /// The pads on top, then the three families of rings, then the water. Each centre rides a small orbit
    /// read from the turning angle: with the motion off the angle is zero and the pool is still.
    /// </summary>
    private static readonly string Field = string.Join(
        ", ",
        Pad($"calc(16% + 5% * cos({Turn}))", $"calc(30% + 4% * sin({Turn}))", "6.5vmax", "4.2vmax"),
        Pad($"calc(82% + 4% * sin({Turn} * 2))", $"calc(24% + 5% * cos({Turn}))", "5vmax", "3.2vmax"),
        Pad($"calc(64% + 6% * cos({Turn} * 2))", $"calc(78% + 4% * sin({Turn}))", "7.5vmax", "4.6vmax"),
        Pad($"calc(26% + 5% * sin({Turn}))", $"calc(84% + 3% * cos({Turn} * 2))", "4vmax", "2.6vmax"),
        Caustics($"calc(24% + 8% * cos({Turn}))", $"calc(34% + 6% * sin({Turn}))", 46, 3, 2),
        Caustics($"calc(72% + 7% * sin({Turn}))", $"calc(58% + 8% * cos({Turn}))", 63, 3, 3),
        Caustics($"calc(46% + 9% * cos({Turn} * 2))", $"calc(92% + 5% * sin({Turn}))", 85, 4, 2),
        "linear-gradient(172deg, var(--omni-backdrop-shallow), var(--omni-backdrop-deep))");

    public static BackdropDefinition Definition { get; } = new(
        "Nénuphar",
        "Reflets de l’eau qui ondulent et quelques feuilles de nénuphar à la dérive.",
        BackdropDefinition.Tokens(
            // Every stop is drawn from the palette over the surface: the water from the information and
            // the accent hues, the leaves from the success hue; the light of the caustics is the page
            // itself, brighter than the tinted water around it.
            ("--omni-backdrop-shallow", "color-mix(in srgb, var(--omni-color-info-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-deep", "color-mix(in srgb, var(--omni-color-accent-fill) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop-glint", "var(--omni-color-surface)"),
            ("--omni-backdrop-leaf", "color-mix(in srgb, var(--omni-color-success-fill) 11%, var(--omni-color-surface))"),
            ("--omni-backdrop-rim", "color-mix(in srgb, var(--omni-color-success-fill) 15%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            // At night the water is deep and the light is a cold tint of the palette over it.
            ("--omni-backdrop-shallow", "color-mix(in srgb, var(--omni-color-info-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-deep", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-glint", "color-mix(in srgb, var(--omni-color-info-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-leaf", "color-mix(in srgb, var(--omni-color-success-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-rim", "color-mix(in srgb, var(--omni-color-success-fill) 18%, var(--omni-color-surface))")));
}
