namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Aplat's backdrop, the default of the Aplat theme: flat pastel shapes in the manner of the Memphis
/// group, no gradient and no shadow. Each shape (discs, rings, triangles, a plus sign, a half disc, a row
/// of dashes, a square) is one layer of the field, drawn in the tile it owns (<c>--omni-scope-backdrop-size</c>,
/// placed by <c>--omni-scope-backdrop-position</c>, never repeated) in one hue of the palette. Each tile
/// floats on its own small orbit read from the scope's turning angle (<c>--omni-scope-turn</c>, one turn
/// in two minutes), so the Animation switch and the reduced-motion setting freeze the shapes. All in CSS.
/// </summary>
internal static class AplatBackdrop
{
    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>One shape: its image, the tile that holds it, and where the tile floats.</summary>
    private readonly record struct Shape(string Image, string Size, string Position);

    /// <summary>The place of a tile: its anchor in the window and the reach of its drift, each axis at its own speed.</summary>
    private static string Drift(int x, int reachX, int y, int reachY, int speed) =>
        $"calc({x}% + {reachX}% * cos({Turn} * {speed})) calc({y}% + {reachY}% * sin({Turn} * {speed}))";

    private static string Disc(string hue) =>
        $"radial-gradient(circle closest-side, var(--omni-backdrop-{hue}) 96%, transparent 100%)";

    private static string Ring(string hue, int inner) =>
        $"radial-gradient(circle closest-side, transparent 0 {inner}%, var(--omni-backdrop-{hue}) {inner + 2}% 92%, transparent 96%)";

    /// <summary>A right triangle filling the half of its square tile that lies past the diagonal.</summary>
    private static string Triangle(string hue, string towards) =>
        $"linear-gradient({towards}, transparent 49.5%, var(--omni-backdrop-{hue}) 50.5%)";

    /// <summary>A bar across the middle of its square tile, horizontal or vertical (two of them, one over the other, make a plus).</summary>
    private static string Bar(string hue, string direction) =>
        $"linear-gradient({direction}transparent 36%, var(--omni-backdrop-{hue}) 37%, var(--omni-backdrop-{hue}) 63%, transparent 64%)";

    private static readonly Shape[] Shapes =
    [
        new(Disc("info"), "15vmin 15vmin", Drift(12, 4, 22, 5, 1)),
        new(Ring("accent", 56), "17vmin 17vmin", Drift(84, 4, 16, 5, 2)),
        new(Triangle("danger", "to bottom right"), "13vmin 13vmin", Drift(70, 5, 62, 4, 1)),
        new(Bar("success", string.Empty), "11vmin 11vmin", Drift(40, 5, 70, 4, 2)),
        new(Bar("success", "90deg, "), "11vmin 11vmin", Drift(40, 5, 70, 4, 2)),
        new(
            "radial-gradient(circle 8vmin at 50% 100%, var(--omni-backdrop-warning) 96%, transparent 100%)",
            "16vmin 8vmin",
            Drift(28, 5, 86, 4, 1)),
        new(
            "repeating-linear-gradient(90deg, var(--omni-backdrop-accent) 0 1.6vmin, transparent 1.6vmin 3.2vmin)",
            "14vmin 1.6vmin",
            Drift(6, 3, 58, 5, 2)),
        new(Disc("danger"), "7vmin 7vmin", Drift(52, 6, 8, 4, 2)),
        new(Triangle("warning", "to top left"), "10vmin 10vmin", Drift(94, 3, 44, 5, 1)),
        new(Ring("info", 50), "9vmin 9vmin", Drift(18, 5, 46, 4, 2)),
        new("linear-gradient(var(--omni-backdrop-success), var(--omni-backdrop-success))", "6vmin 6vmin", Drift(60, 5, 36, 5, 1)),
    ];

    public static BackdropDefinition Definition { get; } = new(
        "Aplat",
        "Formes géométriques pastel qui flottent, façon Memphis.",
        BackdropDefinition.Tokens(
            // One pastel per hue of the palette, drawn over the surface: the page keeps its colour and the
            // text keeps its reading on every shape.
            ("--omni-backdrop-accent", "color-mix(in srgb, var(--omni-color-accent-fill) 26%, var(--omni-color-surface))"),
            ("--omni-backdrop-info", "color-mix(in srgb, var(--omni-color-info-fill) 28%, var(--omni-color-surface))"),
            ("--omni-backdrop-success", "color-mix(in srgb, var(--omni-color-success-fill) 28%, var(--omni-color-surface))"),
            ("--omni-backdrop-warning", "color-mix(in srgb, var(--omni-color-warning-fill) 32%, var(--omni-color-surface))"),
            ("--omni-backdrop-danger", "color-mix(in srgb, var(--omni-color-danger-fill) 26%, var(--omni-color-surface))"),
            ("--omni-backdrop", string.Join(", ", Shapes.Select(shape => shape.Image))),
            ("--omni-scope-backdrop-size", string.Join(", ", Shapes.Select(shape => shape.Size))),
            ("--omni-scope-backdrop-position", string.Join(", ", Shapes.Select(shape => shape.Position))),
            ("--omni-scope-backdrop-repeat", "no-repeat"),
            ("--omni-scope-motion", "omni-scope-turn 120s linear infinite")),
        BackdropDefinition.Tokens(
            ("--omni-backdrop-accent", "color-mix(in srgb, var(--omni-color-accent-fill) 24%, var(--omni-color-surface))"),
            ("--omni-backdrop-info", "color-mix(in srgb, var(--omni-color-info-fill) 24%, var(--omni-color-surface))"),
            ("--omni-backdrop-success", "color-mix(in srgb, var(--omni-color-success-fill) 22%, var(--omni-color-surface))"),
            ("--omni-backdrop-warning", "color-mix(in srgb, var(--omni-color-warning-fill) 22%, var(--omni-color-surface))"),
            ("--omni-backdrop-danger", "color-mix(in srgb, var(--omni-color-danger-fill) 22%, var(--omni-color-surface))")));
}
