namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Galet's backdrop, the default of the Galet theme: three pebbles skip on the page, each sending out
/// concentric ripples (the information hue, the accent for the third) that spread outwards and fade, one
/// period of spacing per cycle of a minute and a half (<c>--omni-scope-motion</c>), in CSS alone. A ripple is a
/// ring of soft edges in a radial gradient; its radius and its strength read the scope's sweep
/// (<c>--omni-scope-sweep</c>, 0 to 1 per cycle), the strength rising from nothing at the centre, peaking
/// half way and dying at the outer edge, so a ring is born and gone unseen and the loop has no seam.
/// </summary>
internal static class RicochetsBackdrop
{
    /// <summary>The progress of the cycle, 0 to 1 (see the stylesheet, <c>--omni-scope-sweep</c>).</summary>
    private const string Sweep = "var(--omni-scope-sweep)";

    /// <summary>
    /// The ripples of one pebble at (<paramref name="x"/>, <paramref name="y"/>): <paramref name="rings"/> rings
    /// <paramref name="period"/> px apart, each drawn soft over <paramref name="width"/> px either side of its
    /// radius, in <paramref name="ink"/>. The ring of rank k sits at radius (k + sweep) periods; its strength is
    /// 4 u (1 - u) of the ink for u = (k + sweep) / rings, between 0 and 1.
    /// </summary>
    private static string Ripples(string x, string y, int period, int rings, int width, string ink)
    {
        var stops = new List<string>();
        for (var rank = 0; rank < rings; rank++)
        {
            var progress = $"({rank} + {Sweep})";
            var radius = $"{progress} * {period}px";
            var strength = $"{progress} / {rings} * (1 - {progress} / {rings}) * 400%";
            stops.Add($"transparent calc({radius} - {width}px)");
            stops.Add($"color-mix(in srgb, {ink} calc({strength}), transparent) calc({radius})");
            stops.Add($"transparent calc({radius} + {width}px)");
        }

        return $"radial-gradient(circle at {x} {y}, {string.Join(", ", stops)})";
    }

    private static readonly string Field = string.Join(", ",
        Ripples("24%", "32%", 190, 5, 26, "var(--omni-backdrop-ring)"),
        Ripples("78%", "70%", 240, 5, 30, "var(--omni-backdrop-ring)"),
        Ripples("90%", "12%", 150, 5, 22, "var(--omni-backdrop-tide)"));

    public static BackdropDefinition Definition { get; } = new(
        "Ricochets",
        "Ondes concentriques d’un galet qui ricoche, qui s’élargissent et s’effacent derrière la page.",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ring", "color-mix(in srgb, var(--omni-color-info-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-tide", "color-mix(in srgb, var(--omni-color-accent-fill) 7%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 90s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ring", "color-mix(in srgb, var(--omni-color-info-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-tide", "color-mix(in srgb, var(--omni-color-accent-fill) 11%, var(--omni-color-surface))")));
}
