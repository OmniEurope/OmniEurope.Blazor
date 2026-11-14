namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Néon's backdrop, the default of the Néon theme: a synthwave grid laid out in perspective under a horizon
/// that glows, scrolling towards the horizon, one row of the grid per second of a cycle of a minute and a
/// half, in CSS alone. A background has no perspective, so it is drawn: the verticals are a fan of hairlines
/// (a repeating conic gradient) that meet at the vanishing point on the horizon, the horizontals are
/// fourteen lines at heights that grow with the square of their depth, a row riding the scope's sweep
/// (<c>--omni-scope-sweep</c>, 0 to 1 per cycle) one rank towards the horizon per cycle (a row is born at the
/// bottom edge, off screen, and dies on the horizon, so the loop has no seam); a haze of the page colour hides
/// the fan above the horizon and fades the grid into it; the horizon is a thin line and an elliptical glow.
/// </summary>
internal static class HorizonBackdrop
{
    /// <summary>The progress of the cycle, 0 to 1 (see the stylesheet, <c>--omni-scope-sweep</c>).</summary>
    private const string Sweep = "var(--omni-scope-sweep)";

    /// <summary>The page behind the field: the deeper page a theme asks for, else the palette surface.</summary>
    private const string Page = "var(--omni-scope-page, var(--omni-color-surface))";

    /// <summary>The height of the horizon, from the top of the window.</summary>
    private const string Horizon = "58%";

    /// <summary>The number of rows of the grid between the horizon and the bottom edge.</summary>
    private const int Rows = 14;

    /// <summary>
    /// The horizontals: the row of rank k lies at depth u = k + 1 - sweep (from the bottom edge, u = 14, to the
    /// horizon, u = 0), at 58 % + 50 % (u / 14)^2 of the window height, 1 px thick on the horizon and 3 px
    /// at the bottom. The rows do not overlap: the closest are 7 px apart.
    /// </summary>
    private static string Rungs()
    {
        var stops = new List<string>();
        for (var rank = 0; rank < Rows; rank++)
        {
            var depth = $"({rank + 1} - {Sweep})";
            var top = $"{Horizon} + 0.255% * {depth} * {depth}";
            var bottom = $"{top} + 1px + 0.15px * {depth}";
            stops.Add($"transparent calc({top})");
            stops.Add($"var(--omni-backdrop-line) calc({top}) calc({bottom})");
            stops.Add($"transparent calc({bottom})");
        }

        return $"linear-gradient(to bottom, {string.Join(", ", stops)})";
    }

    private static readonly string Field = string.Join(", ",
        // The glow of the horizon and its thin line, over everything.
        $"radial-gradient(ellipse 70% 16% at 50% {Horizon}, var(--omni-backdrop-glow) 0, transparent 100%)",
        $"linear-gradient(to bottom, transparent calc({Horizon} - 1px), var(--omni-backdrop-line) calc({Horizon} - 1px) calc({Horizon} + 1px), transparent calc({Horizon} + 1px))",
        // The haze: the page above the horizon, fading into the grid below it.
        $"linear-gradient(to bottom, {Page} {Horizon}, transparent 82%)",
        Rungs(),
        // The fan of verticals, from the vanishing point: one hairline every 8 degrees around straight down.
        $"repeating-conic-gradient(from 176deg at 50% {Horizon}, var(--omni-backdrop-line) 0deg 0.4deg, transparent 0.4deg 8deg)");

    public static BackdropDefinition Definition { get; } = new(
        "Horizon",
        "BackdropDescriptionHorizon",
        BackdropDefinition.Tokens(
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))"),
            ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-accent-fill) 6%, var(--omni-color-surface))"),
            ("--omni-backdrop", Field),
            ("--omni-scope-motion", "omni-scope-turn 90s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-line", "color-mix(in srgb, var(--omni-color-accent-fill) 12%, var(--omni-color-surface))"),
            ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-accent-fill) 8%, var(--omni-color-surface))")));
}
