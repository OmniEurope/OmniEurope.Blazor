namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Givre's backdrop, the default of the Givre theme: four very large pastel orbs of the palette with no
/// hard core (the accent as lavender, the information as sky, success as mint and a small warm glow)
/// laid as radial gradients that reach under the content, over a cool page in light mode; on the night
/// page of dark mode the orbs are deeper and the glow takes the danger hue, an aurora. Each orb drifts
/// on a small orbit, one turn in two minutes (<c>--omni-scope-motion</c>), in CSS alone.
/// </summary>
internal static class GivreBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// The colour field: the four orbs (the stops the contrast checks measure), each fading from its
    /// centre and sized on the viewport, so they overlap under the content instead of staying in the
    /// corners. Laid over a base that drifts from the page towards the information hue, pinned to the
    /// viewport by the scope, so the panes scroll over it. The centre of each orb rides a small orbit
    /// read from the scope's turning angle: with the motion off the angle is zero and the field is still.
    /// </summary>
    private const string Field =
        $"radial-gradient(circle at calc(14% + 5% * cos({Turn})) calc(10% + 6% * sin({Turn})), var(--omni-backdrop-middle) 0, transparent 46vmax), "
        + $"radial-gradient(circle at calc(90% + 4% * sin({Turn})) calc(6% + 5% * cos({Turn})), var(--omni-backdrop-glow) 0, transparent 30vmax), "
        + $"radial-gradient(circle at calc(30% + 6% * sin({Turn} * 2)) calc(66% + 5% * cos({Turn})), var(--omni-backdrop-start) 0, transparent 44vmax), "
        + $"radial-gradient(circle at calc(88% + 5% * cos({Turn})) calc(82% + 6% * sin({Turn} * 2)), var(--omni-backdrop-end) 0, transparent 48vmax), "
        + "linear-gradient(160deg, var(--omni-color-surface), color-mix(in srgb, var(--omni-color-info-fill) 8%, var(--omni-color-surface)))";

    public static BackdropDefinition Definition { get; } = new(
        "Givre",
        "Grandes taches pastel de la palette qui dérivent lentement derrière la page.",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-info-fill) 42%, var(--omni-color-surface))"),
            ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-accent-fill) 34%, var(--omni-color-surface))"),
            ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-success-fill) 34%, var(--omni-color-surface))"),
            ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-warning-fill) 36%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-info-fill) 50%, var(--omni-color-surface))"),
            ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-accent-fill) 58%, var(--omni-color-surface))"),
            ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-success-fill) 40%, var(--omni-color-surface))"),
            ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-danger-fill) 26%, var(--omni-color-surface))")));
}
