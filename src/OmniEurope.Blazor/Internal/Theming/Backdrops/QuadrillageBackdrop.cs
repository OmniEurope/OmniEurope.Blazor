namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Ardoise's backdrop, the default of the Ardoise theme: a fine architect's grid (a hairline every 24 px,
/// a stronger one every 96 px) drawn from the accent over the page, sliding slowly up and to the right,
/// one major square per cycle of a minute and a half (<c>--omni-scope-motion</c>), in CSS alone: each
/// family of lines is a repeating gradient whose first stop is shifted by the scope's sweep
/// (<c>--omni-scope-sweep</c>, 0 to 1 per cycle), and the shift is the period of the major line, so the loop
/// has no seam.
/// </summary>
internal static class QuadrillageBackdrop
{
    /// <summary>The progress of the cycle, 0 to 1 (see the stylesheet, <c>--omni-scope-sweep</c>).</summary>
    private const string Sweep = "var(--omni-scope-sweep)";

    /// <summary>The period of the major line in px: the distance the grid slides in one cycle, a whole number of fine periods.</summary>
    private const int Major = 96;

    /// <summary>The period of the fine line in px.</summary>
    private const int Fine = 24;

    /// <summary>
    /// A family of hairlines: a 1 px line of <paramref name="colour"/> every <paramref name="period"/> px along
    /// <paramref name="direction"/>, the whole pattern shifted by the sweep.
    /// </summary>
    private static string Lines(string direction, int period, string colour)
    {
        var start = $"{Sweep} * {Major}px";
        return $"repeating-linear-gradient({direction}, {colour} calc({start}) calc({start} + 1px), transparent calc({start} + 1px) calc({start} + {period}px))";
    }

    /// <summary>The colour field: the major lines over the fine ones, vertical then horizontal.</summary>
    private static readonly string Field = string.Join(", ",
        Lines("90deg", Major, "var(--omni-backdrop-major)"),
        Lines("0deg", Major, "var(--omni-backdrop-major)"),
        Lines("90deg", Fine, "var(--omni-backdrop-line)"),
        Lines("0deg", Fine, "var(--omni-backdrop-line)"));

    public static BackdropDefinition Definition { get; } = new(
        "Quadrillage",
        "BackdropDescriptionQuadrillage",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-accent-fill) 5%, var(--omni-color-surface))"),
            ("--omni-backdrop-major", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 90s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-major", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))")));
}
