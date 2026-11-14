namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Halo's backdrop, the default of the Halo theme: eight soft discs of light, blurred like the bokeh of a
/// night street (the accent, the information, the success and the warning of the palette), that drift on small
/// orbits and breathe (their radius swells and shrinks) over one cycle of two minutes
/// (<c>--omni-scope-motion</c>), in CSS alone. Each disc is a radial gradient with a faint centre, a brighter
/// rim and a soft edge, as a lens draws it; its position and radius read the scope's turning angle
/// (<c>--omni-scope-turn</c>) through whole multiples, so the loop has no seam.
/// </summary>
internal static class BokehBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// One disc of light centred near (<paramref name="x"/> %, <paramref name="y"/> %), of a radius of
    /// <paramref name="size"/> tenths of vmax, breathing by <paramref name="swell"/> tenths of vmax
    /// either way, drifting on an orbit of 4 %: its orbit turns <paramref name="drift"/> times and its breath
    /// <paramref name="breath"/> times per cycle, both started at <paramref name="phase"/> degrees.
    /// </summary>
    private static string Disc(int x, int y, int size, int swell, int drift, int breath, int phase, string token)
    {
        var radius = $"{size / 10}.{size % 10}vmax + {swell / 10}.{swell % 10}vmax * sin({Turn} * {breath} + {phase}deg)";
        return $"radial-gradient(circle at calc({x}% + 4% * sin({Turn} * {drift} + {phase}deg)) calc({y}% + 4% * cos({Turn} * {drift} + {phase}deg)), "
            + $"color-mix(in srgb, {token} 60%, transparent) 0, {token} calc(({radius}) * 0.8), transparent calc({radius}))";
    }

    private static readonly string Field = string.Join(", ",
        Disc(12, 20, 150, 20, 1, 3, 0, "var(--omni-backdrop-rose)"),
        Disc(34, 62, 110, 15, 2, 2, 40, "var(--omni-backdrop-sky)"),
        Disc(58, 14, 90, 12, 1, 3, 100, "var(--omni-backdrop-mint)"),
        Disc(82, 38, 140, 18, 2, 2, 160, "var(--omni-backdrop-rose)"),
        Disc(70, 82, 120, 16, 1, 3, 220, "var(--omni-backdrop-amber)"),
        Disc(10, 86, 100, 14, 2, 3, 280, "var(--omni-backdrop-sky)"),
        Disc(46, 38, 70, 10, 1, 2, 320, "var(--omni-backdrop-amber)"),
        Disc(92, 8, 80, 10, 2, 3, 60, "var(--omni-backdrop-mint)"));

    public static BackdropDefinition Definition { get; } = new(
        "Bokeh",
        "BackdropDescriptionBokeh",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-rose", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-sky", "color-mix(in srgb, var(--omni-color-info-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-mint", "color-mix(in srgb, var(--omni-color-success-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-amber", "color-mix(in srgb, var(--omni-color-warning-fill) 13%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-rose", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-sky", "color-mix(in srgb, var(--omni-color-info-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-mint", "color-mix(in srgb, var(--omni-color-success-fill) 16%, var(--omni-color-surface))"),
            ("--omni-backdrop-amber", "color-mix(in srgb, var(--omni-color-warning-fill) 14%, var(--omni-color-surface))")));
}
