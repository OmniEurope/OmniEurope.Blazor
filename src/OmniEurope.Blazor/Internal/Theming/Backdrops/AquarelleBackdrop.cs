namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Papier's backdrop, the default of the Papier theme: four blots of ink, as a drop of watercolour spreads
/// on damp paper (the accent, the information, the success and the danger of the palette), each born small and
/// faint, widening slowly and fading as it spreads, one cycle of two minutes (<c>--omni-scope-motion</c>), the
/// four started a quarter of a cycle apart so there is always one being born and one dying. In CSS alone: a blot
/// is two overlapping ellipses of a radial gradient (the pigment gathers at the edge, as watercolour dries,
/// the centre is paler), and its size and its strength read the scope's sweep (<c>--omni-scope-sweep</c>,
/// 0 to 1 per cycle) wrapped at its own phase; the strength is sin(pi p), nothing at the birth and at the
/// end, so the restart is not seen.
/// </summary>
internal static class AquarelleBackdrop
{
    /// <summary>The progress of the cycle, 0 to 1 (see the stylesheet, <c>--omni-scope-sweep</c>).</summary>
    private const string Sweep = "var(--omni-scope-sweep)";

    /// <summary>
    /// One ellipse of a blot centred at (<paramref name="x"/> %, <paramref name="y"/> %), of semi-axes
    /// <paramref name="rx"/> and <paramref name="ry"/> vmax once spread, at the phase <paramref name="phase"/>
    /// (a number of the form 0.25) of the cycle: p = (sweep + phase) wrapped to 0..1, the size grows from 30 %
    /// to 100 % of the axes with p and the strength follows sin(pi p).
    /// </summary>
    private static string Ellipse(int x, int y, int rx, int ry, string phase, string ink)
    {
        var p = $"mod({Sweep} + {phase}, 1)";
        var size = $"(0.3 + 0.7 * {p})";
        var strength = $"sin({p} * 180deg)";
        return $"radial-gradient(ellipse calc({rx}vmax * {size}) calc({ry}vmax * {size}) at {x}% {y}%, "
            + $"color-mix(in srgb, {ink} calc(45% * {strength}), transparent) 0, "
            + $"color-mix(in srgb, {ink} calc(100% * {strength}), transparent) 72%, transparent 100%)";
    }

    /// <summary>A blot: a main ellipse and a smaller one beside it, so the edge is not a perfect oval.</summary>
    private static string[] Blot(int x, int y, int rx, int ry, string phase, string ink) =>
    [
        Ellipse(x, y, rx, ry, phase, ink),
        Ellipse(x + 8, y - 6, rx * 6 / 10, ry * 8 / 10, phase, ink),
    ];

    private static readonly string Field = string.Join(", ",
        Blot(22, 30, 26, 20, "0", "var(--omni-backdrop-ink)")
            .Concat(Blot(74, 22, 22, 26, "0.25", "var(--omni-backdrop-wash)"))
            .Concat(Blot(62, 76, 28, 20, "0.5", "var(--omni-backdrop-moss)"))
            .Concat(Blot(14, 82, 20, 24, "0.75", "var(--omni-backdrop-rose)")));

    public static BackdropDefinition Definition { get; } = new(
        "Aquarelle",
        "BackdropDescriptionAquarelle",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ink", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-wash", "color-mix(in srgb, var(--omni-color-info-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-moss", "color-mix(in srgb, var(--omni-color-success-fill) 13%, var(--omni-color-surface))"),
            ("--omni-backdrop-rose", "color-mix(in srgb, var(--omni-color-danger-fill) 10%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-ink", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-wash", "color-mix(in srgb, var(--omni-color-info-fill) 14%, var(--omni-color-surface))"),
            ("--omni-backdrop-moss", "color-mix(in srgb, var(--omni-color-success-fill) 17%, var(--omni-color-surface))"),
            ("--omni-backdrop-rose", "color-mix(in srgb, var(--omni-color-danger-fill) 18%, var(--omni-color-surface))")));
}
