using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Every theme of the catalogue as a selectable tile, each painted with the palette given.</summary>
public partial class ThemePresetsExample
{
    private OmniThemePreset? _picked;

    /// <summary>The theme in force; null picks the first one of the catalogue.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>Raised with the theme picked.</summary>
    [Parameter]
    public EventCallback<OmniThemePreset> PresetChanged { get; set; }

    /// <summary>The palette every tile wears, or null for the palette each theme comes with.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>The half of the look the tiles show.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    private OmniThemePreset Selected => Preset ?? _picked ?? OmniThemePresets.All[0];

    private static string TitleOf(OmniThemePreset preset) =>
        ReferenceEquals(preset, OmniThemePresets.All[0]) ? $"{preset.Name} (default)" : preset.Name;

    private Task SelectAsync(OmniThemePreset preset)
    {
        _picked = preset;
        return PresetChanged.InvokeAsync(preset);
    }
}
