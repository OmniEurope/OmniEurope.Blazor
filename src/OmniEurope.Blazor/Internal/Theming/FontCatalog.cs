namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The ten fonts of the library. Each theme is drawn with one of them (its default font), and any of
/// them can replace it. Six are system stacks; four start with a web font the package serves itself
/// (Inter, Lexend, Source Serif 4, JetBrains Mono, SIL OFL 1.1), followed by system fallbacks. Every
/// stack ends with a generic family.
/// </summary>
internal static class FontCatalog
{
    public const string Sans = "system-ui, -apple-system, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif";
    public const string Serif = "\"Source Serif 4\", Georgia, \"Iowan Old Style\", \"Palatino Linotype\", \"Times New Roman\", serif";
    public const string Rounded = "ui-rounded, \"SF Pro Rounded\", Nunito, \"Segoe UI\", system-ui, sans-serif";
    public const string Technical = "Bahnschrift, \"DIN Alternate\", \"Roboto Condensed\", \"Arial Narrow\", sans-serif";
    public const string Geometric = "\"Century Gothic\", Futura, \"Avenir Next\", \"Segoe UI\", sans-serif";
    public const string Soft = "Corbel, \"Avenir Next\", Avenir, \"Segoe UI\", sans-serif";
    public const string Transitional = "Cambria, Constantia, \"Hoefler Text\", \"Iowan Old Style\", Georgia, serif";
    public const string Screen = "Lexend, Verdana, Tahoma, \"DejaVu Sans\", \"Bitstream Vera Sans\", sans-serif";
    public const string Console = "\"JetBrains Mono\", \"Lucida Console\", \"Courier New\", ui-monospace, monospace";
    public const string Humanist = "Inter, system-ui, -apple-system, \"Segoe UI\", Roboto, Arial, sans-serif";

    public static IReadOnlyList<(string Name, string Description, string Family)> All { get; } =
    [
        ("Système", "La police de l’interface du système, sans empattement.", Sans),
        ("Inter", "Sans empattement neutre dessinée pour les écrans (police web libre).", Humanist),
        ("Arrondie", "Terminaisons arrondies, ton amical.", Rounded),
        ("Douce", "Sans empattement ouverte et légère.", Soft),
        ("Géométrique", "Cercles et droites nets, allure moderne.", Geometric),
        ("Technique", "Étroite et régulière, lecture de tableau de bord.", Technical),
        ("Lexend", "Large et très lisible, pensée pour la fluidité de lecture (police web libre).", Screen),
        ("Source Serif", "Police de lecture à empattements (police web libre).", Serif),
        ("Transitionnelle", "Empattements fins et contraste modéré.", Transitional),
        ("JetBrains Mono", "Chasse fixe, chaque caractère de même largeur (police web libre).", Console),
    ];
}
