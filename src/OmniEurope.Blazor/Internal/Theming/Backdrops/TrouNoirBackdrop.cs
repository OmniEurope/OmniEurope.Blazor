namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Trou noir's backdrop, the default of the Trou noir theme: high on the right, the horizon is a disc
/// ringed by a thin photon ring, its accretion disc tilted down to the left and bent by the hole, the
/// far side arching over the horizon, turning slowly. A WebGL shader draws it on the scope's canvas
/// (<c>omni-black-hole.js</c>): dark, the disc is drawn in light (<c>black-hole</c>); light, the same disc
/// in ink, its colours inverted (<c>white-hole</c>). Without WebGL the CSS field below shows.
/// </summary>
internal static class TrouNoirBackdrop
{
    /// <summary>
    /// The field without WebGL, where the shader puts the hole (high on the right, a shadow of 9vmax):
    /// the horizon in the page colour and its thin photon ring, with no halo around them.
    /// </summary>
    private const string Field =
        "radial-gradient(circle at 76% 26%, var(--omni-color-surface) 0 9vmax, transparent 9.1vmax), "
        + "radial-gradient(circle at 76% 26%, transparent 0 9.05vmax, var(--omni-backdrop-disk) 9.3vmax, transparent 10.4vmax)";

    public static BackdropDefinition Definition { get; } = new(
        "Trou noir",
        "Disque d’accrétion incliné autour d’un trou noir, qui tourne lentement derrière la page.",
        BackdropDefinition.Tokens(
            // The stops are drawn from the text over the surface (a backdrop writes no colour of its own), a
            // sober ring around a horizon of the page colour; the rule, the neutral badge and the text still
            // read on them with every palette.
            ("--omni-backdrop-disk", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            // The lensed hole is drawn by a shader on the scope's canvas, which a background cannot do; the
            // field above is what shows without WebGL. Light, the disc is ink on the page.
            ("--omni-scope-canvas", "white-hole"),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(("--omni-scope-canvas", "black-hole")),
        OmniModules.BlackHole);
}
