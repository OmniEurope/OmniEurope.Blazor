namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The palettes of the library. Any palette paints any theme. The first ten are those of the
/// reference mockup (<c>docs/plans/archive/PLAN-004-maquette-themes.html</c>, constant <c>PALETTES</c>); the next four
/// (Nuage, Opale, Pastel, Encre) go with the themes Relief, Givre, Aplat and Épure, Horizon with
/// Trou noir and Zénith with Trou blanc. Each palette carries
/// its dark accent as its own value: equal to the light one by default (recette R-053, a
/// button keeps one colour in both modes), and free to differ for a palette that wants it.
/// </summary>
internal static class PaletteCatalog
{
    public static IReadOnlyList<PaletteDefinition> All { get; } =
    [
        new("Essentiel", "Indigo dans les deux modes, sévérités franches.",
            "#4340d2", "#4caf50", "#2196f3", "#ff9800", "#f44336", "#ffffff", "#424242", "#1e1e1e", "#e0e0e0", "#4340d2"),
        new("Océan", "Bleu franc sur fond d'écume, nuit marine en sombre.",
            "#0b63ce", "#0f8f7d", "#0891b2", "#b97f00", "#d33a3a", "#f1f7fc", "#0f2a44", "#0a1a2e", "#d6e8f7", "#0b63ce"),
        new("Forêt", "Vert sapin sur fond de mousse, sous-bois en sombre.",
            "#2f6f4f", "#3c8d40", "#2d7d9a", "#a87a0b", "#b5433a", "#f3f6f1", "#1e2d24", "#121e17", "#dcebdf", "#2f6f4f"),
        new("Lavande", "Violet lavande sur blanc lilas, nuit mauve en sombre.",
            "#7c5cbf", "#3f9e7a", "#5b7fd6", "#c48d2a", "#d0506a", "#fbf9ff", "#2d2540", "#1a1730", "#ece7fb", "#7c5cbf"),
        new("Électrique", "Cyan électrique et violet, nuit d'encre en sombre.",
            "#008c9e", "#1f9d55", "#7c4dff", "#c28a00", "#d6246e", "#f5f3ff", "#1a1036", "#0b0a1a", "#e6e3ff", "#008c9e"),
        new("Or ancien", "Or bruni sur papier crème, brun chaud en sombre.",
            "#8a6a1c", "#3f7a3a", "#2f5f8a", "#c2571a", "#a82a2a", "#fdfbf5", "#1d1b16", "#1b1813", "#ece2c9", "#8a6a1c"),
        new("Braise", "Orange braise sur fond pêche, rougeoiement en sombre.",
            "#c2410c", "#2f855a", "#2563c9", "#a16207", "#be123c", "#fff3ea", "#2a140c", "#1f1411", "#f8e6dc", "#c2410c"),
        new("Mono", "Noir et blanc purs, seules les sévérités en couleur.",
            "#000000", "#1a7f37", "#0550ae", "#8a5a00", "#cf222e", "#ffffff", "#000000", "#000000", "#ffffff", "#ffffff"),
        new("Lagune", "Turquoise de lagune, bleu-vert profond en sombre.",
            "#0e8a7a", "#2f8f46", "#2a7fb8", "#c07f00", "#d1495b", "#f3faf8", "#10302b", "#082127", "#d8eef0", "#0e8a7a"),
        new("Prune", "Prune et rose, velours sombre en sombre.",
            "#7b2d6e", "#2f7d5b", "#4a6fa5", "#b7791f", "#c23a3a", "#fbf6f9", "#2a1426", "#20111e", "#f4e6f0", "#7b2d6e"),
        new("Nuage", "Bleu vif sur gris nuage, anthracite en sombre, rouge orangé en alerte.",
            "#2f6bff", "#2e9d6a", "#3b82c4", "#e0892a", "#e8503a", "#e4e9f1", "#2c3444", "#1b1e23", "#d9dde5", "#2f6bff"),
        new("Opale", "Indigo franc sur blanc nacré, ciel, menthe et pêche en sévérités, nuit bleutée en sombre.",
            "#4f46e5", "#1fae84", "#2ea3dc", "#f0873e", "#e0424f", "#f7f9fc", "#1b2233", "#10151f", "#e6eaf2", "#4f46e5"),
        new("Pastel", "Lilas et rose sur blanc, bleu marine en sombre, ciel en information.",
            "#b04fd0", "#3aa876", "#5bb8e8", "#e6a23c", "#e2556b", "#fbf8fd", "#1b2233", "#1b2233", "#ece8f5", "#b04fd0"),
        new("Encre", "Encre noire sur blanc cassé, un seul bleu d'encre discret.",
            "#27466f", "#2f7d4f", "#3a6ea5", "#9a6a00", "#b3261e", "#fbfbfa", "#141414", "#111111", "#ededed", "#27466f"),
        new("Horizon", "Ambre d'accrétion sur blanc pur, noir absolu en sombre.",
            "#d9660b", "#23a36b", "#3d8bd9", "#c9a20a", "#d93a4a", "#ffffff", "#0b0b0d", "#000000", "#ece9e4", "#d9660b"),
        // Horizon turned over for Trou blanc: the accent is the disc's amber inverted (#2699f4), deepened
        // to read on ivory; graphite text on an ivory panel. The severities keep their usual meaning and
        // are not inverted (a red error stays red).
        new("Zénith", "Bleu de disque inversé sur ivoire, texte graphite, sévérités inchangées.",
            "#1f6fc4", "#23a36b", "#3d8bd9", "#c9a20a", "#d93a4a", "#fbf8f1", "#26282c", "#101114", "#ece9e4", "#1f6fc4"),
    ];
}
