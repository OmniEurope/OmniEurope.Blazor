using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The sample screen in one scope per appearance, or one scope per density.</summary>
public partial class ThemeScopesExample
{
    private static readonly OmniAppearance[] Appearances = Enum.GetValues<OmniAppearance>();

    private static readonly OmniDensity[] Densities = Enum.GetValues<OmniDensity>();

    /// <summary>Whether the scopes vary by density (the appearance given is then shared) rather than by appearance.</summary>
    [Parameter]
    public bool ByDensity { get; set; }

    /// <summary>The appearance every scope wears when they vary by density.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.Dark;

    /// <summary>The theme every scope wears, or null for the shipped one.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>The palette every scope wears, or null for the palette of the theme.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }
}
