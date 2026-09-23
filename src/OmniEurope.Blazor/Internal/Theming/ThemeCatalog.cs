namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The ten themes of the library, drawn for this project. A theme decides the shape only: radii,
/// borders, shadows, fonts and the way buttons are drawn and pressed. Values are those of the reference
/// mockup (<c>plans/PLAN-008-maquette-themes.html</c>, constant <c>THEMES</c>).
/// </summary>
/// <remarks>
/// A shape writes no colour of its own: a coloured shadow or border is drawn from a colour token, so it
/// follows any palette. Only neutral shadows (<c>rgb(0 0 0 / x%)</c>, <c>rgb(255 255 255 / x%)</c>) are
/// literal. Fonts are system stacks only: the package ships no webfont.
/// </remarks>
internal static class ThemeCatalog
{
    private const string Sans = "system-ui, -apple-system, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif";
    private const string Serif = "Georgia, \"Iowan Old Style\", \"Palatino Linotype\", \"Times New Roman\", serif";
    private const string Rounded = "ui-rounded, \"SF Pro Rounded\", Nunito, \"Segoe UI\", system-ui, sans-serif";
    private const string Technical = "Bahnschrift, \"DIN Alternate\", \"Roboto Condensed\", \"Arial Narrow\", sans-serif";
    private const string Geometric = "\"Century Gothic\", Futura, \"Avenir Next\", \"Segoe UI\", sans-serif";
    private const string Soft = "Corbel, \"Avenir Next\", Avenir, \"Segoe UI\", sans-serif";
    private const string Transitional = "Cambria, Constantia, \"Hoefler Text\", \"Iowan Old Style\", Georgia, serif";
    private const string Screen = "Verdana, Tahoma, \"DejaVu Sans\", \"Bitstream Vera Sans\", sans-serif";
    private const string Console = "\"Lucida Console\", \"Courier New\", ui-monospace, monospace";
    private const string NoShadow = "0 0 #0000";

    // One radius everywhere for Défaut, the button's: small, medium, large, cards, alerts and floating
    // surfaces are all 2.5 px. Chosen smaller than the 4 px of the reference production on purpose.
    private const string DefaultRadius = "0.15625rem";

