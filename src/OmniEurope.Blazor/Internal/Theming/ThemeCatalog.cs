namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The ten themes of the library, drawn for this project. Each one is meant to be told apart at a
/// glance, not only by its colours: radii, borders, shadows, fonts and buttons change with it.
/// </summary>
/// <remarks>
/// Fonts are system stacks only. The package ships no webfont, so a theme names families that most
/// systems carry and falls back to a generic family everywhere else.
/// </remarks>
internal static class ThemeCatalog
{
    private const string SystemSans = "system-ui, -apple-system, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif";
    private const string Serif = "Georgia, \"Iowan Old Style\", \"Palatino Linotype\", \"Times New Roman\", serif";
    private const string Mono = "ui-monospace, \"Cascadia Mono\", \"SF Mono\", Consolas, \"Liberation Mono\", monospace";
    private const string Rounded = "ui-rounded, \"SF Pro Rounded\", \"Nunito\", \"Segoe UI\", system-ui, sans-serif";
    private const string Humanist = "\"Gill Sans\", \"Gill Sans MT\", Calibri, \"Trebuchet MS\", sans-serif";
    private const string Geometric = "\"Century Gothic\", \"Futura\", \"Avenir Next\", \"Segoe UI\", sans-serif";
    private const string Technical = "Bahnschrift, \"DIN Alternate\", \"Roboto Condensed\", \"Arial Narrow\", sans-serif";
    private const string Warm = "Candara, Optima, \"Segoe UI\", sans-serif";

    private const string NoShadow = "0 0 #0000";

    public static IReadOnlyList<ThemeDefinition> All { get; } =
    [
        new("Ardoise", "Angles vifs, gris-bleu, sobre et tenu",
            "#3b5b7a", "#2f7d4f", "#2b6cb0", "#b7791f", "#c53030",
            "#f7f9fb", "#1f2933", "#141a21", "#d9e2ec",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.06em"),
                ("--omni-button-font-weight", "600"),
                ("--omni-heading-text-transform", "uppercase"), ("--omni-heading-letter-spacing", "0.05em"),
                ("--omni-card-shadow", NoShadow), ("--omni-font-family", SystemSans))),

        new("Galet", "Tout en rondeur, vert sauge, doux au toucher",
            "#5f8b6e", "#4a8f5c", "#4f86a8", "#c58f2c", "#c2574f",
            "#fafaf6", "#2e3a33", "#1b211d", "#e3ebe4",
            Shape(
                ("--omni-radius", "0.875rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.5rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "0 0.375rem 1.25rem rgb(0 0 0 / 10%)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Rounded), ("--omni-heading-font-weight", "700"))),

