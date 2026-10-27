using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>One nested theme scope whose every setting is a parameter, holding the sample screen.</summary>
public partial class ThemeScopeExample
{
    /// <summary>Light, dark, or following the system.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.Dark;

    /// <summary>Spacing and control height of the scope.</summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>The theme that gives the shape, or null for the shipped one.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>The palette that gives the colours, or null for the palette of the theme.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>The font of the scope, or null for the one the theme is drawn with.</summary>
    [Parameter]
    public OmniThemeFont? Font { get; set; }

    /// <summary>Whether a theme that moves its backdrop may move it; only some themes have one.</summary>
    [Parameter]
    public bool BackdropMotion { get; set; } = true;

    /// <summary>Whether the content takes the whole width instead of the centred column.</summary>
    [Parameter]
    public bool FullWidth { get; set; }
}
