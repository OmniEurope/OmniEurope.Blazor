namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The fourteen themes of the library, drawn for this project. A theme decides the shape only: radii,
/// borders, shadows, fonts and the way buttons are drawn and pressed. The first ten take the values of
/// the reference mockup (<c>plans/PLAN-008-maquette-themes.html</c>, constant <c>THEMES</c>); Relief,
/// Givre, Aplat and Épure recreate four interface styles (neumorphism, glassmorphism, flat design and
/// minimalism) from our own tokens. Relief, Givre and Aplat declare a <see cref="ThemeDefinition.ContrastWaiver"/>:
/// their style wins over the contrast thresholds (owner decision of 2026-09-28), Épure keeps them.
/// </summary>
/// <remarks>
/// A shape writes no colour of its own: a coloured shadow or border is drawn from a colour token, so it
/// follows any palette. Only neutral shadows (<c>rgb(0 0 0 / x%)</c>, <c>rgb(255 255 255 / x%)</c>) are
/// literal. Fonts come from <see cref="FontCatalog"/>: system stacks and four web fonts the package serves itself.
/// </remarks>
internal static class ThemeCatalog
{
    private const string Sans = FontCatalog.Sans;
    private const string Serif = FontCatalog.Serif;
    private const string Rounded = FontCatalog.Rounded;
    private const string Technical = FontCatalog.Technical;
    private const string Geometric = FontCatalog.Geometric;
    private const string Soft = FontCatalog.Soft;
    private const string Transitional = FontCatalog.Transitional;
    private const string Screen = FontCatalog.Screen;
    private const string Console = FontCatalog.Console;
    private const string Humanist = FontCatalog.Humanist;
    private const string NoShadow = "0 0 #0000";

    // One radius everywhere for Défaut, the button's: small, medium, large, cards, alerts and floating
    // surfaces are all 2.5 px. Chosen smaller than the 4 px of the reference production on purpose.
    private const string DefaultRadius = "0.15625rem";