        new("Néon", "Nuit électrique, cyan et magenta, lueurs",
            "#008c9e", "#1f9d55", "#7c4dff", "#c28a00", "#d6246e",
            "#f5f3ff", "#1a1036", "#0b0a1a", "#e6e3ff",
            Shape(
                ("--omni-radius", "0.375rem"), ("--omni-button-radius", "0.375rem"), ("--omni-card-radius", "0.5rem"),
                ("--omni-button-shadow", "0 0 0.9rem color-mix(in srgb, var(--omni-color-accent) 60%, transparent)"),
                ("--omni-card-shadow", "0 0 1.5rem color-mix(in srgb, var(--omni-color-accent) 22%, transparent)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 45%, var(--omni-color-surface))"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.1em"),
                ("--omni-heading-letter-spacing", "0.08em"), ("--omni-heading-text-transform", "uppercase"),
                ("--omni-font-family", Technical)),
            DarkAccent: "#00e5ff"),

        new("Papier", "Encre sur papier crème, filets noirs, lettres à empattements",
            "#9b1c1c", "#2e6b30", "#1f4e79", "#8a5a00", "#b3261e",
            "#fbf8f1", "#1b1b1b", "#1c1a17", "#efe9dd",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-border-width", "2px"), ("--omni-button-border-width", "2px"),
                ("--omni-button-border-color", "var(--omni-color-text)"),
                ("--omni-color-border", "var(--omni-color-text)"),
                ("--omni-card-shadow", NoShadow), ("--omni-shadow-md", NoShadow),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Serif), ("--omni-heading-font-family", Serif))),

        new("Terracotta", "Argile chaude, ocre et brique, ombres douces",
            "#b4552f", "#5f7f1e", "#3f7f8c", "#c98a1c", "#a93636",
            "#fdf6f0", "#3b2a22", "#231915", "#f3e4d8",
            Shape(
                ("--omni-radius", "0.5rem"), ("--omni-button-radius", "0.5rem"), ("--omni-card-radius", "0.75rem"),
                ("--omni-button-shadow", "0 0.125rem 0.25rem rgb(120 60 30 / 25%)"),
                ("--omni-card-shadow", "0 0.25rem 0.875rem rgb(120 60 30 / 14%)"),
                ("--omni-font-family", Warm), ("--omni-heading-font-weight", "700"))),

        new("Rétro", "Jaune et noir, contours épais, ombres décalées",
            "#111111", "#00875a", "#2f5fd0", "#b35900", "#d62839",
            "#fff4b8", "#111111", "#121212", "#ffe45c",
            Shape(
                ("--omni-radius", "0.25rem"), ("--omni-button-radius", "0.25rem"), ("--omni-card-radius", "0.25rem"),
                ("--omni-border-width", "2px"), ("--omni-button-border-width", "2px"),
                ("--omni-button-border-color", "var(--omni-color-text)"),
                ("--omni-color-border", "var(--omni-color-text)"),
                ("--omni-button-shadow", "0.25rem 0.25rem 0 var(--omni-color-text)"),
                ("--omni-card-shadow", "0.375rem 0.375rem 0 var(--omni-color-text)"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-font-weight", "800"),
                ("--omni-font-family", Geometric), ("--omni-heading-font-weight", "800")),
            DarkAccent: "#ffe45c"),

        new("Forêt", "Sous-bois, verts profonds, lettres humanistes",
            "#2f6f4f", "#3c8d40", "#2d7d9a", "#a87a0b", "#b5433a",
            "#f3f6f1", "#1e2d24", "#0f1a14", "#d8e8dc",
            Shape(
                ("--omni-radius", "0.375rem"), ("--omni-button-radius", "0.375rem"), ("--omni-card-radius", "0.5rem"),
                ("--omni-card-shadow", NoShadow),
                ("--omni-button-font-weight", "600"), ("--omni-button-letter-spacing", "0.02em"),
                ("--omni-font-family", Humanist), ("--omni-heading-font-family", Humanist))),

        new("Lavande", "Violet poudré, grandes rondeurs, air et lumière",
            "#7c5cbf", "#3f9e7a", "#5b7fd6", "#c48d2a", "#d0506a",
            "#fbf9ff", "#2d2540", "#1b1728", "#ebe5fa",
            Shape(
                ("--omni-radius", "1rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"),
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 35%, transparent)"),
                ("--omni-card-shadow", "0 0.75rem 2rem rgb(80 50 140 / 14%)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 40%, var(--omni-color-surface))"),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", SystemSans))),

        new("Océan", "Grand large, bleus profonds, reliefs marqués",
            "#0b63ce", "#0f8f7d", "#0891b2", "#b97f00", "#d33a3a",
            "#f1f7fc", "#0f2a44", "#07192b", "#d6e8f7",
            Shape(
                ("--omni-radius", "0.25rem"), ("--omni-button-radius", "0.25rem"), ("--omni-card-radius", "0.375rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-button-shadow", "0 0.25rem 0.5rem rgb(7 40 80 / 28%)"),
                ("--omni-card-shadow", "0 0.75rem 1.75rem rgb(7 40 80 / 24%)"),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", SystemSans), ("--omni-heading-font-weight", "800"))),

        new("Mono", "Noir sur blanc, contraste total, chasse fixe",
            "#000000", "#1a7f37", "#0550ae", "#8a5a00", "#cf222e",
            "#ffffff", "#000000", "#000000", "#ffffff",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-color-border", "var(--omni-color-text)"),
                ("--omni-card-shadow", NoShadow), ("--omni-shadow-md", NoShadow),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.08em"),
                ("--omni-font-family", Mono), ("--omni-heading-font-family", Mono)),
            DarkAccent: "#ffffff")
    ];

    private static Dictionary<string, string> Shape(params (string Name, string Value)[] entries)
        => entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
}
