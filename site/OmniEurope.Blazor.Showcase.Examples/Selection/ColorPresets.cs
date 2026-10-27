namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A named colour the colour examples offer, written as <c>#RRGGBB</c>, the only form the picker accepts.</summary>
/// <param name="Key">A stable identifier of the preset, for a host that translates its name.</param>
/// <param name="Name">The English name of the preset.</param>
/// <param name="Hex">The colour as <c>#RRGGBB</c>.</param>
public sealed record ColorPreset(string Key, string Name, string Hex);

/// <summary>The presets shared by the colour examples and by the page that sets the bound value from outside.</summary>
public static class ColorPresets
{
    /// <summary>The presets, in display order.</summary>
    public static IReadOnlyList<ColorPreset> All { get; } =
    [
        new("Accent", "Accent", "#165DFF"),
        new("Success", "Success", "#087443"),
        new("Warning", "Warning", "#A15C00"),
        new("Danger", "Danger", "#B42318"),
        new("Ink", "Ink", "#172033")
    ];
}