    public static IReadOnlyList<ThemeDefinition> All { get; } =
    [
        new("Essentiel", "Allure de la production Aetheus, rayon de 2,5 px, élévation discrète, sans empattement.", "Essentiel",
            Shape(
                ("--omni-radius", DefaultRadius), ("--omni-radius-sm", DefaultRadius), ("--omni-radius-lg", DefaultRadius),
                ("--omni-button-radius", DefaultRadius),
                // Button elevation reads two mode tokens: a denser black shadow in dark mode, where
                // 22 % no longer shows, and a light top-edge highlight that only exists there.
                ("--omni-button-shadow", "inset 0 1px 0 var(--omni-elevation-highlight), 0 0.0625rem 0.125rem var(--omni-elevation-shadow)"),
                // Trial (PLAN-008 T14), removable as a block: the layer of cards, tiles, alerts,
                // notifications, dialogs and menus.
                ("--omni-card-radius", DefaultRadius), ("--omni-alert-radius", DefaultRadius),
                ("--omni-card-background", "var(--omni-layer-fill)"),
                ("--omni-card-border-color", "var(--omni-layer-stroke)"),
                ("--omni-card-shadow", "var(--omni-layer-shadow)"),
                ("--omni-overlay-background", "var(--omni-layer-acrylic)"),
                ("--omni-overlay-filter", "blur(28px) saturate(1.3)"),
                ("--omni-overlay-shadow", "var(--omni-layer-shadow-deep)"),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", Sans)),
            Shape()),

        new("Ardoise", "Angles vifs, aucune ombre, capitales espacées.", "Océan",
            Shape(
                // Press: no movement, the button sinks like a flat key.
                ("--omni-button-press-transform", "none"), ("--omni-button-press-shadow", "inset 0 0.125rem 0.25rem rgb(0 0 0 / 30%)"),
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.06em"),
                ("--omni-button-font-weight", "600"),
                ("--omni-heading-text-transform", "uppercase"), ("--omni-heading-letter-spacing", "0.05em"),
                ("--omni-card-shadow", NoShadow), ("--omni-font-family", Sans)),
            Shape()),

        new("Galet", "Tout en rondeur, boutons pilule, liseré et ombre diffuse à la place de la bordure.", "Forêt",
            Shape(
                // Press: the pebble squashes a little under the finger.
                ("--omni-button-press-transform", "scale(0.95)"),
                ("--omni-radius", "0.875rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.5rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 9%, transparent), 0 0.375rem 1.25rem rgb(0 0 0 / 10%)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Rounded), ("--omni-heading-font-weight", "700")),
            Shape(
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-text) 5%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 12%, transparent), 0 0.5rem 1.5rem rgb(0 0 0 / 45%)"))),

        new("Halo", "Grandes rondeurs et halos colorés tirés de l’accent.", "Lavande",
            Shape(
                // Press: the halo tightens under the button, which shrinks a touch.
                ("--omni-button-press-transform", "scale(0.97)"), ("--omni-button-press-shadow", "0 0.0625rem 0.25rem color-mix(in srgb, var(--omni-color-accent) 55%, transparent)"),
                ("--omni-radius", "1rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"),
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 35%, transparent)"),
                ("--omni-card-shadow", "0 0.75rem 2rem color-mix(in srgb, var(--omni-color-accent) 18%, transparent)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 48%, var(--omni-color-surface))"),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", Sans)),
            Shape(
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-accent) 7%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-accent) 24%, transparent), 0 0.75rem 2rem color-mix(in srgb, var(--omni-color-accent) 20%, transparent)"),
                ("--omni-button-shadow", "0 0.25rem 1rem color-mix(in srgb, var(--omni-color-accent) 45%, transparent)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 42%, var(--omni-color-surface))"))),

        new("Néon", "Nuit électrique : lueurs d’accent, bordures teintées, capitales très espacées.", "Électrique",
            Shape(
                // Press: no movement, the glow flares up.
                ("--omni-button-press-transform", "none"), ("--omni-button-press-shadow", "0 0 0.25rem var(--omni-color-accent), 0 0 1.75rem color-mix(in srgb, var(--omni-color-accent) 85%, transparent)"),
                ("--omni-radius", "0.375rem"), ("--omni-button-radius", "0.375rem"), ("--omni-card-radius", "0.5rem"),
                ("--omni-button-shadow", "0 0 0.9rem color-mix(in srgb, var(--omni-color-accent) 60%, transparent)"),
                ("--omni-card-shadow", "0 0 1.5rem color-mix(in srgb, var(--omni-color-accent) 22%, transparent)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 45%, var(--omni-color-surface))"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.1em"),
                ("--omni-heading-letter-spacing", "0.08em"), ("--omni-heading-text-transform", "uppercase"),
                ("--omni-font-family", Technical)),
            Shape()),

        new("Papier", "Encre sur papier : filets de 2 px, empattements, aucune ombre.", "Or ancien",
            Shape(
                // Press: a stamp, the rule doubles and the button slides one pixel diagonally.
                ("--omni-button-press-transform", "translate(1px, 1px)"), ("--omni-button-press-shadow", "inset 0 0 0 1px var(--omni-color-text)"),
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-border-width", "2px"), ("--omni-button-border-width", "2px"),
                ("--omni-button-border-color", "var(--omni-color-text)"),
                ("--omni-color-border", "var(--omni-color-text)"),
                ("--omni-card-shadow", NoShadow),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Serif), ("--omni-heading-font-family", Serif)),
            Shape(
                // A 2 px rule in the full text colour is garish on a near-black page.
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-text) 4%, var(--omni-color-surface))"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 58%, var(--omni-color-surface))"),
                ("--omni-button-border-color", "color-mix(in srgb, var(--omni-color-text) 58%, var(--omni-color-surface))"))),

        new("Rétro", "Contours épais et ombres dures décalées, sans flou.", "Braise",
            Shape(
                // Press: the button drops into its shadow, which disappears.
                ("--omni-button-press-transform", "translate(0.25rem, 0.25rem)"), ("--omni-button-press-shadow", "0 0 0 2px var(--omni-color-surface)"),
                ("--omni-radius", "0.25rem"), ("--omni-button-radius", "0.25rem"), ("--omni-card-radius", "0.25rem"),
                ("--omni-border-width", "2px"), ("--omni-button-border-width", "2px"),
                ("--omni-button-border-color", "var(--omni-color-text)"),
                ("--omni-color-border", "var(--omni-color-text)"),
                // A surface-coloured ring comes before the offset shadow: without it, a button filled
                // with the text colour and its shadow of the same colour read as one block.
                ("--omni-button-shadow", "0 0 0 2px var(--omni-color-surface), 0.25rem 0.25rem 0 var(--omni-color-text)"),
                ("--omni-card-shadow", "0 0 0 2px var(--omni-color-surface), 0.375rem 0.375rem 0 var(--omni-color-text)"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-font-weight", "800"),
                ("--omni-font-family", Geometric), ("--omni-heading-font-weight", "800")),
            Shape()),

        new("Octet", "Console à huit bits : cadres en escalier, police d’écran.", "Mono",
            Shape(
                // Press: the console key drops one step and takes a pixel shadow on top.
                ("--omni-button-press-transform", "translateY(0.125rem)"), ("--omni-button-press-shadow", Staircase("0.1875rem") + ", inset 0 0.1875rem 0 rgb(0 0 0 / 35%)"),
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"),
                ("--omni-border-width", "2px"), ("--omni-card-border-width", "0"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 70%, var(--omni-color-surface))"),
                ("--omni-card-shadow", Staircase("0.25rem")),
                ("--omni-button-shadow", Staircase("0.1875rem")),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.06em"),
                ("--omni-button-font-weight", "700"),
                ("--omni-font-family", Screen), ("--omni-heading-font-family", Console),
                ("--omni-heading-text-transform", "uppercase"), ("--omni-heading-letter-spacing", "0.04em")),
            Shape()),

        new("Nénuphar", "Coins asymétriques en feuille et reflets de lagune.", "Lagune",
            Shape(
                // Press: a ripple leaves the button, like a drop on water.
                ("--omni-button-press-transform", "scale(0.97)"), ("--omni-button-press-shadow", "0 0 0 0.375rem color-mix(in srgb, var(--omni-color-accent) 22%, transparent)"),
                ("--omni-radius", "0.5rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "1.125rem 0.25rem"), ("--omni-card-radius", "2rem 0.375rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-accent) 24%, transparent), 0 0.75rem 1.75rem color-mix(in srgb, var(--omni-color-accent) 16%, transparent)"),
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 30%, transparent)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Soft)),
            Shape(
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-accent) 6%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-accent) 32%, transparent), 0 0.75rem 1.75rem rgb(0 0 0 / 40%), 0 0 2rem color-mix(in srgb, var(--omni-color-accent) 10%, transparent)"),
                ("--omni-button-shadow", "0 0.25rem 0.875rem color-mix(in srgb, var(--omni-color-accent) 40%, transparent)"))),

        new("Velours", "Biseaux, reflets internes et ombres profondes.", "Prune",
            Shape(
                // Press: the button sinks into the velvet, the highlight vanishes.
                ("--omni-button-press-transform", "translateY(1px)"), ("--omni-button-press-shadow", "inset 0 0.125rem 0.375rem rgb(0 0 0 / 40%)"),
                ("--omni-radius", "0.625rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "0.5rem"), ("--omni-card-radius", "1rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 9%), 0 0 0 1px color-mix(in srgb, var(--omni-color-text) 8%, transparent), 0 0.75rem 2rem rgb(0 0 0 / 22%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 20%), 0 0.125rem 0.375rem rgb(0 0 0 / 22%)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Sans), ("--omni-heading-font-family", Transitional), ("--omni-heading-font-weight", "600")),
            Shape()),
    ];

    /// <summary>A pixel-staircase frame drawn with four hard shadows, reserved to a zero radius.</summary>
    private static string Staircase(string step) =>
        $"0 -{step} 0 0 var(--omni-color-text), 0 {step} 0 0 var(--omni-color-text), -{step} 0 0 0 var(--omni-color-text), {step} 0 0 0 var(--omni-color-text)";

    private static Dictionary<string, string> Shape(params (string Name, string Value)[] tokens) =>
        tokens.ToDictionary(token => token.Name, token => token.Value, StringComparer.Ordinal);
}
