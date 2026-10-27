using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Every palette of the catalogue as a selectable tile, each laid over the theme given.</summary>
public partial class ThemePalettesExample
{
    private OmniThemePalette? _picked;

    /// <summary>The theme every tile keeps the shape of; null is the first one of the catalogue.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>The palette in force; null picks the palette the theme comes with.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>Raised with the palette picked.</summary>
    [Parameter]
    public EventCallback<OmniThemePalette> PaletteChanged { get; set; }

    /// <summary>The half of the look the tiles show.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    private OmniThemePalette Own => OmniThemePresets.DefaultPaletteFor(Preset);

    private OmniThemePalette Selected => Palette ?? _picked ?? Own;

    private string TitleOf(OmniThemePalette palette) =>
        ReferenceEquals(palette, Own) ? $"{palette.Name} (default)" : palette.Name;

    private Task SelectAsync(OmniThemePalette palette)
    {
        _picked = palette;
        return PaletteChanged.InvokeAsync(palette);
    }
}
