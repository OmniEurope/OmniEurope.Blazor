namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Relief's backdrop, the default of the Relief theme: a topographic map. Three hills, each a family of
/// elliptic contour lines (the first two with a darker index line every fifth) over a faint tint that
/// rises towards its summit. The summits wander on a small orbit and the spacing of the lines breathes,
/// so the contours undulate as if the land were slowly swelling. All in CSS, moved by the scope's turning
/// angle (<c>--omni-scope-turn</c>, one turn in two minutes), so the Animation switch and the
/// reduced-motion setting freeze it.
/// </summary>
internal static class ReliefBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// A family of contour lines around a summit: a hairline every <paramref name="pitch"/> pixels, a
    /// pitch that swings by a few pixels and makes the lines flow, the farther the faster. With a
    /// <paramref name="multiple"/> above one the line is the index contour of a coarser family.
    /// </summary>
    private static string Contours(string x, string y, int pitch, int swing, int speed, int multiple, string colour)
    {
        var p = $"calc({multiple} * ({pitch}px + {swing}px * sin({Turn} * {speed})))";
        var width = multiple > 1 ? "3px" : "2px";
        return $"repeating-radial-gradient(ellipse 100px 68px at {x} {y}, transparent 0, transparent calc({p} - {width}), "
            + $"{colour} calc({p} - 1px), transparent {p})";
    }

    /// <summary>The faint tint of the high ground, a soft ellipse over the summit.</summary>
    private static string Rise(string x, string y, string radiusX, string radiusY) =>
        $"radial-gradient(ellipse {radiusX} {radiusY} at {x} {y}, var(--omni-backdrop-tint) 0, transparent 100%)";

    private static readonly string FirstX = $"calc(24% + 4% * cos({Turn}))";
    private static readonly string FirstY = $"calc(32% + 5% * sin({Turn}))";
    private static readonly string SecondX = $"calc(78% + 4% * sin({Turn}))";
    private static readonly string SecondY = $"calc(26% + 4% * cos({Turn} * 2))";
    private static readonly string ThirdX = $"calc(56% + 5% * cos({Turn} * 2))";
    private static readonly string ThirdY = $"calc(84% + 3% * sin({Turn}))";

    private static readonly string Field = string.Join(
        ", ",
        Contours(FirstX, FirstY, 26, 3, 2, 5, "var(--omni-backdrop-index)"),
        Contours(SecondX, SecondY, 30, 3, 3, 5, "var(--omni-backdrop-index)"),
        Contours(FirstX, FirstY, 26, 3, 2, 1, "var(--omni-backdrop-line)"),
        Contours(SecondX, SecondY, 30, 3, 3, 1, "var(--omni-backdrop-line)"),
        Contours(ThirdX, ThirdY, 22, 2, 2, 1, "var(--omni-backdrop-line)"),
        Rise(FirstX, FirstY, "40vmax", "28vmax"),
        Rise(SecondX, SecondY, "34vmax", "24vmax"),
        Rise(ThirdX, ThirdY, "30vmax", "20vmax"));

    public static BackdropDefinition Definition { get; } = new(
        "Relief",
        "Courbes de niveau d’une carte topographique qui ondulent lentement.",
        BackdropDefinition.Tokens(
            // The lines are a faint tint of the text over the surface, the index lines a touch firmer, the
            // high ground a breath of the accent: the page keeps its colour and its neumorphic cards.
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-text) 11%, var(--omni-color-surface))"),
            ("--omni-backdrop-index", "color-mix(in srgb, var(--omni-color-text) 17%, var(--omni-color-surface))"),
            ("--omni-backdrop-tint", "color-mix(in srgb, var(--omni-color-accent-fill) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop-index", "color-mix(in srgb, var(--omni-color-text) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-tint", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))")));
}
