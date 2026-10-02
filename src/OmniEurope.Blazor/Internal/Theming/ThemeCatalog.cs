namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The themes of the library, drawn for this project. A theme decides the shape only: radii,
/// borders, shadows, fonts and the way buttons are drawn and pressed. The first ten take the values of
/// the reference mockup (<c>docs/plans/archive/PLAN-004-maquette-themes.html</c>, constant <c>THEMES</c>); Relief,
/// Givre, Aplat and Épure recreate four interface styles (neumorphism, glassmorphism, flat design and
/// minimalism) from our own tokens, and Trou noir is a black page lit by its accent alone. Relief, Givre and Aplat declare a <see cref="ThemeDefinition.ContrastWaiver"/>:
/// their style wins over the contrast thresholds (owner decision of 2026-09-28), Épure keeps them.
/// </summary>
/// <remarks>
/// A shape writes no colour of its own: a coloured shadow or border is drawn from a colour token, so it
/// follows any palette. Only neutral black and white (<c>rgb(0 0 0 / x%)</c>, <c>rgb(255 255 255 / x%)</c>)
/// are literal: the shadows, and Trou noir's page, the palette surface mixed with black. Fonts come from <see cref="FontCatalog"/>: system stacks and four web fonts the package serves itself.
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

    // One radius everywhere for Essentiel, the button's: small, medium, large, cards, alerts and floating
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
                // Trial (PLAN-004 T14), removable as a block: the layer of cards, tiles, alerts,
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
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"), ("--omni-alert-radius", "0"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.06em"),
                ("--omni-button-font-weight", "600"),
                ("--omni-heading-text-transform", "uppercase"), ("--omni-heading-letter-spacing", "0.05em"),
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow), ("--omni-alert-shadow", NoShadow),
                ("--omni-font-family", Sans)),
            Shape()),

        new("Galet", "Tout en rondeur, boutons pilule, liseré et ombre diffuse à la place de la bordure.", "Forêt",
            Shape(
                // Press: the pebble squashes a little under the finger.
                ("--omni-button-press-transform", "scale(0.95)"),
                ("--omni-radius", "0.875rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.5rem"), ("--omni-alert-radius", "1.125rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 7%, transparent), 0 0.75rem 2rem rgb(0 0 0 / 12%)"),
                // A pebble: shaded underneath, resting on a soft diffuse shadow.
                ("--omni-button-shadow", "inset 0 -0.125rem 0 rgb(0 0 0 / 10%), 0 0.125rem 0.5rem rgb(0 0 0 / 14%)"),
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
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"), ("--omni-alert-radius", "1rem"),
                ("--omni-button-shadow", "0 0.25rem 0.75rem color-mix(in srgb, var(--omni-color-accent) 35%, transparent)"),
                ("--omni-card-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-accent) 20%, transparent), 0 0.75rem 2.25rem color-mix(in srgb, var(--omni-color-accent) 28%, transparent)"),
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
                ("--omni-radius", "0.375rem"), ("--omni-button-radius", "0.375rem"), ("--omni-card-radius", "0.5rem"), ("--omni-alert-radius", "0.375rem"),
                // On a light page a wide glow smears: the tube is tight there, and flares at night (below).
                ("--omni-button-shadow", "0 0 0.5rem color-mix(in srgb, var(--omni-color-accent) 40%, transparent)"),
                ("--omni-card-shadow", "0 0 1rem color-mix(in srgb, var(--omni-color-accent) 16%, transparent)"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-accent) 45%, var(--omni-color-surface))"),
                ("--omni-button-text-transform", "uppercase"), ("--omni-button-letter-spacing", "0.1em"),
                ("--omni-heading-letter-spacing", "0.08em"), ("--omni-heading-text-transform", "uppercase"),
                ("--omni-font-family", Technical)),
            Shape(
                ("--omni-button-shadow", "0 0 0.9rem color-mix(in srgb, var(--omni-color-accent) 60%, transparent)"),
                ("--omni-card-shadow", "0 0 1.5rem color-mix(in srgb, var(--omni-color-accent) 22%, transparent)"))),

        new("Papier", "Encre sur papier : filets de 2 px, empattements, aucune ombre.", "Or ancien",
            Shape(
                // Press: a stamp, the rule doubles and the button slides one pixel diagonally.
                ("--omni-button-press-transform", "translate(1px, 1px)"), ("--omni-button-press-shadow", "inset 0 0 0 1px var(--omni-color-text)"),
                ("--omni-radius", "0"), ("--omni-radius-sm", "0"), ("--omni-radius-lg", "0"),
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"), ("--omni-alert-radius", "0"),
                ("--omni-border-width", "2px"), ("--omni-button-border-width", "2px"),
                ("--omni-button-border-color", "var(--omni-color-text)"),
                ("--omni-color-border", "var(--omni-color-text)"),
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow), ("--omni-alert-shadow", NoShadow),
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
                ("--omni-radius", "0.25rem"), ("--omni-button-radius", "0.25rem"), ("--omni-card-radius", "0.25rem"), ("--omni-alert-radius", "0.25rem"),
                // A filled alert is a block of the same family: the hard offset shadow, no blur.
                ("--omni-alert-shadow", "0.25rem 0.25rem 0 var(--omni-color-text)"),
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
                ("--omni-button-radius", "0"), ("--omni-card-radius", "0"), ("--omni-alert-radius", "0"),
                ("--omni-alert-shadow", Staircase("0.1875rem")),
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
                ("--omni-button-radius", "1.125rem 0.25rem"), ("--omni-card-radius", "2rem 0.375rem"), ("--omni-alert-radius", "1.25rem 0.375rem"),
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
                ("--omni-button-radius", "0.5rem"), ("--omni-card-radius", "1rem"), ("--omni-alert-radius", "0.75rem"),
                ("--omni-card-border-width", "0"),
                // Velvet has a pile: the card is warmed by the accent, lit along its top edge and sits on
                // a deep shadow; a button is a cushion, lit above and shaded below.
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-accent) 4%, var(--omni-color-surface))"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 70%), 0 0 0 1px color-mix(in srgb, var(--omni-color-text) 10%, transparent), 0 0.25rem 0.5rem rgb(0 0 0 / 8%), 0 1.25rem 2.5rem rgb(0 0 0 / 20%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 30%), inset 0 -0.125rem 0 rgb(0 0 0 / 16%), 0 0.25rem 0.625rem rgb(0 0 0 / 24%)"),
                ("--omni-button-font-weight", "600"),
                ("--omni-font-family", Sans), ("--omni-heading-font-family", Transitional), ("--omni-heading-font-weight", "600")),
            Shape(
                // The bright top edge of the light card would be a white line at night.
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 10%), 0 0 0 1px color-mix(in srgb, var(--omni-color-text) 9%, transparent), 0 1.25rem 2.5rem rgb(0 0 0 / 45%)"))),

        // Neumorphism: the surfaces are the colour of the page and stand out by a pair of soft shadows,
        // a light one up and to the left, a dark one down and to the right; a press turns them inward.
        // Below the contrast thresholds (waiver): the fields lose their rule and are creased into the
        // page by inset shadows alone, and the secondary button is moulded in the page colour. The
        // dark shadow is the page darkened, so it keeps the palette's hue. The focus state keeps its
        // guarantee: a solid accent ring, and the focused field takes the accent rule.
        new("Relief", "Surfaces modelées dans la page : ombres douces claire et sombre, appui en creux.", "Nuage",
            Shape(
                // Press: the key sinks, its two shadows turn inward.
                ("--omni-button-press-transform", "scale(0.98)"),
                ("--omni-button-press-shadow", $"inset 0.25rem 0.25rem 0.5rem {ReliefShade}, inset -0.25rem -0.25rem 0.5rem rgb(255 255 255 / 90%)"),
                ("--omni-radius", "0.75rem"), ("--omni-radius-sm", "0.5rem"), ("--omni-radius-lg", "1.25rem"),
                ("--omni-button-radius", "0.875rem"), ("--omni-card-radius", "1.5rem"), ("--omni-alert-radius", "1rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-background", "var(--omni-color-surface)"),
                ("--omni-card-shadow", $"-0.5625rem -0.5625rem 1.25rem rgb(255 255 255 / 95%), 0.5625rem 0.5625rem 1.25rem {ReliefShade}"),
                ("--omni-button-shadow", $"-0.3125rem -0.3125rem 0.75rem rgb(255 255 255 / 95%), 0.3125rem 0.3125rem 0.75rem {ReliefShade}"),
                ("--omni-input-shadow", $"inset 0.1875rem 0.1875rem 0.4375rem {ReliefShade}, inset -0.1875rem -0.1875rem 0.4375rem rgb(255 255 255 / 95%)"),
                ("--omni-input-border-color", "var(--omni-color-surface)"),
                // The secondary button is the page itself, raised by its shadows; hovered it dips a
                // little, pressed it is hollowed.
                ("--omni-color-neutral-fill", "var(--omni-color-surface)"),
                ("--omni-color-neutral-fill-hover", "var(--omni-color-surface-muted)"),
                ("--omni-color-neutral-fill-active", "var(--omni-color-surface-muted)"),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                ("--omni-overlay-shadow", $"-0.375rem -0.375rem 1rem rgb(255 255 255 / 80%), 0.625rem 0.75rem 1.75rem {ReliefShade}"),
                ("--omni-focus-ring", FocusRing), ("--omni-focus-ring-danger", FocusRingDanger), ("--omni-focus-ring-inset", FocusRingInset),
                ("--omni-button-font-weight", "600"), ("--omni-heading-font-weight", "700"),
                ("--omni-font-family", Rounded)),
            Shape(
                // Charcoal: the light shadow is a faint white, the dark one close to black.
                ("--omni-button-press-shadow", "inset 0.25rem 0.25rem 0.5rem rgb(0 0 0 / 60%), inset -0.25rem -0.25rem 0.5rem rgb(255 255 255 / 7%)"),
                ("--omni-card-shadow", "-0.5625rem -0.5625rem 1.25rem rgb(255 255 255 / 12%), 0.5625rem 0.5625rem 1.25rem rgb(0 0 0 / 74%)"),
                ("--omni-button-shadow", "-0.3125rem -0.3125rem 0.75rem rgb(255 255 255 / 10%), 0.3125rem 0.3125rem 0.75rem rgb(0 0 0 / 70%)"),
                ("--omni-input-shadow", "inset 0.1875rem 0.1875rem 0.4375rem rgb(0 0 0 / 55%), inset -0.1875rem -0.1875rem 0.4375rem rgb(255 255 255 / 7%)"),
                ("--omni-overlay-shadow", "-0.375rem -0.375rem 1rem rgb(255 255 255 / 6%), 0.625rem 0.75rem 1.75rem rgb(0 0 0 / 60%)")),
            ContrastWaiver: "Relief par ombres douces : bordures et marques non textuelles sous les seuils WCAG."),

        // Glassmorphism, airy glass: frosted panes over a colour field painted by the theme scope. The
        // field is four very large orbs of the palette with no hard core (the accent as lavender, the
        // information as sky, success as mint and a small warm glow, laid as radial gradients that reach
        // under the content) over a cool white page in light mode; on the night page of dark mode the
        // orbs are deeper and the glow takes the danger hue, an aurora. Each orb drifts on a small
        // orbit, one turn in two minutes (--omni-scope-motion). The accent stays one deep confident
        // colour for actions, so it stands out from the pale hues under it. The shell (app bar, sidebar)
        // is glass too, so the field runs edge to edge. Cards are clearly frosted: a pseudo-element under each card blurs and saturates the field (the card
        // itself takes no filter, which would make it the containing block of the fixed popovers and
        // tooltips it holds), and the card lays over it a milky translucent fill, a 1 px light rim, a top
        // highlight and a large soft shadow. Below the contrast thresholds (waiver): only the focus ring
        // keeps its guarantee.
        new("Givre", "Panneaux de verre dépoli sur un fond de grandes taches pastel, liseré clair et grands arrondis.", "Opale",
            Shape(
                // Press: the pane gives a little under the finger and frosts from inside.
                ("--omni-button-press-transform", "scale(0.96)"), ("--omni-button-press-shadow", "inset 0 0.125rem 0.5rem rgb(0 0 0 / 20%)"),
                ("--omni-radius", "0.875rem"), ("--omni-radius-sm", "0.625rem"), ("--omni-radius-lg", "1.5rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.5rem"), ("--omni-alert-radius", "1.125rem"),
                ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-info-fill) 42%, var(--omni-color-surface))"),
                ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-accent-fill) 34%, var(--omni-color-surface))"),
                ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-success-fill) 34%, var(--omni-color-surface))"),
                ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-warning-fill) 36%, var(--omni-color-surface))"),
                ("--omni-backdrop", GivreField),
                ("--omni-scope-motion", "omni-scope-turn 120s linear infinite"),
                ("--omni-shell-background", "color-mix(in srgb, var(--omni-color-surface) 62%, transparent)"),
                // One hook frosts every card surface; the scope isolates so the frost paints just above the field.
                ("--omni-scope-isolation", "isolate"),
                ("--omni-card-filter", "blur(22px) saturate(1.6)"),
                ("--omni-color-text-muted", "color-mix(in srgb, var(--omni-color-text) 76%, var(--omni-color-surface))"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 26%, transparent)"),
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-surface) 52%, transparent)"),
                ("--omni-card-border-color", "rgb(255 255 255 / 90%)"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 95%), 0 0.25rem 0.75rem rgb(0 0 0 / 4%), 0 1.5rem 3rem rgb(0 0 0 / 7%)"),
                // A grid, an editor or a log is read line by line, and its scrolling frame cannot carry
                // the frost: a pane dense enough to read as the frosted cards do.
                ("--omni-grid-background", "color-mix(in srgb, var(--omni-color-surface) 86%, transparent)"),
                // The dialog sits on the frosted scrim, which already blurs the page: a dense pane.
                ("--omni-dialog-background", "color-mix(in srgb, var(--omni-color-surface) 92%, transparent)"),
                // A filled alert is tinted glass: the field shows through its fill, under a light rim.
                ("--omni-alert-fill-opacity", "82%"),
                ("--omni-alert-shadow", "inset 0 1px 0 rgb(255 255 255 / 45%), 0 0.5rem 1.5rem rgb(0 0 0 / 8%)"),
                // Fields and the secondary button are glass as well.
                ("--omni-input-background", "color-mix(in srgb, var(--omni-color-surface) 66%, transparent)"),
                ("--omni-color-neutral-fill", "color-mix(in srgb, var(--omni-color-surface) 64%, transparent)"),
                ("--omni-color-neutral-fill-hover", "color-mix(in srgb, var(--omni-color-surface) 84%, transparent)"),
                ("--omni-color-neutral-fill-active", "color-mix(in srgb, var(--omni-color-surface) 94%, transparent)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 35%), 0 0.25rem 0.875rem rgb(0 0 0 / 9%)"),
                ("--omni-overlay-background", "color-mix(in srgb, var(--omni-color-surface) 72%, transparent)"),
                ("--omni-overlay-filter", "blur(24px) saturate(1.6)"),
                ("--omni-overlay-shadow", "inset 0 1px 0 rgb(255 255 255 / 85%), 0 1.25rem 3rem rgb(0 0 0 / 14%)"),
                // A light frosted veil: the page keeps its colours behind the dialog.
                ("--omni-color-overlay", "rgb(0 0 0 / 22%)"), ("--omni-scrim-filter", "blur(12px) saturate(1.4)"),
                ("--omni-focus-ring", FocusRing), ("--omni-focus-ring-danger", FocusRingDanger), ("--omni-focus-ring-inset", FocusRingInset),
                ("--omni-button-font-weight", "500"), ("--omni-heading-font-weight", "600"),
                ("--omni-font-family", Sans)),
            Shape(
                ("--omni-backdrop-start", "color-mix(in srgb, var(--omni-color-info-fill) 50%, var(--omni-color-surface))"),
                ("--omni-backdrop-middle", "color-mix(in srgb, var(--omni-color-accent-fill) 58%, var(--omni-color-surface))"),
                ("--omni-backdrop-end", "color-mix(in srgb, var(--omni-color-success-fill) 40%, var(--omni-color-surface))"),
                ("--omni-backdrop-glow", "color-mix(in srgb, var(--omni-color-danger-fill) 26%, var(--omni-color-surface))"),
                ("--omni-shell-background", "color-mix(in srgb, var(--omni-color-surface) 50%, transparent)"),
                ("--omni-color-text-muted", "color-mix(in srgb, var(--omni-color-text) 80%, var(--omni-color-surface))"),
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 28%, transparent)"),
                // Night glass is a light veil, not a darker pane: the aurora shows through it.
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-text) 7%, transparent)"),
                ("--omni-card-border-color", "rgb(255 255 255 / 14%)"),
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 14%), 0 0.25rem 0.75rem rgb(0 0 0 / 16%), 0 1.5rem 3rem rgb(0 0 0 / 30%)"),
                ("--omni-grid-background", "color-mix(in srgb, var(--omni-color-surface) 84%, transparent)"),
                ("--omni-dialog-background", "color-mix(in srgb, var(--omni-color-surface) 90%, transparent)"),
                ("--omni-alert-shadow", "inset 0 1px 0 rgb(255 255 255 / 16%), 0 0.5rem 1.5rem rgb(0 0 0 / 30%)"),
                ("--omni-input-background", "color-mix(in srgb, var(--omni-color-surface) 52%, transparent)"),
                ("--omni-color-neutral-fill", "color-mix(in srgb, var(--omni-color-text) 9%, transparent)"),
                ("--omni-color-neutral-fill-hover", "color-mix(in srgb, var(--omni-color-text) 15%, transparent)"),
                ("--omni-color-neutral-fill-active", "color-mix(in srgb, var(--omni-color-text) 21%, transparent)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 18%), 0 0.25rem 1rem rgb(0 0 0 / 28%)"),
                ("--omni-overlay-background", "color-mix(in srgb, var(--omni-color-surface) 76%, transparent)"),
                ("--omni-color-overlay", "rgb(0 0 0 / 45%)"),
                ("--omni-overlay-shadow", "inset 0 1px 0 rgb(255 255 255 / 12%), 0 1.25rem 3rem rgb(0 0 0 / 48%)")),
            ContrastWaiver: "Panneaux translucides sur un fond coloré : la lisibilité dépend du fond, les seuils WCAG ne sont pas garantis."),

        // Flat design: no shadow, no gradient, no relief anywhere; solid colour fields, pill buttons,
        // large rounded cards told apart by their fill alone, and a press that only changes the fill. Below the
        // contrast thresholds (waiver): the cards are a bold field of the accent, the secondary button a
        // field of the information colour, and the filled alerts lose the glow the package gives them.
        // Grids stay plain panels of the page, the white block laid on the field.
        new("Aplat", "Aplats de couleur sans ombre ni dégradé, boutons pilule et cartes pleines.", "Pastel",
            Shape(
                // Press: nothing moves, the fill changes shade (the primary field pales, away from its ink).
                ("--omni-button-press-transform", "none"), ("--omni-button-press-shadow", NoShadow),
                ("--omni-radius", "0.625rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "1rem"),
                ("--omni-button-radius", "999px"), ("--omni-card-radius", "1.25rem"), ("--omni-alert-radius", "0.75rem"),
                ("--omni-card-border-width", "0"),
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-accent) 34%, var(--omni-color-surface))"),
                ("--omni-card-border-color", "transparent"),
                ("--omni-grid-background", "var(--omni-color-surface)"),
                ("--omni-dialog-background", "var(--omni-color-surface)"),
                // The primary button is a light field of the accent with the near-black ink of the bright
                // fills, the secondary a field of the information colour.
                ("--omni-color-accent-fill", "color-mix(in srgb, var(--omni-color-accent) 44%, var(--omni-color-surface))"),
                ("--omni-color-accent-fill-hover", "color-mix(in srgb, var(--omni-color-accent) 36%, var(--omni-color-surface))"),
                ("--omni-color-accent-fill-active", "color-mix(in srgb, var(--omni-color-accent) 28%, var(--omni-color-surface))"),
                ("--omni-color-on-accent-fill", "var(--omni-color-on-bright)"),
                ("--omni-color-neutral-fill", "color-mix(in srgb, var(--omni-color-info-fill) 34%, var(--omni-color-surface))"),
                ("--omni-color-neutral-fill-hover", "color-mix(in srgb, var(--omni-color-info-fill) 46%, var(--omni-color-surface))"),
                ("--omni-color-neutral-fill-active", "color-mix(in srgb, var(--omni-color-info-fill) 58%, var(--omni-color-surface))"),
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow), ("--omni-alert-shadow", NoShadow),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                // A menu or a window is the page's own colour laid over the page: without an edge it
                // cannot be told from it. A flat hairline, no blur, so still no shadow.
                ("--omni-overlay-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 22%, transparent)"),
                ("--omni-elevation-shadow-soft", "transparent"), ("--omni-elevation-highlight", "transparent"),
                ("--omni-focus-ring", FocusRing), ("--omni-focus-ring-danger", FocusRingDanger), ("--omni-focus-ring-inset", FocusRingInset),
                ("--omni-button-font-weight", "600"), ("--omni-heading-font-weight", "700"),
                ("--omni-font-family", Geometric)),
            Shape(
                // On the dark page the light fields are drawn towards the light text.
                ("--omni-color-accent-fill", "color-mix(in srgb, var(--omni-color-accent) 50%, var(--omni-color-text))"),
                ("--omni-color-accent-fill-hover", "color-mix(in srgb, var(--omni-color-accent) 40%, var(--omni-color-text))"),
                ("--omni-color-accent-fill-active", "color-mix(in srgb, var(--omni-color-accent) 30%, var(--omni-color-text))"),
                ("--omni-color-neutral-fill-hover", "color-mix(in srgb, var(--omni-color-info-fill) 40%, var(--omni-color-surface))"),
                ("--omni-color-neutral-fill-active", "color-mix(in srgb, var(--omni-color-info-fill) 46%, var(--omni-color-surface))")),
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
                ("--omni-card-shadow", NoShadow), ("--omni-button-shadow", NoShadow), ("--omni-alert-shadow", NoShadow),
                ("--omni-overlay-background", "var(--omni-color-surface)"), ("--omni-overlay-filter", "none"),
                ("--omni-overlay-shadow", NoShadow),
                ("--omni-elevation-shadow-soft", "transparent"), ("--omni-elevation-highlight", "transparent"),
                ("--omni-focus-ring", "0 0 0 2px var(--omni-color-surface), 0 0 0 3px var(--omni-color-text)"),
                ("--omni-focus-ring-danger", "0 0 0 2px var(--omni-color-surface), 0 0 0 3px var(--omni-color-danger)"),
                ("--omni-focus-ring-inset", "inset 0 0 0 2px var(--omni-color-text)"),
                ("--omni-font-size-h1", "clamp(2rem, 1.25rem + 3vw, 3.25rem)"), ("--omni-font-size-h2", "clamp(1.5rem, 1.1rem + 1.6vw, 2.25rem)"),
                ("--omni-font-size-h3", "1.5rem"), ("--omni-font-size-h4", "1.25rem"),
                ("--omni-heading-font-weight", "800"), ("--omni-heading-letter-spacing", "-0.03em"),
                ("--omni-button-font-weight", "500"),
                ("--omni-font-family", Humanist)),
            Shape()),

        // A black hole, always dark (owner decision of 2026-10-01: no light half): the page is the
        // palette's surface taken three quarters of the way to black (--omni-scope-page), the cards are a faint veil of the text colour
        // closed by a hairline, so the field shows through them. High on the right, the horizon is a disc
        // ringed by a thin photon ring, its white accretion disc tilted down to the left and bent by the
        // hole, the far side arching over the horizon (a WebGL shader, omni-black-hole.js). Every white stays sober,
        // a tenth of the text colour at most, so the theme holds the contrast thresholds with every palette; the
        // dense surfaces (grids, dialogs) are opaque.
        new("Trou noir", "Toujours sombre : noir absolu, cartes à peine voilées, disque d’accrétion incliné, blanc au bord intérieur puis couleur d’accent, qui tourne lentement derrière la page.", "Horizon",
            Shape(
                // Press: the light falls in, the button shrinks and darkens from its edge.
                ("--omni-button-press-transform", "scale(0.96)"), ("--omni-button-press-shadow", "inset 0 0 0.75rem rgb(0 0 0 / 55%)"),
                ("--omni-radius", "0.5rem"), ("--omni-radius-sm", "0.375rem"), ("--omni-radius-lg", "0.875rem"),
                ("--omni-button-radius", "0.5rem"), ("--omni-card-radius", "0.875rem"), ("--omni-alert-radius", "0.625rem"),
                // White on the black page, as the text colour is (a theme writes no colour of its own), and
                // sober: the stops stay as far from the page as text, the rule and the neutral badge still
                // read on them with every palette.
                ("--omni-backdrop-disk", "color-mix(in srgb, var(--omni-color-text) 9%, var(--omni-color-surface))"),
                ("--omni-backdrop-halo", "color-mix(in srgb, var(--omni-color-text) 2%, var(--omni-color-surface))"),
                // A firmer rule than the palette's: it keeps its floor on the stops of the field too.
                ("--omni-color-border", "color-mix(in srgb, var(--omni-color-text) 36%, var(--omni-color-surface))"),
                ("--omni-backdrop", TrouNoirField),
                // The lensed black hole is drawn by a shader on the scope's canvas (omni-black-hole.js), which
                // a background cannot do; the field above is what shows without WebGL.
                ("--omni-scope-canvas", "black-hole"),
                // A deep black page whatever the palette: its dark surface three quarters of the way to
                // black (owner decision of 2026-10-02), a neutral black as the shadows are. Text and fills
                // are measured against the surface, so a darker page only widens their contrast.
                ("--omni-scope-page", "color-mix(in srgb, var(--omni-color-surface) 25%, rgb(0 0 0 / 100%))"),
                ("--omni-scope-isolation", "isolate"),
                ("--omni-scope-motion", "omni-scope-turn 120s linear infinite"),
                ("--omni-card-background", "color-mix(in srgb, var(--omni-color-text) 4%, transparent)"),
                ("--omni-card-border-color", "color-mix(in srgb, var(--omni-color-text) 13%, transparent)"),
                ("--omni-card-shadow", "0 1px 2px rgb(0 0 0 / 5%)"),
                ("--omni-button-shadow", "0 1px 2px rgb(0 0 0 / 22%)"),
                // A grid is read line by line and its frozen columns cover the rows that slide under
                // them; a dialog sits over the page: both are opaque, one step off the page. The grid stays
                // at 3 %: its header darkens the frame by 6 % more, and muted titles must still read there.
                ("--omni-grid-background", "color-mix(in srgb, var(--omni-color-text) 3%, var(--omni-color-surface))"),
                ("--omni-dialog-background", "color-mix(in srgb, var(--omni-color-text) 5%, var(--omni-color-surface))"),
                ("--omni-focus-ring", FocusRing), ("--omni-focus-ring-danger", FocusRingDanger), ("--omni-focus-ring-inset", FocusRingInset),
                ("--omni-button-font-weight", "600"), ("--omni-heading-font-weight", "600"),
                ("--omni-heading-letter-spacing", "0.01em"),
                ("--omni-font-family", Geometric)),
            Shape(
                // On black a dark shadow shows nothing: the card takes a top highlight and the buttons
                // glow with the accent, as does the floating layer.
                ("--omni-card-shadow", "inset 0 1px 0 rgb(255 255 255 / 5%)"),
                ("--omni-button-shadow", "inset 0 1px 0 rgb(255 255 255 / 12%), 0 0 1.125rem color-mix(in srgb, var(--omni-color-accent) 26%, transparent)"),
                ("--omni-overlay-shadow", "0 0 0 1px color-mix(in srgb, var(--omni-color-text) 14%, transparent), 0 1.5rem 4rem rgb(0 0 0 / 85%), 0 0 3.5rem color-mix(in srgb, var(--omni-color-accent) 10%, transparent)")),
            DarkOnly: true),
    ];

    /// <summary>
    /// The focus ring of the themes (Relief, Givre, Aplat, Trou noir) whose surfaces stand out by relief, translucency,
    /// fill or a faint veil alone: a solid accent ring of 2 px separated from the control by a ring of the surface, so it
    /// shows on a pressed hollow, a glass pane or a filled block alike.
    /// </summary>
    private const string FocusRing = "0 0 0 2px var(--omni-color-surface), 0 0 0 4px var(--omni-color-accent)";

    /// <summary>The same solid ring in the danger colour, around an invalid field.</summary>
    private const string FocusRingDanger = "0 0 0 2px var(--omni-color-surface), 0 0 0 4px var(--omni-color-danger)";

    /// <summary>
    /// The same ring drawn inside a surface whose overflow would clip an outer one (an editor, a
    /// scrolling list): 2 px of solid accent within the edge.
    /// </summary>
    private const string FocusRingInset = "inset 0 0 0 2px var(--omni-color-accent)";

    /// <summary>The angle the scope turns once per cycle of a moving field (see the stylesheet, <c>--omni-scope-turn</c>).</summary>
    private const string Turn = "var(--omni-scope-turn)";

    /// <summary>
    /// Givre's colour field: four very large orbs (lavender, a warm glow, sky and mint: the stops the
    /// contrast checks measure), each fading from its centre with no hard core and sized on the
    /// viewport, so they overlap under the content instead of staying in the corners. Laid as radial
    /// gradients over a base that drifts from the page towards the information hue, pinned to the
    /// viewport by the scope, so the frosted panes scroll over it. The centre of each orb rides a
    /// small orbit read from the scope's turning angle: with the motion off the angle is zero and the
    /// field is still.
    /// </summary>
    private const string GivreField =
        $"radial-gradient(circle at calc(14% + 5% * cos({Turn})) calc(10% + 6% * sin({Turn})), var(--omni-backdrop-middle) 0, transparent 46vmax), "
        + $"radial-gradient(circle at calc(90% + 4% * sin({Turn})) calc(6% + 5% * cos({Turn})), var(--omni-backdrop-glow) 0, transparent 30vmax), "
        + $"radial-gradient(circle at calc(30% + 6% * sin({Turn} * 2)) calc(66% + 5% * cos({Turn})), var(--omni-backdrop-start) 0, transparent 44vmax), "
        + $"radial-gradient(circle at calc(88% + 5% * cos({Turn})) calc(82% + 6% * sin({Turn} * 2)), var(--omni-backdrop-end) 0, transparent 48vmax), "
        + "linear-gradient(160deg, var(--omni-color-surface), color-mix(in srgb, var(--omni-color-info-fill) 8%, var(--omni-color-surface)))";

    /// <summary>
    /// Trou noir's field without WebGL, where the shader puts the hole (high on the right, a shadow of
    /// 9vmax): the horizon in the page colour, its thin photon ring, and a faint halo further out.
    /// </summary>
    private const string TrouNoirField =
        "radial-gradient(circle at 76% 26%, var(--omni-color-surface) 0 9vmax, transparent 9.1vmax), "
        + "radial-gradient(circle at 76% 26%, transparent 0 9.05vmax, var(--omni-backdrop-disk) 9.3vmax, transparent 10.4vmax), "
        + "radial-gradient(circle at 76% 26%, transparent 0 9vmax, var(--omni-backdrop-halo) 9.6vmax, transparent 22vmax)";

    /// <summary>
    /// Relief's dark shadow in light mode: the page darkened, so the shadow keeps the hue of the
    /// palette's surface like the moulded material it imitates.
    /// </summary>
    private const string ReliefShade = "color-mix(in srgb, var(--omni-color-surface) 64%, rgb(0 0 0 / 100%))";

    /// <summary>A pixel-staircase frame drawn with four hard shadows, reserved to a zero radius.</summary>
    private static string Staircase(string step) =>
        $"0 -{step} 0 0 var(--omni-color-text), 0 {step} 0 0 var(--omni-color-text), -{step} 0 0 0 var(--omni-color-text), {step} 0 0 0 var(--omni-color-text)";

    private static Dictionary<string, string> Shape(params (string Name, string Value)[] tokens) =>
        tokens.ToDictionary(token => token.Name, token => token.Value, StringComparer.Ordinal);
}