    public static IReadOnlyList<ThemeDefinition> All { get; } =
    [
        new("Essentiel", "Allure sobre d'application de gestion, rayon de 2,5 px, élévation discrète, sans empattement.", "Essentiel",
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
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 28%, transparent)"),
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

        // Neumorphism: the surfaces are the colour of the page and stand out by a pair of soft shadows,
        // a light one up and to the left, a dark one down and to the right; a press turns them inward.
        // Kept to the library's ratios: the fields keep the palette's border under their hollow, since
        // the shadows alone come nowhere near 3:1, and the focus ring is a solid accent ring.
        new("Relief", "Surfaces modelées dans la page : ombres douces claire et sombre, appui en creux.", "Nuage",
            Shape(
                // Press: the key sinks, its two shadows turn inward.
                ("--omni-button-press-transform", "scale(0.98)"), ("--omni-button-press-shadow", "inset 0.1875rem 0.1875rem 0.375rem rgb(0 0 0 / 18%), inset -0.1875rem -0.1875rem 0.375rem rgb(255 255 255 / 60%)"),
                ("--omni-radius", "0.75rem"), ("--omni-radius-sm", "0.5rem"), ("--omni-radius-lg", "1.25rem"),
                ("--omni-button-radius", "0.875rem"), ("--omni-card-radius", "1.25rem"), ("--omni-alert-radius", "1rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-background", "var(--omni-color-surface)"),
                ("--omni-card-shadow", "-0.375rem -0.375rem 0.875rem rgb(255 255 255 / 75%), 0.375rem 0.375rem 0.875rem rgb(0 0 0 / 14%)"),
                ("--omni-button-shadow", "-0.1875rem -0.1875rem 0.5rem rgb(255 255 255 / 70%), 0.1875rem 0.1875rem 0.5rem rgb(0 0 0 / 16%)"),
                ("--omni-input-shadow", "inset 0.125rem 0.125rem 0.3125rem rgb(0 0 0 / 12%), inset -0.125rem -0.125rem 0.3125rem rgb(255 255 255 / 70%)"),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                ("--omni-overlay-shadow", "-0.25rem -0.25rem 0.75rem rgb(255 255 255 / 60%), 0.5rem 0.75rem 1.75rem rgb(0 0 0 / 18%)"),
                ("--omni-focus-ring", FocusRing),
                ("--omni-button-font-weight", "600"), ("--omni-heading-font-weight", "700"),
                ("--omni-font-family", Rounded)),
            Shape(
                ("--omni-button-press-shadow", "inset 0.1875rem 0.1875rem 0.375rem rgb(0 0 0 / 50%), inset -0.1875rem -0.1875rem 0.375rem rgb(255 255 255 / 5%)"),
                ("--omni-card-shadow", "-0.375rem -0.375rem 0.875rem rgb(255 255 255 / 5%), 0.375rem 0.375rem 1rem rgb(0 0 0 / 55%)"),
                ("--omni-button-shadow", "-0.1875rem -0.1875rem 0.5rem rgb(255 255 255 / 5%), 0.1875rem 0.1875rem 0.5rem rgb(0 0 0 / 50%)"),
                ("--omni-input-shadow", "inset 0.125rem 0.125rem 0.3125rem rgb(0 0 0 / 45%), inset -0.125rem -0.125rem 0.3125rem rgb(255 255 255 / 5%)"),
                ("--omni-overlay-shadow", "-0.25rem -0.25rem 0.75rem rgb(255 255 255 / 5%), 0.5rem 0.75rem 1.75rem rgb(0 0 0 / 55%)")),
            ContrastWaiver: "Relief par ombres douces : bordures et marques non textuelles sous les seuils WCAG."),

        // Glassmorphism: translucent panels over a colour field painted by the theme scope. Kept to the
        // library's ratios: the field is a pale gradient of the palette (every text of the page must
        // read on each of its stops, checked stop by stop), the cards are translucent without a blur of
        // their own (a filter would make them the containing block of the positioned popovers and
        // tooltips they hold, the reason the dialog has none), and the frosted blur goes to the floating
        // layers and the dialog scrim, which cover what they blur.
        new("Givre", "Panneaux de verre dépoli sur un fond de couleurs fondues, liseré clair et grands arrondis.", "Crépuscule",
            Shape(
                // Press: the pane gives a little under the finger and frosts from inside.
                ("--omni-button-press-transform", "scale(0.96)"), ("--omni-button-press-shadow", "inset 0 0.125rem 0.5rem rgb(0 0 0 / 22%)"),
                ("--omni-radius", "0.75rem"), ("--omni-radius-sm", "0.5rem"), ("--omni-radius-lg", "1.25rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"), ("--omni-alert-radius", "1rem"),
                ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-accent) 16%, var(--omni-color-surface))"),
                ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-info) 12%, var(--omni-color-surface))"),
                ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-warning) 14%, var(--omni-color-surface))"),
                // Muted text drawn nearer the body text than the palette draws it: the colour field darkens
                // (or lightens) the page under it, and the palette leaves the muted text no margin for that.
                ("--omni-color-text-muted", "color-mix(in srgb, var(--omni-color-text) 86%, var(--omni-color-surface))"),
                // The field borders sit on the colour field too: drawn with more of the text than the palette's.
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 45%, var(--omni-color-surface))"),
                ("--omni-backdrop", "linear-gradient(135deg, var(--omni-backdrop-start), var(--omni-backdrop-middle) 50%, var(--omni-backdrop-end))"),
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-surface) 58%, transparent)"),
                // The dialog sits on the scrim, not on the colour field: it keeps an opaque pane.
                ("--omni-dialog-background", "var(--omni-color-surface)"),
                ("--omni-card-border-color", "rgb(255 255 255 / 60%)"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 55%), 0 0.5rem 2rem rgb(0 0 0 / 10%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 35%), 0 0.25rem 0.875rem rgb(0 0 0 / 12%)"),
                ("--omni-overlay-background", "color-mix(in srgb, var(--omni-color-surface) 62%, transparent)"),
                ("--omni-overlay-filter", "blur(24px) saturate(1.6)"),
                ("--omni-overlay-shadow", "inset 0 1px 0 rgb(255 255 255 / 50%), 0 1rem 2.5rem rgb(0 0 0 / 18%)"),
                ("--omni-scrim-filter", "blur(6px)"),
                ("--omni-focus-ring", FocusRing),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", Sans)),
            Shape(
                ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-accent) 28%, var(--omni-color-surface))"),
                ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-info) 18%, var(--omni-color-surface))"),
                ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-warning) 24%, var(--omni-color-surface))"),
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-surface) 52%, transparent)"),
                ("--omni-card-border-color", "rgb(255 255 255 / 14%)"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 10%), 0 0.75rem 2rem rgb(0 0 0 / 40%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 18%), 0 0.25rem 1rem rgb(0 0 0 / 35%)"),
                ("--omni-overlay-shadow", "inset 0 1px 0 rgb(255 255 255 / 10%), 0 1rem 2.5rem rgb(0 0 0 / 50%)")),
            ContrastWaiver: "Panneaux translucides sur un fond coloré : la lisibilité dépend du fond, les seuils WCAG ne sont pas garantis."),

        // Flat design: no shadow, no gradient, no relief anywhere; solid blocks, pill buttons, large
        // rounded cards told apart by their fill alone, and a press that only darkens.
        new("Aplat", "Aplats de couleur sans ombre ni dégradé, boutons pilule et cartes pleines.", "Pastel",
            Shape(
                // Press: nothing moves, the fill darkens.
                ("--omni-button-press-transform", "none"), ("--omni-button-press-shadow", NoShadow),
                ("--omni-radius", "0.625rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"), ("--omni-alert-radius", "0.75rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-background", "var(--omni-color-surface-muted)"),
                // Muted text nearer the body text: a full block (a card, a grid header on it) darkens the
                // page under it, and the palette leaves the muted text no margin for that.
                ("--omni-color-text-muted", "color-mix(in srgb, var(--omni-color-text) 86%, var(--omni-color-surface))"),
                ("--omni-card-border-color", "var(--omni-color-border)"),
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                ("--omni-overlay-shadow", NoShadow),
                ("--omni-elevation-shadow-soft", "transparent"), ("--omni-elevation-highlight", "transparent"),
                ("--omni-focus-ring", FocusRing),
                ("--omni-button-font-weight", "600"), ("--omni-heading-font-weight", "700"),
                ("--omni-font-family", Geometric)),
            Shape(),
            ContrastWaiver: "Aplats de couleur vifs : certaines paires de texte et de fond passent sous les seuils WCAG."),

        // Minimalism: black on white, no shadow, no radius, hairlines, very large bold headings. The
        // primary action is drawn in ink and the palette's accent is kept for one discreet place: the
        // hover of that action and the text accents (links, current tab).
        new("Épure", "Noir sur blanc, filets fins, angles vifs et très grands titres gras.", "Encre",
            Shape(
                // Press: the ink key drops one pixel, nothing else.
                ("--omni-button-press-transform", "translateY(1px)"), ("--omni-button-press-shadow", NoShadow),
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"), ("--omni-alert-radius", "0"),
                ("--omni-color-accent-fill", "var(--omni-color-text)"),
                ("--omni-color-accent-fill-hover", "var(--omni-color-accent-strong)"),
                ("--omni-color-accent-fill-active", "color-mix(in srgb, var(--omni-color-accent-strong) 60%, var(--omni-color-text))"),
                ("--omni-color-on-accent-fill", "var(--omni-color-surface)"),
                ("--omni-card-background", "var(--omni-color-surface)"),
                ("--omni-card-border-color", "var(--omni-color-border)"),
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                ("--omni-overlay-shadow", NoShadow),
                ("--omni-elevation-shadow-soft", "transparent"), ("--omni-elevation-highlight", "transparent"),
                ("--omni-focus-ring", "0 0 0 2px var(--omni-color-surface), 0 0 0 3px var(--omni-color-text)"),
                ("--omni-font-size-h1", "clamp(2rem, 1.25rem + 3vw, 3.25rem)"), ("--omni-font-size-h2", "clamp(1.5rem, 1.1rem + 1.6vw, 2.25rem)"),
                ("--omni-font-size-h3", "1.5rem"), ("--omni-font-size-h4", "1.25rem"),
                ("--omni-heading-font-weight", "800"), ("--omni-heading-letter-spacing", "-0.03em"),
                ("--omni-button-font-weight", "500"),
                ("--omni-font-family", Humanist)),
            Shape()),
    ];

    /// <summary>
    /// The focus ring of the four themes whose surfaces stand out by relief, translucency or fill
    /// alone: a solid accent ring of 2 px separated from the control by a ring of the surface, so it
    /// shows on a pressed hollow, a glass pane or a filled block alike.
    /// </summary>
    private const string FocusRing = "0 0 0 2px var(--omni-color-surface), 0 0 0 4px var(--omni-color-accent)";

    /// <summary>A pixel-staircase frame drawn with four hard shadows, reserved to a zero radius.</summary>
    private static string Staircase(string step) =>
        $"0 -{step} 0 0 var(--omni-color-text), 0 {step} 0 0 var(--omni-color-text), -{step} 0 0 0 var(--omni-color-text), {step} 0 0 0 var(--omni-color-text)";

    private static Dictionary<string, string> Shape(params (string Name, string Value)[] tokens) =>
        tokens.ToDictionary(token => token.Name, token => token.Value, StringComparer.Ordinal);
}
