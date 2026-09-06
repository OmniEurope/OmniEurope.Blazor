namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The Bootswatch palettes, transcribed from the SCSS variables each theme publishes.
/// </summary>
/// <remarks>
/// <para>
/// Bootswatch is MIT licensed, and only the colour values are carried over: none of its stylesheets,
/// class names or generated CSS is reproduced here. The values feed a generator that expresses them
/// in this library's own tokens, which is why a Bootswatch theme can be offered without the site
/// depending on Bootstrap in any way.
/// </para>
/// <para>
/// A theme that leaves <c>$body-bg</c> or <c>$body-color</c> unset inherits the Bootstrap defaults,
/// so those two are recorded here as <c>#ffffff</c> and <c>#212529</c> rather than left blank.
/// </para>
/// <para>
/// Font families are deliberately not carried over. The package ships no webfont, so a palette that
/// named one would only render it on machines that happen to have it installed; every theme uses the
/// system interface font instead, which looks native everywhere and needs no network fetch.
/// </para>
/// </remarks>
public static class BootswatchPalettes
{
    private const string DefaultBackground = "#ffffff";
    private const string DefaultForeground = "#212529";

    /// <summary>Every palette, in alphabetical order.</summary>
    public static IReadOnlyList<ThemePalette> All { get; } =
    [
        new("Brite", "Vert acide sur fond blanc", "#a2e436", "#68d391", "#22d2ed", "#ffc700", "#f56565", DefaultBackground, DefaultForeground),
        new("Cerulean", "Bleu ciel franc", "#2fa4e7", "#73a839", "#033c73", "#dd5600", "#c71c22", DefaultBackground, "#495057"),
        new("Cosmo", "Bleu net et contrasté", "#2780e3", "#3fb618", "#9954bb", "#ff7518", "#ff0039", DefaultBackground, "#373a3c"),
        new("Cyborg", "Noir profond et bleu électrique", "#2a9fd6", "#77b300", "#9933cc", "#ff8800", "#cc0000", "#060606", "#adafae"),
        new("Darkly", "Ardoise et turquoise", "#375a7f", "#00bc8c", "#3498db", "#f39c12", "#e74c3c", "#222222", "#ffffff"),
        new("Flatly", "Bleu nuit mat", "#2c3e50", "#18bc9c", "#3498db", "#f39c12", "#e74c3c", DefaultBackground, DefaultForeground),
        new("Journal", "Corail de magazine", "#eb6864", "#22b24c", "#336699", "#f5e625", "#f57a00", DefaultBackground, DefaultForeground),
        new("Litera", "Bleu sobre et lisible", "#4582ec", "#02b875", "#17a2b8", "#f0ad4e", "#d9534f", DefaultBackground, "#343a40"),
        new("Lumen", "Bleu clair et lumineux", "#158cba", "#28b62c", "#75caeb", "#ff851b", "#ff4136", DefaultBackground, DefaultForeground),
        new("Lux", "Noir typographique", "#1a1a1a", "#4bbf73", "#1f9bcf", "#f0ad4e", "#d9534f", DefaultBackground, "#55595c"),
        new("Minty", "Menthe et corail", "#78c2ad", "#56cc9d", "#6cc3d5", "#ffce67", "#ff7851", DefaultBackground, "#888888"),
        new("Morph", "Bleu poudré en relief", "#378dfc", "#43cc29", "#5b62f4", "#ffc107", "#e52527", "#d9e3f1", "#7b8ab8"),
        new("Pulse", "Violet soutenu", "#593196", "#13b955", "#009cdc", "#efa31d", "#fc3939", DefaultBackground, "#444444"),
        new("Quartz", "Violet et rose sur verre", "#e83283", "#41d7a7", "#39cbfb", "#ffc107", "#fd7e14", "#686dc3", "#ffffff"),
        new("Sandstone", "Bleu ardoise et sable", "#325d88", "#93c54b", "#29abe0", "#f47c3c", "#d9534f", DefaultBackground, "#3e3f3a"),
        new("Simplex", "Rouge brique franc", "#d9230f", "#469408", "#029acf", "#d9831f", "#9b479f", "#fcfcfc", DefaultForeground),
        new("Sketchy", "Crayonné noir et blanc", "#333333", "#28a745", "#17a2b8", "#ffc107", "#dc3545", DefaultBackground, DefaultForeground),
        new("Slate", "Gris métal", "#3a3f44", "#62c462", "#5bc0de", "#f89406", "#ee5f5b", "#272b30", "#aaaaaa"),
        new("Solar", "Solarized, ambre sur pétrole", "#b58900", "#2aa198", "#268bd2", "#cb4b16", "#d33682", "#002b36", "#839496"),
        new("Spacelab", "Bleu acier discret", "#446e9b", "#3cb521", "#3399f3", "#d47500", "#cd0200", DefaultBackground, "#777777"),
        new("Superhero", "Orange sur bleu nuit", "#df6919", "#5cb85c", "#5bc0de", "#ffc107", "#d9534f", "#0f2537", "#ebebeb"),
        new("United", "Orange Ubuntu", "#e95420", "#38b44a", "#17a2b8", "#efb73e", "#df382c", DefaultBackground, "#333333"),
        new("Vapor", "Néon violet et cyan", "#6f42c1", "#3cf281", "#1ba2f6", "#ffc107", "#e44c55", "#1a0933", "#32fbe2"),
        new("Yeti", "Bleu glacier", "#008cba", "#43ac6a", "#5bc0de", "#e99002", "#f04124", DefaultBackground, DefaultForeground),
        new("Zephyr", "Bleu électrique et net", "#3459e6", "#2fb380", "#287bb5", "#f4bd61", "#da292e", DefaultBackground, "#495057")
    ];
}
