namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The twenty themes of the library, drawn for this project. Each one is meant to be told apart at a
/// glance, not only by its colours: radii, borders, shadows, fonts and buttons change with it.
/// </summary>
/// <remarks>
/// Fonts are system stacks only. The package ships no webfont, so a theme names families that most
/// systems carry and falls back to a generic family everywhere else. A shadow that must survive dark
/// mode is drawn from a mode colour token (text, border, accent) or carries a hairline ring, since a
/// black shadow disappears on a dark page and a card without a border would disappear with it.
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
    private const string OldStyle = "\"Palatino Linotype\", Palatino, \"Book Antiqua\", \"URW Palladio L\", serif";
    private const string Didone = "Didot, \"Bodoni MT\", \"Bodoni 72\", \"Noto Serif Display\", \"Times New Roman\", serif";
    private const string Grotesk = "\"Helvetica Neue\", Helvetica, \"Arial Nova\", \"Nimbus Sans\", Arial, sans-serif";
    private const string Schoolbook = "\"Century Schoolbook\", \"New Century Schoolbook\", \"TeX Gyre Schola\", Georgia, serif";
    private const string Handwriting = "\"Segoe Print\", \"Bradley Hand\", \"Chalkboard SE\", \"Comic Neue\", cursive";
    private const string Soft = "Corbel, \"Avenir Next\", Avenir, \"Segoe UI\", sans-serif";
    private const string Transitional = "Cambria, Constantia, \"Hoefler Text\", \"Iowan Old Style\", Georgia, serif";
    private const string Trebuchet = "\"Trebuchet MS\", \"Lucida Grande\", \"Lucida Sans Unicode\", \"DejaVu Sans\", sans-serif";
    private const string Franklin = "\"Franklin Gothic Book\", \"Franklin Gothic Medium\", \"Libre Franklin\", Arial, sans-serif";
    private const string Poster = "Impact, Haettenschweiler, \"Franklin Gothic Heavy\", \"Arial Narrow Bold\", sans-serif";
    private const string Display = "\"Segoe UI Variable Display\", \"SF Pro Display\", \"Segoe UI\", \"Helvetica Neue\", system-ui, sans-serif";
    private const string Screen = "Verdana, Tahoma, \"DejaVu Sans\", \"Bitstream Vera Sans\", sans-serif";
    private const string Console = "\"Lucida Console\", \"Courier New\", ui-monospace, monospace";

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
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 9%, transparent), 0 0.375rem 1.25rem rgb(0 0 0 / 10%)"),
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
                ("--omni-shadow-md", "0.25rem 0.25rem 0 var(--omni-color-text)"),
                ("--omni-shadow-lg", "0.5rem 0.5rem 0 var(--omni-color-text)"),
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
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 48%, var(--omni-color-surface))"),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", SystemSans))),

        new("Océan", "Grand large, bleus profonds, reliefs marqués",
            "#0b63ce", "#0f8f7d", "#0891b2", "#b97f00", "#d33a3a",
            "#f1f7fc", "#0f2a44", "#07192b", "#d6e8f7",
            Shape(
                ("--omni-radius", "0.25rem"), ("--omni-button-radius", "0.25rem"), ("--omni-card-radius", "0.375rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-button-shadow", "0 0.25rem 0.5rem rgb(7 40 80 / 28%)"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 11%, transparent), 0 0.75rem 1.75rem rgb(7 40 80 / 24%)"),
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
            DarkAccent: "#ffffff"),

        new("Bonbon", "Rose acidulé, reliefs épais, touches qui s'enfoncent",
            "#d63384", "#2b9e5f", "#3b82f6", "#e08a00", "#e03131",
            "#fff6fa", "#3a0d26", "#17131f", "#fbe7f1",
            Shape(
                ("--omni-radius", "0.75rem"), ("--omni-radius-sm", "0.5rem"), ("--omni-radius-lg", "1.25rem"),
                ("--omni-button-radius", "0.875rem"), ("--omni-card-radius", "1.25rem"),
                ("--omni-border-width", "2px"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 38%, var(--omni-color-surface))"),
                ("--omni-button-shadow", "inset 0 -0.1875rem 0 rgb(0 0 0 / 22%)"),
                ("--omni-card-shadow", "0 0.3125rem 0 var(--omni-color-border)"),
                ("--omni-shadow-md", "0 0.3125rem 0 var(--omni-color-border)"),
                ("--omni-button-font-weight", "800"),
                ("--omni-font-family", SystemSans), ("--omni-heading-font-family", Rounded), ("--omni-heading-font-weight", "800")),
            DarkAccent: "#ff6fb1"),

        new("Gravure", "Ivoire et or ancien, doubles filets, capitales gravées",
            "#8a6a1c", "#3f7a3a", "#2f5f8a", "#c2571a", "#a82a2a",
            "#fdfbf5", "#1d1b16", "#12110e", "#efe7d2",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 55%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "0 0 0 0.1875rem var(--omni-color-surface), 0 0 0 0.25rem var(--omni-color-border)"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.08em"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", OldStyle), ("--omni-heading-font-family", Didone),
                ("--omni-heading-font-weight", "400"), ("--omni-heading-letter-spacing", "0.01em")),
            DarkAccent: "#d4af37"),

        new("Affiche", "Affiche suisse, vermillon franc, bandeaux et grotesque serrée",
            "#e8341c", "#00843d", "#0067b1", "#c27400", "#a3123a",
            "#ffffff", "#0b0b0b", "#0f0f10", "#f5f5f5",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-card-shadow", "inset 0 0.375rem 0 var(--omni-color-accent)"),
                ("--omni-focus-ring", "0 0 0 0.125rem var(--omni-color-surface), 0 0 0 0.25rem var(--omni-color-accent)"),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Grotesk),
                ("--omni-heading-font-weight", "800"), ("--omni-heading-letter-spacing", "-0.025em"))),

        new("Cahier", "Cahier d'écolier le jour, tableau noir la nuit, marge rouge",
            "#4b2c9b", "#2d7a46", "#2f6db5", "#b36b00", "#c8322f",
            "#fdfdfa", "#1e2340", "#1f2b27", "#eef0e6",
            Shape(
                ("--omni-radius", "0.25rem"), ("--omni-radius-sm", "0.125rem"), ("--omni-radius-lg", "0.375rem"),
                ("--omni-button-radius", "0.375rem"), ("--omni-card-radius", "0.125rem"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 32%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "inset 0.25rem 0 0 color-mix(in srgb, var(--omni-color-danger) 75%, var(--omni-color-surface))"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Schoolbook), ("--omni-heading-font-family", Handwriting)),
            DarkAccent: "#f4d35e"),

        new("Nénuphar", "Vert d'eau, coins en feuille, reflets de lagune",
            "#0e8a7a", "#2f8f46", "#2a7fb8", "#c07f00", "#d1495b",
            "#f3faf8", "#10302b", "#0b1d1a", "#d8eee8",
            Shape(
                ("--omni-radius", "0.5rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "1.125rem 0.25rem"), ("--omni-card-radius", "2rem 0.375rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-accent) 24%, transparent), 0 0.75rem 1.75rem color-mix(in srgb, var(--omni-color-accent) 16%, transparent)"),
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 30%, transparent)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Soft))),

        new("Velours", "Prune profonde, ombres de velours, titres à empattements",
            "#7b2d6e", "#2f7d5b", "#4a6fa5", "#b7791f", "#c23a3a",
            "#fbf6f9", "#2a1426", "#1a0f19", "#f2e4ee",
            Shape(
                ("--omni-radius", "0.625rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "0.5rem"), ("--omni-card-radius", "1rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 9%), 0 0 0 1px color-mix(in srgb, var(--omni-color-text) 8%, transparent), 0 0.75rem 2rem rgb(0 0 0 / 22%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 20%), 0 0.125rem 0.375rem rgb(0 0 0 / 22%)"),
                ("--omni-shadow-md", "0 0.75rem 2rem rgb(0 0 0 / 28%)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", SystemSans), ("--omni-heading-font-family", Transitional), ("--omni-heading-font-weight", "600")),
            DarkAccent: "#d98cc4"),

        new("Sable", "Sable et olive, boutons galets sur cartes nettes",
            "#66731f", "#2f7d4f", "#3a6ea5", "#b86e00", "#b3362c",
            "#f7f1e5", "#2b261c", "#1c1912", "#eee6d4",
            Shape(
                ("--omni-radius", "0.125rem"), ("--omni-radius-sm", "0.125rem"), ("--omni-radius-lg", "0.25rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "0"),
                ("--omni-card-shadow", NoShadow),
                ("--omni-button-font-weight", "600"), ("--omni-button-letter-spacing", "0.03em"),
                ("--omni-font-family", Trebuchet), ("--omni-heading-letter-spacing", "0.01em"))),

        new("Béton", "Béton brut, orange de chantier, puits creusés, capitales d'affiche",
            "#e85d04", "#2b8a3e", "#1971c2", "#b08900", "#c92a2a",
            "#ebe9e4", "#1c1c1a", "#1b1b1a", "#e8e6e1",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "inset 0 0 0 1px color-mix(in srgb, var(--omni-color-text) 16%, transparent), inset 0 0.25rem 0.625rem rgb(0 0 0 / 10%)"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.04em"),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Franklin), ("--omni-heading-font-family", Poster),
                ("--omni-heading-font-weight", "400"), ("--omni-heading-text-transform", "uppercase"),
                ("--omni-heading-letter-spacing", "0.02em"))),

        new("Givre", "Bleu glacier, filets de givre, lettres fines",
            "#1283a8", "#1f8f6a", "#3b6fd4", "#b7791f", "#d0435b",
            "#f5fafd", "#0f2a3a", "#0a1620", "#dcebf3",
            Shape(
                ("--omni-radius", "0.125rem"), ("--omni-radius-sm", "0.0625rem"), ("--omni-radius-lg", "0.25rem"),
                ("--omni-button-radius", "0.125rem"), ("--omni-card-radius", "0.25rem"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 42%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "inset 0 0 1.75rem color-mix(in srgb, var(--omni-color-accent) 10%, transparent)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 35%)"),
                ("--omni-button-font-weight", "500"), ("--omni-button-letter-spacing", "0.02em"),
                ("--omni-font-family", Display),
                ("--omni-heading-font-weight", "300"), ("--omni-heading-letter-spacing", "0.01em")),
            DarkAccent: "#8fd3ec"),

        new("Octet", "Console à huit bits, cadres en escalier, bleu cobalt",
            "#2c4bd8", "#1f8f3a", "#0e7490", "#b36b00", "#d62f2f",
            "#f4f3ee", "#1a1a2e", "#12122a", "#e6e6ff",
            Shape(
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-border-width", "2px"), ("--omni-card-border-width", "0"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 70%, var(--omni-color-surface))"),
                ("--omni-card-shadow", Staircase("0.25rem")),
                ("--omni-button-shadow", Staircase("0.1875rem")),
                ("--omni-shadow-md", "0.375rem 0.375rem 0 0 color-mix(in srgb, var(--omni-color-text) 35%, transparent)"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.06em"),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Screen), ("--omni-heading-font-family", Console),
                ("--omni-heading-text-transform", "uppercase"), ("--omni-heading-letter-spacing", "0.04em")))
    ];

    /// <summary>
    /// A frame drawn as four offset copies of the box, one per side, so the corners stay notched like
    /// the edge of a sprite. Needs a square box: a radius would round the copies.
    /// </summary>
    private static string Staircase(string step) =>
        $"0 -{step} 0 0 var(--omni-color-text), 0 {step} 0 0 var(--omni-color-text), -{step} 0 0 0 var(--omni-color-text), {step} 0 0 0 var(--omni-color-text)";

    private static Dictionary<string, string> Shape(params (string Name, string Value)[] entries)
        => entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
}
