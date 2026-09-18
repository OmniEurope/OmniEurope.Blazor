namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The ten palettes of the library. Any palette paints any theme. Values are those of the reference
/// mockup (<c>plans/PLAN-008-maquette-themes.html</c>, constant <c>PALETTES</c>); each palette carries
/// its own dark accent, spread around the colour wheel so that the palettes stay apart in dark mode.
/// </summary>
internal static class PaletteCatalog
{
    public static IReadOnlyList<PaletteDefinition> All { get; } =
    [
        new("Défaut", "Indigo en clair, violet en sombre, sévérités franches.",
            "#4340d2", "#4caf50", "#2196f3", "#ff9800", "#f44336", "#ffffff", "#424242", "#1e1e1e", "#e0e0e0", "#bb86fc"),
        new("Océan", "Bleu franc sur fond d'écume, nuit marine en sombre.",
            "#0b63ce", "#0f8f7d", "#0891b2", "#b97f00", "#d33a3a", "#f1f7fc", "#0f2a44", "#0a1a2e", "#d6e8f7", "#5aa9ff"),
        new("Forêt", "Vert sapin sur fond de mousse, sous-bois en sombre.",
            "#2f6f4f", "#3c8d40", "#2d7d9a", "#a87a0b", "#b5433a", "#f3f6f1", "#1e2d24", "#121e17", "#dcebdf", "#62cf8f"),
        new("Lavande", "Violet lavande sur blanc lilas, nuit mauve en sombre.",
            "#7c5cbf", "#3f9e7a", "#5b7fd6", "#c48d2a", "#d0506a", "#fbf9ff", "#2d2540", "#1a1730", "#ece7fb", "#b4a3ff"),
        new("Électrique", "Cyan électrique et violet, nuit d'encre en sombre.",
            "#008c9e", "#1f9d55", "#7c4dff", "#c28a00", "#d6246e", "#f5f3ff", "#1a1036", "#0b0a1a", "#e6e3ff", "#00e5ff"),
        new("Or ancien", "Or bruni sur papier crème, brun chaud en sombre.",
            "#8a6a1c", "#3f7a3a", "#2f5f8a", "#c2571a", "#a82a2a", "#fdfbf5", "#1d1b16", "#1b1813", "#ece2c9", "#d4af37"),
        new("Braise", "Orange braise sur fond pêche, rougeoiement en sombre.",
            "#c2410c", "#2f855a", "#2563c9", "#a16207", "#be123c", "#fff3ea", "#2a140c", "#1f1411", "#f8e6dc", "#ff8a5b"),
        new("Mono", "Noir et blanc purs, seules les sévérités en couleur.",
            "#000000", "#1a7f37", "#0550ae", "#8a5a00", "#cf222e", "#ffffff", "#000000", "#000000", "#ffffff", "#ffffff"),
        new("Lagune", "Turquoise de lagune, bleu-vert profond en sombre.",
            "#0e8a7a", "#2f8f46", "#2a7fb8", "#c07f00", "#d1495b", "#f3faf8", "#10302b", "#082127", "#d8eef0", "#2dd4bf"),
        new("Prune", "Prune et rose, velours sombre en sombre.",
            "#7b2d6e", "#2f7d5b", "#4a6fa5", "#b7791f", "#c23a3a", "#fbf6f9", "#2a1426", "#20111e", "#f4e6f0", "#e883d2"),
    ];
}
